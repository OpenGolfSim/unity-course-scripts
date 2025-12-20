using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

public class OpenGolfSimOBJImport
{
    public class ImportedMesh
    {
        public Mesh mesh;
        public string name;
    }

    public static List<ImportedMesh> ImportOBJ(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("OBJ file not found: " + path);
            return null;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<ImportedMesh> importedMeshes = new List<ImportedMesh>();

        // For accumulating mesh data per object
        List<int> meshTriangles = new List<int>();
        List<Vector3> meshVertices = new List<Vector3>();
        List<Vector3> meshNormals = new List<Vector3>();
        List<Vector2> meshUVs = new List<Vector2>();
        List<Color> meshColors = new List<Color>();

        string currentName = "OBJMesh_0";
        int meshCount = 0;

        void CommitCurrentMesh()
        {
            if (meshTriangles.Count == 0) return; // skip empty
            Mesh mesh = new Mesh();
            if (meshVertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(meshVertices);
            mesh.SetTriangles(meshTriangles, 0);
            if (meshNormals.Count == meshVertices.Count)
                mesh.SetNormals(meshNormals);
            if (meshUVs.Count == meshVertices.Count)
                mesh.SetUVs(0, meshUVs);
            if (meshColors.Count == meshVertices.Count) {
                mesh.SetColors(meshColors);
            } else {
                Debug.Log("No mesh colors!");
            }

            mesh.RecalculateBounds();
            if (meshNormals.Count != meshVertices.Count)
                mesh.RecalculateNormals();

            importedMeshes.Add(new ImportedMesh { mesh = mesh, name = currentName });

            // Clear for next mesh
            meshVertices.Clear();
            meshNormals.Clear();
            meshUVs.Clear();
            meshTriangles.Clear();
            meshColors.Clear();
        }

        using (StreamReader sr = new StreamReader(path))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("o ") || line.StartsWith("g "))
                {
                    CommitCurrentMesh();
                    currentName = line.Substring(2).Trim();
                    if (string.IsNullOrEmpty(currentName))
                        currentName = "OBJMesh_" + (++meshCount);
                }
                else if (line.StartsWith("v "))
                {
                    var vals = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    float x = float.Parse(vals[1]);
                    float y = float.Parse(vals[2]);
                    float z = float.Parse(vals[3]);
                    vertices.Add(new Vector3(x, y, -z)); // Invert Z
                    if (vals.Length >= 7) // v x y z r g b
                    {
                        float r = float.Parse(vals[4]);
                        float g = float.Parse(vals[5]);
                        float b = float.Parse(vals[6]);
                        if (r > 1f || g > 1f || b > 1f)
                        {
                            r /= 255f; g /= 255f; b /= 255f;
                        }
                        colors.Add(new Color(r, g, b, 1f));
                    }
                    else
                    {
                        colors.Add(Color.white);
                    }
                }
                else if (line.StartsWith("vn "))
                {
                    var vals = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    float nx = float.Parse(vals[1]);
                    float ny = float.Parse(vals[2]);
                    float nz = float.Parse(vals[3]);
                    normals.Add(new Vector3(nx, ny, -nz)); // invert Z for normals
                }
                else if (line.StartsWith("vt "))
                {
                    var vals = line.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    uvs.Add(new Vector2(float.Parse(vals[1]), float.Parse(vals[2])));
                }
                else if (line.StartsWith("f "))
                {
                    var vals = line.Substring(2).Trim().Split(' ');

                    // Triangulate face with reversed winding order
                    if (vals.Length < 3)
                        continue; // skip degenerate
                    for (int i = vals.Length - 1; i >= 2; i--)
                    {
                        // Add triangle (i, i-1, 0): always reverse the winding
                        int[] order = new int[] { i, i - 1, 0 };
                        foreach (int j in order)
                        {
                            var indices = vals[j].Split('/');
                            int vertIdx = int.Parse(indices[0]) - 1;
                            meshVertices.Add(vertices[vertIdx]);
                            meshTriangles.Add(meshVertices.Count - 1);
                            meshColors.Add(colors[vertIdx]);

                            if (indices.Length > 1 && indices[1] != "")
                            {
                                int uvIdx = int.Parse(indices[1]) - 1;
                                meshUVs.Add(uvs[uvIdx]);
                            }
                            else
                            {
                                meshUVs.Add(Vector2.zero);
                            }

                            if (indices.Length > 2 && indices[2] != "")
                            {
                                int nIdx = int.Parse(indices[2]) - 1;
                                meshNormals.Add(normals[nIdx]);
                            }
                            else
                            {
                                meshNormals.Add(Vector3.up);
                            }
                        }
                    }
                }
            }
        }
        // Commit last mesh after file ends
        CommitCurrentMesh();

        return importedMeshes;
    }
}