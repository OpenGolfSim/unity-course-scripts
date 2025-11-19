using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class OpenGolfSimImportWindow : EditorWindow
{
    private string selectedFolderPath = "";
    private bool shouldImport;
    private string[] objFiles = null;
    private List<ObjImportGroup> objImportGroups = new List<ObjImportGroup>();
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
        fairwayMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/Fairway.mat");
        fringeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/Fringe.mat");
        greenMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/Green.mat");
        roughMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/Rough.mat");
        firstCutMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/FirstCut.mat");
        teeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/TeeBox.mat");
        sandMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/Sand.mat");
        riverMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Grass/SGRiverbeds.mat");
    }
    
    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(OpenGolfSimConstants.BannerPath, typeof(Texture));
        GUILayout.Box(banner, GUILayout.Width(400), GUILayout.Height(100));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(30);


        GUILayout.Label("Select Folder Containing OBJ Files", EditorStyles.boldLabel);

        if (GUILayout.Button("Select Folder"))
        {
            string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if (!string.IsNullOrEmpty(folderPath))
            {
                selectedFolderPath = folderPath;
                ScanAndGroupObjFiles();
            }
        }

    if (!string.IsNullOrEmpty(selectedFolderPath) && objImportGroups.Count > 0)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected Folder:", selectedFolderPath);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Prefix", GUILayout.Width(100));
        GUILayout.Label("Files", GUILayout.Width(300));
        GUILayout.Label("Material", GUILayout.Width(200));
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        foreach (var group in objImportGroups)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(group.prefix, GUILayout.Width(100));
            EditorGUILayout.LabelField(string.Join(", ", group.objPaths.ConvertAll(Path.GetFileName)), GUILayout.Width(300));
            group.assignedMaterial = (Material)EditorGUILayout.ObjectField(group.assignedMaterial, typeof(Material), false, GUILayout.Width(200));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

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
      if (GUILayout.Button("Import OBJs"))
      {
          shouldImport = true;
      }
      // Import outside of GUI layout flow
      if (shouldImport)
      {
          shouldImport = false;
          Debug.Log("IMPORT!");
          ImportFiles();
      }
    }
  }

  void ScanAndGroupObjFiles()
  {
      objImportGroups.Clear();
      string[] objFiles = Directory.GetFiles(selectedFolderPath, "*.obj");

      // Group by prefix (before the first underscore)
      Dictionary<string, ObjImportGroup> groupDict = new Dictionary<string, ObjImportGroup>();
      foreach (var objPath in objFiles)
      {
          string fileName = Path.GetFileNameWithoutExtension(objPath).ToLower();
          string[] parts = fileName.Split('_');
          string prefix = (parts.Length > 1) ? parts[0] : fileName;

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
          group.objPaths.Add(objPath);
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

  void ImportFiles() {
    
    string folderName = Path.GetFileNameWithoutExtension(selectedFolderPath);
    GameObject parentObj = new GameObject(folderName);

      // Copy to Assets/ImportedOBJs if not already present
    //   if (!File.Exists(destPathAbsolute)) {
    //     File.Copy(objPath, destPathAbsolute, true);
    //   }
    // string importFolderRelative = Path.Combine("Assets", folderName);
    // string importFolderAbsolute = Path.Combine(Application.dataPath, folderName);

    // Create folder if it doesn't exist
    // if (!Directory.Exists(importFolderAbsolute)) {
    //     Directory.CreateDirectory(importFolderAbsolute);
    // }
    // Material fairwayMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/FairwayMaterial.mat");
    // Material greenMaterial   = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/GreenMaterial.mat");

    foreach (var group in objImportGroups) {
        foreach (var objPath in group.objPaths) {

            string fileName = Path.GetFileNameWithoutExtension(objPath);
            Debug.Log($"Importing mesh: {fileName}");
            // string fileName = Path.GetFileName(objPath);

            EditorGUILayout.LabelField(fileName);
            // Inside your foreach loop over obj files:

            // Skip if already exists
            // if (GameObject.Find(fileName) != null)
            //   continue;
            Debug.Log($"Importing mesh: {fileName}");

            // string destPathAbsolute = Path.Combine(importFolderAbsolute, fileName);
            // string destPathRelative = Path.Combine(importFolderRelative, fileName);

            Mesh mesh = OpenGolfSimOBJImport.ImportOBJ(objPath);
            mesh.name = fileName;

            // Mesh mesh = ImportOBJ(objPath);
            GameObject obj = new GameObject(fileName);
            obj.transform.SetParent(parentObj.transform);
            MeshFilter mf = obj.AddComponent<MeshFilter>();
            mf.mesh = mesh;

            MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            if (group.assignedMaterial != null) {
                meshRenderer.sharedMaterial = group.assignedMaterial;
            }
            

            MeshCollider collider = obj.AddComponent<MeshCollider>();
            collider.sharedMesh = null; // clear first!
            collider.sharedMesh = mesh;
            // collider.convex = true; // Better for CCD

        }
    }
    Vector3 eulerAngles = new Vector3(180, 0, 0); // 90 degrees around the Y-axis
    parentObj.transform.rotation = Quaternion.Euler(eulerAngles);
    parentObj.transform.position = new Vector3(0, 0.4f, 1230);
  }


}
