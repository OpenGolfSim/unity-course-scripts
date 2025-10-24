using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using KdTree;
using KdTree.Math;

public class OpenGolfSimOBJImport
{
    struct OBJVertex
    {
        public int vIdx;
        public int vtIdx;
        public int vnIdx;
        public OBJVertex(int v, int vt, int vn)
        {
            vIdx = v;
            vtIdx = vt;
            vnIdx = vn;
        }
        public override int GetHashCode()
        {
            return vIdx * 73856093 ^ vtIdx * 19349663 ^ vnIdx * 83492791;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is OBJVertex)) return false;
            OBJVertex other = (OBJVertex)obj;
            return vIdx == other.vIdx && vtIdx == other.vtIdx && vnIdx == other.vnIdx;
        }
    }

    public static Mesh ImportOBJ(string path)
    {
        var positions = new List<Vector3>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        var meshVertices = new List<Vector3>();
        var meshUVs = new List<Vector2>();
        var meshNormals = new List<Vector3>();
        var meshTriangles = new List<int>();

        var vertexDict = new Dictionary<OBJVertex, int>();

        foreach (string line in File.ReadLines(path))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith("v "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                positions.Add(new Vector3(x, -y, z));
            }
            else if (trimmed.StartsWith("vt "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
                uvs.Add(new Vector2(u, v));
            }
            else if (trimmed.StartsWith("vn "))
            {
                var parts = trimmed.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                float nx = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float ny = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float nz = float.Parse(parts[3], CultureInfo.InvariantCulture);
                normals.Add(new Vector3(nx, ny, nz));
            }
            else if (trimmed.StartsWith("f "))
            {
                var parts = trimmed.Substring(2).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3) continue; // skip invalid faces

                int[] faceIndices = new int[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    string[] idx = parts[i].Split('/');
                    int vIdx = idx.Length > 0 && idx[0] != "" ? int.Parse(idx[0]) - 1 : -1;
                    int vtIdx = idx.Length > 1 && idx[1] != "" ? int.Parse(idx[1]) - 1 : -1;
                    int vnIdx = idx.Length > 2 && idx[2] != "" ? int.Parse(idx[2]) - 1 : -1;

                    var vert = new OBJVertex(vIdx, vtIdx, vnIdx);
                    int meshIdx;
                    if (!vertexDict.TryGetValue(vert, out meshIdx))
                    {
                        meshVertices.Add(vIdx >= 0 ? positions[vIdx] : Vector3.zero);
                        meshUVs.Add(vtIdx >= 0 ? uvs[vtIdx] : Vector2.zero);
                        meshNormals.Add(vnIdx >= 0 ? normals[vnIdx] : Vector3.up);

                        meshIdx = meshVertices.Count - 1;
                        vertexDict[vert] = meshIdx;
                    }
                    faceIndices[i] = meshIdx;
                }

                // triangulate polygon (should be already triangles, but if not, fan method)
                for (int i = 1; i < parts.Length - 1; i++)
                {
                  // meshTriangles.Add(faceIndices[0]);
                  // meshTriangles.Add(faceIndices[i]);
                  // meshTriangles.Add(faceIndices[i + 1]);
                  meshTriangles.Add(faceIndices[0]);
                  meshTriangles.Add(faceIndices[i + 1]);
                  meshTriangles.Add(faceIndices[i]);
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = meshVertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = meshVertices.ToArray();
        if (meshUVs.Count == meshVertices.Count) mesh.uv = meshUVs.ToArray();
        if (meshNormals.Count == meshVertices.Count) mesh.normals = meshNormals.ToArray();
        mesh.triangles = meshTriangles.ToArray();
        // if (mesh.normals == null || mesh.normals.Length == 0) mesh.RecalculateNormals();
        mesh.RecalculateNormals();

        mesh.UploadMeshData(false); // mesh is readable

        var triangles = mesh.triangles;
        var vertices = mesh.vertices;
        var vertexIsBoundary = new bool[vertices.Length];
        var edgeCount = new Dictionary<(int, int), int>();

        // Build edge map
        for (int i = 0; i < triangles.Length; i += 3) {
          int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
          void AddEdge(int v1, int v2) {
            var edge = v1 < v2 ? (v1, v2) : (v2, v1);
            if (!edgeCount.ContainsKey(edge)) edgeCount[edge] = 0;
            edgeCount[edge]++;
          }
          AddEdge(a, b);
          AddEdge(b, c);
          AddEdge(c, a);
        }

        // Mark boundary vertices
        foreach (var kvp in edgeCount) {
          if (kvp.Value == 1) { // Edge only used by one triangle = boundary
            vertexIsBoundary[kvp.Key.Item1] = true;
            vertexIsBoundary[kvp.Key.Item2] = true;
          }
        }

        // Collect positions of all boundary vertices
        var boundaryPositions = new List<Vector3>();
        for (int i = 0; i < vertices.Length; ++i) {
          if (vertexIsBoundary[i]) {
            boundaryPositions.Add(vertices[i]);
          }
        }


        SetEdgeBlendColors(mesh, 0.2f);

        // float blendRadius = 0.2f;
        // var colors = new Color[vertices.Length];

        // for (int i = 0; i < vertices.Length; ++i)
        // {
        //     if (vertexIsBoundary[i])
        //     {
        //         colors[i] = new Color(1, 0, 0, 1); // Fully boundary (blend=1)
        //         continue;
        //     }

        //     float minDist = float.MaxValue;
        //     foreach (var boundary in boundaryPositions)
        //     {
        //         float dist = Vector3.Distance(vertices[i], boundary);
        //         if (dist < minDist) minDist = dist;
        //     }

        //     if (minDist <= blendRadius)
        //     {
        //         float t = 1f - Mathf.Clamp01(minDist / blendRadius); // 1 at edge, 0 at blendRadius
        //         colors[i] = new Color(t, 0, 0, 1); // t goes from 1 to 0 as you move away from the edge
        //     }
        //     else
        //     {
        //         colors[i] = new Color(0, 0, 0, 1); // Not near an edge (blend=0)
        //     }
        // }
        // mesh.colors = colors;

        // Use vertex color to mark boundary
        // var colors = new Color[vertices.Length];
        // for (int i = 0; i < vertices.Length; ++i)
        //     colors[i] = vertexIsBoundary[i] ? Color.red : Color.black;
        // mesh.colors = colors;        
        return mesh;
    }


    public static void SetEdgeBlendColors(Mesh mesh, float blendRadius = 0.2f)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        // 1. Build edge map
        var edgeCount = new Dictionary<(int, int), int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            void AddEdge(int v1, int v2)
            {
                var edge = v1 < v2 ? (v1, v2) : (v2, v1);
                if (!edgeCount.ContainsKey(edge)) edgeCount[edge] = 0;
                edgeCount[edge]++;
            }
            AddEdge(a, b);
            AddEdge(b, c);
            AddEdge(c, a);
        }

        // 2. Mark boundary vertices
        var vertexIsBoundary = new bool[vertices.Length];
        foreach (var kvp in edgeCount)
        {
            if (kvp.Value == 1)
            {
                vertexIsBoundary[kvp.Key.Item1] = true;
                vertexIsBoundary[kvp.Key.Item2] = true;
            }
        }

        // 3. Build KD-tree of boundary vertices
        var tree = new KdTree<float, int>(3, new FloatMath());
        for (int i = 0; i < vertices.Length; ++i)
        {
            if (vertexIsBoundary[i])
            {
                var v = vertices[i];
                tree.Add(new[] { v.x, v.y, v.z }, i);
            }
        }

        // 4. Assign blend color
        var colors = new Color[vertices.Length];
        for (int i = 0; i < vertices.Length; ++i)
        {
            if (vertexIsBoundary[i])
            {
                colors[i] = new Color(1, 0, 0, 1); // Full boundary
                continue;
            }
            var v = vertices[i];
            var nearest = tree.GetNearestNeighbours(new[] { v.x, v.y, v.z }, 1);
            float dist = float.MaxValue;
            if (nearest.Length > 0)
            {
                var nn = nearest[0];
                var boundaryPoint = new Vector3(nn.Point[0], nn.Point[1], nn.Point[2]);
                dist = Vector3.Distance(v, boundaryPoint);
            }
            if (dist <= blendRadius)
            {
                float t = 1f - Mathf.Clamp01(dist / blendRadius); // 1 at edge, 0 at blendRadius
                colors[i] = new Color(t, 0, 0, 1);
            }
            else
            {
                colors[i] = new Color(0, 0, 0, 1);
            }
        }
        mesh.colors = colors;
    }
}