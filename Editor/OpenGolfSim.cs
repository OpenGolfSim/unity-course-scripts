using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;


public class OpenGolfSimMenu : EditorWindow
{
    

    [MenuItem("Tools/OpenGolfSim/Import Meshes")]
    public static void ShowImportWindow()
    {
        GetWindow<OpenGolfSimImportWindow>("OpenGolfSim SDK: Import Meshes");
    }

    [MenuItem("Tools/OpenGolfSim/Course Tools")]
    public static void ShowExportWindow()
    {
      GetWindow<OpenGolfSimExportWindow>("OpenGolfSim SDK: Build Course");
    }

    private const string menuPath = "GameObject/OpenGolfSim/Create Course Details";
    [MenuItem(menuPath, false, 10)]
    private static void Create(MenuCommand menuCommand)
    {
        // Create root GameObject
        GameObject go = new GameObject("OGSCourseDetails");

        // Add the custom runtime component
        go.AddComponent<OGSCourse>();

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
    public List<string> objPaths = new List<string>();
    public Material assignedMaterial;
}