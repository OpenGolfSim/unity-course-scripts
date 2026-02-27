using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[System.Serializable]
public class TreeLODInstanceBatch
{
    public Mesh mesh;
    public Material material;
    public float scaleBase;
    public float scaleVariation;
    public int subMeshIndex; // << NEW
}
[System.Serializable]
public class TreeLODGroup
{
    public List<TreeLODInstanceBatch> batches = new();
    public List<Matrix4x4> matrices = new();
    public float lodStart;
    public float lodEnd;
}

[System.Serializable]
public class TreeInfo
{
    public GameObject prefab;
    public int frequencyWeighting = 1;
    // public float treeScaleBase = 1f;
    // public float treeScaleVariation = 0f;
    public Vector2 treeScaleRange = new Vector2(0.85f, 1.25f);
    [HideInInspector] public List<TreeLODGroup> lods = new();
    [HideInInspector] public float baseHeight = 1f;
    [HideInInspector] public float cullScreenRelativeHeight = 0.01f; // default
    [HideInInspector] public bool hasBillboard = false;
    [HideInInspector] public BillboardAsset billboardAsset = null;
    [HideInInspector] public Material billboardMaterial = null;
    [HideInInspector] public bool initialized = false;
}


[ExecuteInEditMode]
public class OGSTreePlanter : MonoBehaviour
{
    [Header("Tree Mask Settings")]
    public Texture2D treeMask;
    public Terrain terrain;

    [Header("Tree Prefabs (with LODGroups)")]
    // public TreeInfo treeA;
    // public TreeInfo treeB;
    public List<TreeInfo> trees = new List<TreeInfo>();

    [Header("Planting Settings")]
    public float minDensity = 0.1f;
    public float maxDensity = 0.8f;
    public int randomSeedValue = 123;
    public float maskToDensityMultiplier = 0.8f;
    public float yOffset = 0.0f;
    // public Vector2 treeScaleRange = new(0.85f, 1.25f);
    
    [Header("Performance Tuning")]
    public float maxRenderDistance = 300f;

    // public bool autoPlantOnPlay = false;
    private Matrix4x4[] instanceBuffer = new Matrix4x4[MAX_BATCH];
    private List<Matrix4x4>[] lodBuckets = new List<Matrix4x4>[0];
    private static Mesh BillboardQuad;
    private int areaWidth = 256;
    private int areaLength = 256;


    const int MAX_BATCH = 1023;

    // void Start()
    // {
    //   Debug.Log("STARTED");
    //     ExtractLODGroups(treeA);
    //     ExtractLODGroups(treeB);
    //     if (Application.isPlaying && autoPlantOnPlay) {
    //         PlantTrees();
    //     }
    //     // // Extract LODs from prefabs

    //     // if (autoPlantOnPlay) {
    //     //     PlantTrees();
    //     // }
    // }

    Camera GetCurrentCamera()
    {
        if (Application.isPlaying)
        {
            return Camera.main;
        }
        #if UNITY_EDITOR
        if (SceneView.lastActiveSceneView != null)
            return SceneView.lastActiveSceneView.camera;
        #endif
        return null;
    }
    void OnEnable()
    {
       Regenerate();
    }

    public void Regenerate() {
        // Extract LODs only once per prefab assign/change
        foreach(var tree in trees) {
            ExtractLODGroups(tree);
        }
        // ExtractLODGroups(treeA);
        // ExtractLODGroups(treeB);
        // Optionally auto-plant
        // if (autoPlantOnPlay)
        // {
            PlantTrees();
        // }
        #if UNITY_EDITOR
        // Ensure scene saves on change
        EditorApplication.update += RepaintSceneView;
        #endif
    }

    void OnDisable()
    {
        #if UNITY_EDITOR
        EditorApplication.update -= RepaintSceneView;
        #endif
    }

    void Awake() {
        if (BillboardQuad == null) BillboardQuad = BuildQuadMesh();
    }
    Mesh BuildQuadMesh() {
        // Make sure the quad is -0.5..+0.5 in X and 0..1 in Y, with pivot at bottom center
        var mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(-0.5f,0,0), new Vector3(0.5f,0,0),
            new Vector3(-0.5f,1,0), new Vector3(0.5f,1,0)
        };
        mesh.uv = new Vector2[] {
            new Vector2(0,0), new Vector2(1,0),
            new Vector2(0,1), new Vector2(1,1)
        };
        mesh.triangles = new int[] { 0,2,1, 1,2,3 };
        return mesh;
    }    
    // Helper: force SceneView to repaint so DrawMeshInstanced sees results in edit mode
    #if UNITY_EDITOR
    void RepaintSceneView()
    {
        if (!Application.isPlaying && SceneView.lastActiveSceneView != null)
            SceneView.lastActiveSceneView.Repaint();
    }
    #endif

    public void ExtractLODGroups(TreeInfo info)
    {
        if (info == null || info.prefab == null) return;
        var instance = Instantiate(info.prefab);
        instance.SetActive(false);
        var allRenderers = instance.GetComponentsInChildren<Renderer>();
        if (allRenderers.Length > 0)
        {
            Bounds b = allRenderers[0].bounds;
            foreach (var r in allRenderers) b.Encapsulate(r.bounds);
            info.baseHeight = b.size.y;
        }
        else
        {
            info.baseHeight = 1f;
        }
        
        var lodGroup = instance.GetComponent<LODGroup>();
        if (lodGroup == null)
        {
            Debug.LogError("Tree prefab '" + info.prefab.name + "' must have LODGroup!");
            DestroyImmediate(instance);
            return;
        }
        info.lods.Clear();
        var lods = lodGroup.GetLODs();
        for (int i = 0; i < lods.Length; i++)
        {
            var group = new TreeLODGroup()
            {
                lodStart = lods[i].screenRelativeTransitionHeight,
                lodEnd = i == 0 ? 1f : lods[i - 1].screenRelativeTransitionHeight,
                batches = new List<TreeLODInstanceBatch>()
            };
            Debug.Log($"{info.prefab.name} LOD{i}, lodStart:{group.lodStart}, lodEnd:{group.lodEnd}");
            foreach (var renderer in lods[i].renderers)
            {
                // For MeshRenderer and BillboardRenderer support
                var billboard = renderer as BillboardRenderer;
                if (billboard != null)
                {
                    // This is a billboard LOD!
                    group.batches.Add(new TreeLODInstanceBatch {
                        mesh = null, // No mesh
                        material = billboard.material,
                        subMeshIndex = 0 // Not needed, but keep signature
                    });
                    // Optionally cache BillboardAsset reference here if needed
                    info.hasBillboard = true;
                    info.billboardAsset = billboard.billboard;
                    info.billboardMaterial = billboard.material;
                    continue;
                }
                var meshFilter = renderer.GetComponent<MeshFilter>();
                var mesh = meshFilter.sharedMesh;
                var materials = renderer.sharedMaterials;

                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    // Some meshes may have more submeshes than materials, clamp if needed
                    var mat = materials[Mathf.Min(subMesh, materials.Length-1)];
                    group.batches.Add(new TreeLODInstanceBatch { mesh = mesh, material = mat, subMeshIndex = subMesh });
                }

                // var meshFilter = renderer.GetComponent<MeshFilter>();
                // if (meshFilter == null) continue;
                // var mat = renderer.sharedMaterial;
                // group.batches.Add(new TreeLODInstanceBatch { mesh = meshFilter.sharedMesh, material = mat });
            }
            info.lods.Add(group);
        }
        if (lods.Length > 0) {
            info.cullScreenRelativeHeight = lods[lods.Length - 1].screenRelativeTransitionHeight;
        }

        DestroyImmediate(instance);
    }

    public void PlantTrees()
    {
        Debug.Log("Planting trees...");
        Random.InitState(randomSeedValue);
        // Clear matrices for all LODs
        foreach (var tree in trees) {
            foreach (var lod in tree.lods) {
                lod.matrices.Clear();
            }
        }
        // foreach (var lod in treeA.lods) lod.matrices.Clear();
        // foreach (var lod in treeB.lods) lod.matrices.Clear();

        float tw = terrain.terrainData.size.x;
        float tl = terrain.terrainData.size.z;
        int planted = 0;
        for (int x = 0; x < areaWidth; x++)
        for (int z = 0; z < areaLength; z++)
        {
            int maskX = Mathf.FloorToInt((float)x / areaWidth * treeMask.width);
            int maskZ = Mathf.FloorToInt((float)z / areaLength * treeMask.height);
            float maskVal = treeMask.GetPixel(
                Mathf.Clamp(maskX, 0, treeMask.width - 1),
                Mathf.Clamp(maskZ, 0, treeMask.height - 1)
            ).grayscale;

            float density = maskVal == 0 ? 0 : Mathf.Lerp(minDensity, maxDensity, maskVal) * maskToDensityMultiplier;
            if (Random.value > density) continue;

            if (z == 0 && x % 16 == 0) Debug.Log($"MaskX:{maskX} MaskZ:{maskZ} Value:{maskVal}, density:{density}");

            float worldX = ((float)x / areaWidth) * tw + terrain.transform.position.x + Random.Range(-0.5f, 0.5f);
            float worldZ = ((float)z / areaLength) * tl + terrain.transform.position.z + Random.Range(-0.5f, 0.5f);
            
            float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrain.transform.position.y;
            worldY += yOffset;
            
            // bool useA = Random.value > 0.5f;
            TreeInfo tree = GetRandomTree(Random.value);
            float scale = Random.Range(tree.treeScaleRange.x, tree.treeScaleRange.y);
            Quaternion rot = Quaternion.Euler(0, Random.value * 360f, 0);
            Vector3 pos = new(worldX, worldY, worldZ);
            Matrix4x4 mtx = Matrix4x4.TRS(pos, rot, Vector3.one * scale);

            // Place in LOD0 initially - will switch at render time (see below)
            if (tree.lods.Count > 0) {
                tree.lods[0].matrices.Add(mtx);
                planted++;
            }
            // if (useA && treeA.lods.Count > 0) {
            //     treeA.lods[0].matrices.Add(mtx);
            //     planted++;
            // } else if (!useA && treeB.lods.Count > 0) {
            //     treeB.lods[0].matrices.Add(mtx);
            //     planted++;
            // }
        }
        // Debug.Log($"Planted {planted} trees");
        int idx = 0;
        foreach (var tree in trees) {
            Debug.Log($"tree {idx} has {tree.lods[0].matrices.Count} locations");
            idx++;
        }
        // Debug.Log($"treeB has {treeB.lods[0].matrices.Count} lods");
    }

    void Update()
    {
        Camera cam = GetCurrentCamera();
        if (cam == null) return;

        foreach (var tree in trees) {
           RenderTrees(tree, cam); 
        }
        // RenderTrees(treeA, cam);
        // RenderTrees(treeB, cam);
        
    }

    TreeInfo GetRandomTree(float randomValue)
    {
        // Step 1: Find the total weight
        int totalWeight = 0;
        foreach (var tree in trees)
        {
            totalWeight += tree.frequencyWeighting;
        }
        // Step 2: Pick a random value in [0, totalWeight)
        float r = randomValue * totalWeight;
        int accumulated = 0;

        // Step 3: Walk through the list, summing weights
        foreach (var tree in trees)
        {
            accumulated += tree.frequencyWeighting;
            if (r < accumulated)
            {
                return tree;
            }
        }

        // Should never get here unless something went wrong
        return trees[trees.Count - 1];
    }

    void RenderTrees(TreeInfo info, Camera cam)
    {
        if (info == null || info.lods.Count == 0) return;
        var matricesSource = info.lods[0].matrices;
        if (matricesSource.Count == 0) return;

        // LOD bucketing
        lodBuckets = new List<Matrix4x4>[info.lods.Count];
        for (int i = 0; i < info.lods.Count; i++) lodBuckets[i] = new();

        foreach (var mtx in matricesSource)
        {
            Vector3 treePosition = mtx.GetColumn(3);
            float dist = Vector3.Distance(treePosition, cam.transform.position);

            if (dist > maxRenderDistance)
                continue; // Completely skip trees that are too far away

            float screenHeight = ComputeScreenRelativeHeight(cam, mtx, dist, info);
            float minScreenHeight = info.cullScreenRelativeHeight;

            if (screenHeight < minScreenHeight)
                continue; // Don't render, tree is culled!

            bool assigned = false;
            for (int l = 0; l < info.lods.Count; l++)
            {
                if (screenHeight >= info.lods[l].lodStart)
                {
                    lodBuckets[l].Add(mtx);
                    assigned = true;
                    break;
                }
            }
            if (!assigned && info.lods.Count > 0)
                lodBuckets[info.lods.Count - 1].Add(mtx);

        }

        // Draw all batches for each LOD group at the positions for that LOD
        for (int l = 0; l < info.lods.Count; l++)
        {
            var group = info.lods[l];
            var matrices = lodBuckets[l];
            if (matrices.Count == 0) continue;
            // Check if this is the billboard LOD
            bool isBillboardLOD = info.hasBillboard && (l == info.lods.Count - 1);
            
            if (!isBillboardLOD) {
                ShadowCastingMode shadowMode = (l <= 1) ? ShadowCastingMode.On : ShadowCastingMode.Off;

                foreach (var batch in group.batches)
                {
                    if (batch.mesh == null || batch.material == null) continue;
                    // In your draw loop:
                    for (int i = 0; i < matrices.Count; i += MAX_BATCH)
                    {
                        int batchCount = Mathf.Min(MAX_BATCH, matrices.Count - i);
                        matrices.CopyTo(i, instanceBuffer, 0, batchCount);
                        Graphics.DrawMeshInstanced(
                            batch.mesh,
                            batch.subMeshIndex,
                            batch.material,
                            instanceBuffer,
                            batchCount,
                            null,
                            shadowMode,
                            true
                        );
                    }
                }
            }
        }
        // 2. Draw billboards (billboard LOD only)
        // For trees with billboards, last LOD only!
        if (info.hasBillboard && info.lods.Count > 0)
        {
            int l = info.lods.Count - 1;
            var group = info.lods[l];
            var matrices = lodBuckets[l];
            if (matrices.Count > 0 && info.billboardMaterial != null && BillboardQuad != null)
            {
                // Build matrix array of aligned billboards
                Matrix4x4[] billboardMatrices = new Matrix4x4[Mathf.Min(MAX_BATCH, matrices.Count)];
                for (int i = 0; i < matrices.Count; i += MAX_BATCH)
                {
                    int batchCount = Mathf.Min(MAX_BATCH, matrices.Count - i);
                    for (int j = 0; j < batchCount; j++)
                    {
                        Matrix4x4 treeMtx = matrices[i + j];
                        Vector3 pos = treeMtx.GetColumn(3);
                        // Always face camera (Y-locked for upright trees)
                        Quaternion rot = Quaternion.LookRotation(cam.transform.forward, Vector3.up);
                        float scale = treeMtx.lossyScale.y;
                        billboardMatrices[j] = Matrix4x4.TRS(pos, rot, new Vector3(scale, scale, 1f));
                    }
                    Graphics.DrawMeshInstanced(
                        BillboardQuad,
                        0,
                        info.billboardMaterial,
                        billboardMatrices,
                        batchCount,
                        null,
                        ShadowCastingMode.Off,
                        true
                    );
                }
            }
        }

    }

    int GetLodIndexForHeight(TreeInfo info, float screenHeight)
    {
        for (int l = 0; l < info.lods.Count; l++)
            if (screenHeight >= info.lods[l].lodStart)
                return l;
        return info.lods.Count - 1;
    }

    // Similar to how LODGroup calculates the visible fraction
    float ComputeScreenRelativeHeight(Camera cam, Matrix4x4 mtx, float dist, TreeInfo tree)
    {
        float scale = mtx.lossyScale.y;
        float objHeight = tree.baseHeight * scale;

        float fovRad = cam.fieldOfView * Mathf.Deg2Rad;       
        float frustumHeight = 2.0f * dist * Mathf.Tan(fovRad * 0.5f); // How tall the camera 'window' is at dist
        float relativeHeight = objHeight / frustumHeight;      // Portion of screen vertically
        return relativeHeight;  // 1.0 = fills screen, 0.5 = half screen, etc.
    }

    void OnValidate() {
        foreach (var tree in trees) {
            if (tree != null && !tree.initialized) {
                // Only set defaults when fields are at defaults
                tree.frequencyWeighting = 1;
                tree.treeScaleRange = new Vector2(0.85f, 1.25f);
                tree.baseHeight = 1f;
                tree.cullScreenRelativeHeight = 0.01f;
                tree.initialized = true; // Mark as initialized, never overwrite again
            }
        }
    }
}