using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class OpenGolfSimConstants
{
    public static string BannerPath = "Packages/com.opengolfsim.tools/Editor/ogs_devtools.png";
}

public class OpenGolfSimMenu : EditorWindow
{
    


    [MenuItem("Tools/OpenGolfSim/Export Course Build")]
    public static void ShowExportWindow()
    {
      GetWindow<OpenGolfSimExportWindow>("OpenGolfSim Dev Toolkit: Export Course Build");
    }

    [MenuItem("Tools/OpenGolfSim/Import Meshes")]
    public static void ShowImportWindow()
    {
        GetWindow<OpenGolfSimImportWindow>("OpenGolfSim Dev Toolkit: Import Meshes");
    }    

    private const string menuPathTrees = "GameObject/OpenGolfSim/Create Tree Planter";
    [MenuItem(menuPathTrees, false, 10)]
    private static void CreatePlanter(MenuCommand menuCommand)
    {
        OGSTreePlanter existingRef = FindObjectOfType<OGSTreePlanter>();
        if (existingRef != null) {
            Debug.LogWarning("OGSTreePlanter already exists!");
            bool result = EditorUtility.DisplayDialog(
                "Warning", // Title of the dialog
                "OGSTreePlanter already exists", // Message body
                "Dismiss"
            );
            return;
        }

        // Create root GameObject
        GameObject go = new GameObject("OGSTreePlant");

        // Add the custom runtime component
        go.AddComponent<OGSTreePlanter>();

        // If the menu was invoked from a context (like creating as child), parent appropriately
        GameObject parent = menuCommand.context as GameObject;
        if (parent != null)
        {
            // Align to parent and set parent
            GameObjectUtility.SetParentAndAlign(go, parent);
        }
        else
        {
            // Place at scene origin by default
            go.transform.position = Vector3.zero;
        }

        // Register Undo so the action can be undone
        Undo.RegisterCreatedObjectUndo(go, "Create Course Details");

        // Select the newly created object
        Selection.activeGameObject = go;
    }

    private const string menuPath = "GameObject/OpenGolfSim/Create Course Details";
    [MenuItem(menuPath, false, 10)]
    private static void Create(MenuCommand menuCommand)
    {
        OGSCourseData existingRef = FindObjectOfType<OGSCourseData>();
        if (existingRef != null) {
            Debug.LogWarning("OGSCourseData already exists!");
            bool result = EditorUtility.DisplayDialog(
                "Warning", // Title of the dialog
                "OGSCourseData already exists", // Message body
                "Dismiss"
            );
            return;
        }


        // Create root GameObject
        GameObject go = new GameObject("OGSCourseData");

        // Add the custom runtime component
        go.AddComponent<OGSCourseData>();

        // If the menu was invoked from a context (like creating as child), parent appropriately
        GameObject parent = menuCommand.context as GameObject;
        if (parent != null)
        {
            // Align to parent and set parent
            GameObjectUtility.SetParentAndAlign(go, parent);
        }
        else
        {
            // Place at scene origin by default
            go.transform.position = Vector3.zero;
        }

        // Register Undo so the action can be undone
        Undo.RegisterCreatedObjectUndo(go, "Create Course Details");

        // Select the newly created object
        Selection.activeGameObject = go;
    }

    // Optional: make the menu item enabled only when a scene is open.
    // This validation method ensures the menu item is enabled (return true) only when appropriate.
    [MenuItem(menuPath, true)]
    private static bool ValidateCreate()
    {
        // Don't allow creation when application is playing
        return !Application.isPlaying;
    }


    

    // [MenuItem("Tools/OpenGolfSim/Inspect Selected")]
    // static void InspectSelectedPrefabMeshes()
    // {
    //     foreach (var obj in Selection.objects)
    //     {
    //         // Try to load as GameObject (works for prefabs and imported models)
    //         GameObject go = obj as GameObject;
    //         if (!go)
    //         {
    //             var path = AssetDatabase.GetAssetPath(obj);
    //             go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    //         }

    //         if (go)
    //         {
    //             Debug.Log($"Inspecting prefab/model: {go.name}");
    //             var meshFilters = go.GetComponentsInChildren<MeshFilter>(true);
    //             foreach (var mf in meshFilters)
    //             {
    //                 var mesh = mf.sharedMesh;
    //                 if (mesh != null)
    //                 {
    //                     Debug.Log($"Mesh: {mesh.name} | Vertices: {mesh.vertexCount} | Parent: {mf.transform.name}");
    //                     // You can add more mesh inspection code here
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogWarning($"{obj.name} is not a prefab or model GameObject.");
    //         }
    //     }
    // }

}


[System.Serializable]
public class ObjImportItem
{
    public string objPath;
    public string fileName;
    public Material assignedMaterial;
}
public class ObjImportGroup
{
    public string prefix;
    // public List<string> objPaths = new List<string>();
    public List<OpenGolfSimOBJImport.ImportedMesh> objMeshes = new List<OpenGolfSimOBJImport.ImportedMesh>();
    public Material assignedMaterial;
}