using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;

public class OpenGolfSimImportWindow : EditorWindow
{
    private string selectedFolderPath = "";
    private bool shouldImport;
    private string[] objFiles = null;
    private List<ObjImportGroup> objImportGroups = new List<ObjImportGroup>();
    private int objCount = 0;
    private Vector2 scrollPos;
    private Material fairwayMaterial;
    private Material fringeMaterial;
    private Material greenMaterial;
    private Material roughMaterial;
    private Material firstCutMaterial;
    private Material teeMaterial;
    private Material sandMaterial;
    private Material riverMaterial;

    private void OnEnable()
    {
        // Load default materials (update the path as needed)
        fairwayMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Fairway.mat");
        fringeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Fringe.mat");
        greenMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Green.mat");
        roughMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Rough.mat");
        firstCutMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/FirstCut.mat");
        teeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Tee.mat");
        sandMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/Sand.mat");
        riverMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/LakeRiverBed.mat");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        // GUILayout.FlexibleSpace();
        GUILayout.Space(40);
        GUIStyle customLabelStyle = new GUIStyle(); 
        customLabelStyle.fontSize = 16; // Set to your desired font size
        customLabelStyle.fontStyle = FontStyle.Bold; // Set to your desired font size
        customLabelStyle.normal.textColor = Color.white; // Example: set text color to red

        GUILayout.Label("Import Meshes", customLabelStyle);
        GUILayout.EndVertical();

        // GUILayout.FlexibleSpace();
        Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(OpenGolfSimConstants.BannerPath, typeof(Texture));
        GUILayout.Box(banner, GUILayout.Width(240), GUILayout.Height(60));
        GUILayout.EndHorizontal();

        // Draw a horizontal line using a custom style
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); 

        GUILayout.Space(30);


        GUILayout.BeginVertical("Box");
        GUILayout.Label("Input", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        GUILayout.Label("Mesh path", EditorStyles.boldLabel);
        
        if (!string.IsNullOrEmpty(selectedFolderPath) && objImportGroups.Count > 0)
        {
            GUILayout.Label(selectedFolderPath);
            if (GUILayout.Button("Clear"))
            {
                selectedFolderPath = null;
            }
        } else {
            if (GUILayout.Button("Select OBJ File"))
            {
                // string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                string objPath = EditorUtility.OpenFilePanel("Select OBJ", "", "obj");
                if (!string.IsNullOrEmpty(objPath))
                {
                    selectedFolderPath = objPath;
                    Debug.Log($"objPath: {objPath}");
                    
                    ScanAndGroupObjFiles();
                }
            }
        }
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();


    if (!string.IsNullOrEmpty(selectedFolderPath) && objImportGroups.Count > 0)
    {
        EditorGUILayout.Space();
        GUILayout.BeginVertical("Box");
        GUILayout.Label("Meshes", EditorStyles.boldLabel);
        GUILayout.Space(10);
        // EditorGUILayout.LabelField("Selected Folder:", selectedFolderPath);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Surface", GUILayout.Width(100));
        GUILayout.Label("Files", GUILayout.Width(300));
        GUILayout.Label("Material", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        foreach (var group in objImportGroups)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(group.prefix, GUILayout.Width(100));
            
            List<string> meshNames = new List<string>();
            foreach (var m in group.objMeshes)
            {
                meshNames.Add(m.name);
            }
            EditorGUILayout.LabelField(string.Join(", ", meshNames), GUILayout.Width(300));

            group.assignedMaterial = (Material)EditorGUILayout.ObjectField(group.assignedMaterial, typeof(Material), false, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();

      // // Optionally: List OBJ files in the folder
      // if (Directory.Exists(selectedFolderPath))
      // {
      //   string folderName = Path.GetFileNameWithoutExtension(selectedFolderPath);
      //   objFiles = Directory.GetFiles(selectedFolderPath, "*.obj");
      //   EditorGUILayout.Space();

      //   if (objFiles.Length == 0) {
      //     GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
      //     labelStyle.normal.textColor = Color.red;
      //     EditorGUILayout.LabelField("Error: No OBJ Files Found!", labelStyle);
      //     return;
      //   }

      //   EditorGUILayout.LabelField("OBJ Files Found:", objFiles.Length.ToString());
        
      //   EditorGUILayout.Space();


      //   // Header
      //   EditorGUILayout.BeginHorizontal();
      //   GUILayout.Label("OBJ File", GUILayout.Width(200));
      //   GUILayout.Label("Material", GUILayout.Width(200));
      //   EditorGUILayout.EndHorizontal();

      //   scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
      //   foreach (var item in objImportList)
      //   {
      //       EditorGUILayout.BeginHorizontal();
      //       EditorGUILayout.LabelField(item.fileName, GUILayout.Width(200));
      //       item.assignedMaterial = (Material)EditorGUILayout.ObjectField(item.assignedMaterial, typeof(Material), false, GUILayout.Width(200));
      //       EditorGUILayout.EndHorizontal();
      //   }
      //   EditorGUILayout.EndScrollView();

      // Only import when the button is pressed!
      GUI.enabled = !shouldImport;
      if (GUILayout.Button("Import OBJs"))
      {
          shouldImport = true;
      }
      // Import outside of GUI layout flow
      if (shouldImport)
      {
          shouldImport = false;
          Debug.Log("IMPORT!");
          EditorCoroutineUtility.StartCoroutine(ImportFiles(), this);
        //   ImportFiles();
      }
    }
  }

  void ScanAndGroupObjFiles()
  {
    objImportGroups.Clear();
    objCount = 0;
    //   string[] objFiles = Directory.GetFiles(selectedFolderPath, "*.obj");
    List<OpenGolfSimOBJImport.ImportedMesh> importedMeshes = OpenGolfSimOBJImport.ImportOBJ(selectedFolderPath);
    Dictionary<string, ObjImportGroup> groupDict = new Dictionary<string, ObjImportGroup>();
    foreach (OpenGolfSimOBJImport.ImportedMesh mesh in importedMeshes) {
        Debug.Log($"mesh: {mesh.name}");
        string[] parts = mesh.name.Split('_');
        string prefix = (parts.Length > 1) ? parts[0] : mesh.name;

    // }
    // return;
    //   // Group by prefix (before the first underscore)
    //   foreach (var objPath in objFiles)
    //   {
    //       string fileName = Path.GetFileNameWithoutExtension(objPath).ToLower();
    //       string[] parts = fileName.Split('_');
    //       string prefix = (parts.Length > 1) ? parts[0] : fileName;

          // Find/create group
          if (!groupDict.TryGetValue(prefix, out ObjImportGroup group))
          {
              group = new ObjImportGroup { prefix = prefix };
              // Assign default materials by prefix (customize as needed)
              if (prefix.Contains("fairway")) {
                  group.assignedMaterial = fairwayMaterial;
              } else if (prefix.Contains("green")) {
                  group.assignedMaterial = greenMaterial;
              } else if (prefix.Contains("rough")) {
                  group.assignedMaterial = roughMaterial;
              } else if (prefix.Contains("first")) {
                  group.assignedMaterial = firstCutMaterial;
              } else if (prefix.Contains("fringe")) {
                  group.assignedMaterial = fringeMaterial;
              } else if (prefix.Contains("tee")) {
                  group.assignedMaterial = teeMaterial;
              } else if (prefix.Contains("sand")) {
                  group.assignedMaterial = sandMaterial;
              } else if (prefix.Contains("river")) {
                  group.assignedMaterial = riverMaterial;
              }

              groupDict[prefix] = group;
          }
          group.objMeshes.Add(mesh);
        //   group.objPaths.Add(objPath);
          objCount++;
      }
      objImportGroups = new List<ObjImportGroup>(groupDict.Values);
  }
  
  public static void EnableMeshReadWrite(string assetPath)
  {
    ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
    if (importer != null)
    {
        importer.isReadable = true; // Enable Read/Write
        importer.SaveAndReimport(); // Reimport to apply
        Debug.Log($"Enabled Read/Write for mesh at: {assetPath}");
    }
    else
    {
        Debug.LogWarning($"Could not find ModelImporter for asset: {assetPath}");
    }
  }
  
  IEnumerator ImportFiles() {
    yield return null;
    
    string folderName = Path.GetFileNameWithoutExtension(selectedFolderPath);
    GameObject parentObj = new GameObject(folderName);

    foreach (var group in objImportGroups) {
        
        foreach (var objMesh in group.objMeshes) {
            GameObject obj = new GameObject(objMesh.name);
            obj.transform.SetParent(parentObj.transform);

            MeshFilter mf = obj.AddComponent<MeshFilter>();
            // Mesh mesh = OpenGolfSimOBJImport.ImportOBJ(objPath);
            // mesh.name = objMesh.name;
            mf.mesh = objMesh.mesh;

            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            if (group.assignedMaterial != null) {
                meshRenderer.sharedMaterial = group.assignedMaterial;
            }
            
            MeshCollider collider = obj.AddComponent<MeshCollider>();
            collider.sharedMesh = null; // clear first!
            collider.sharedMesh = objMesh.mesh;

        }
    }
  }
//   IEnumerator ImportFiles() {
//     yield return null;
//     int progressId = Progress.Start("Starting import...");

//     string folderName = Path.GetFileNameWithoutExtension(selectedFolderPath);
//     GameObject parentObj = new GameObject(folderName);

//     int finished = 0;
//     foreach (var group in objImportGroups) {


//         foreach (var objPath in group.objPaths) {
//             string fileName = Path.GetFileNameWithoutExtension(objPath);
//             // EditorUtility.DisplayProgressBar("Importing", $"Processing {finished} of {objImportGroups.Count} groups", finished / objImportGroups.Count);
//             float percent = (float)finished / (float)objCount;
//             string progressLabel = $"Importing {fileName} ({finished + 1} of {objCount}, {Mathf.Round(percent * 100)}%)";
//             // Debug.Log(progressLabel);

//             Progress.Report(progressId, percent, progressLabel);

//             yield return null;

//             // Debug.Log($"Importing mesh: {fileName}, {percent}");
//             // string fileName = Path.GetFileName(objPath);

//             // EditorGUILayout.LabelField(fileName);
//             // Inside your foreach loop over obj files:

//             // Skip if already exists
//             // if (GameObject.Find(fileName) != null)
//             //   continue;
//             // Debug.Log($"Importing mesh: {fileName}");

//             // string destPathAbsolute = Path.Combine(importFolderAbsolute, fileName);
//             // string destPathRelative = Path.Combine(importFolderRelative, fileName);


//             GameObject obj = new GameObject(fileName);
//             obj.transform.SetParent(parentObj.transform);

//             MeshFilter mf = obj.AddComponent<MeshFilter>();
//             Mesh mesh = OpenGolfSimOBJImport.ImportOBJ(objPath);
//             mesh.name = fileName;
//             mf.mesh = mesh;

//             MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
//             meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
//             if (group.assignedMaterial != null) {
//                 meshRenderer.sharedMaterial = group.assignedMaterial;
//             }
            
//             MeshCollider collider = obj.AddComponent<MeshCollider>();
//             collider.sharedMesh = null; // clear first!
//             collider.sharedMesh = mesh;

//             finished += 1;
//         }
//     }
//     Debug.Log("Import finished");
//     // EditorUtility.ClearProgressBar();
//     Progress.Remove(progressId);

//     Vector3 eulerAngles = new Vector3(180, 0, 0); // 90 degrees around the Y-axis
//     parentObj.transform.rotation = Quaternion.Euler(eulerAngles);
//     parentObj.transform.position = new Vector3(0, 0, 0);
//   }


}
