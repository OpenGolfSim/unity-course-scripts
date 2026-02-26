using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;


[Serializable]
public class CourseDataFile
{
    public string name;
    public string slug;
    public int version;
    public string description;
    public int gameMode;
    public int engine;
}

public class OpenGolfSimExportWindow : EditorWindow
{
  private int selectedTab = 0;
  private string[] tabNames = { "Course Settings", "Export Course", "Help" };

  private string courseTitle = "";
  private string courseDescription = "";
  int selectedPlatformIndex = 0;
  int selectedGameMode = 2;
  int selectedBundleIndex = 0;
  private string selectedFolderPath = "";
  // string[] platformNames = {"default (Build Target)", "win64", "osx"};
  bool enablePlatformWindows = true;
  bool enablePlatformMacOS = true;
  bool enablePlatformiOS = false;
  bool enablePlatformAndroid = false;
  bool enablePlatformWebGL = false;
  // string[] platformNames = {"win64", "macos"};
  string[] gameModes = {"Range", "Range Game", "Course"};
  string[] bundleNames;
  bool emptyBundleNames = true;
  bool startExport = false;
  bool isExporting = false;
  private Dictionary<GameObject, bool> cameraObjectStates = new Dictionary<GameObject, bool>();
  private bool camerasDeactivated = false;

  private void OnEnable()
  {
      RefreshBundleNames();
      if (courseTitle == "") {
        courseTitle = PlayerSettings.productName;
      }
  }

  private void OnGUI()
  {
    // string version = PlayerSettings.bundleVersion;

    GUILayout.BeginHorizontal();
    GUILayout.Space(10);
    GUILayout.BeginVertical();

    // GUILayout.FlexibleSpace();
    GUILayout.Space(40);
    GUIStyle customLabelStyle = new GUIStyle(); 
    customLabelStyle.fontSize = 16; // Set to your desired font size
    customLabelStyle.fontStyle = FontStyle.Bold; // Set to your desired font size
    customLabelStyle.normal.textColor = Color.white; // Example: set text color to red

    GUILayout.Label("Export Course", customLabelStyle);
    GUILayout.EndVertical();

    // GUILayout.FlexibleSpace();
    Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(OpenGolfSimConstants.BannerPath, typeof(Texture));
    GUILayout.Box(banner, GUILayout.Width(240), GUILayout.Height(60));
    GUILayout.EndHorizontal();

    // Draw a horizontal line using a custom style
    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); 

    GUILayout.Space(30);

    
    if (startExport) {
      
      GUIStyle centeredStyle = new GUIStyle();
      // Set the text alignment to middle center
      centeredStyle.alignment = TextAnchor.MiddleCenter;
      centeredStyle.fontSize = 24;
      centeredStyle.normal.textColor = Color.white;
      centeredStyle.padding = new RectOffset(0, 0, 20, 20);
      GUILayout.Label("Exporting...", centeredStyle);


      if (!isExporting) {
        isExporting = true;
        Debug.Log("Export start");
        // data.description = courseDescription;
        EditorCoroutineUtility.StartCoroutine(ExportAll(), this);
      }
      return;
    }

    GUILayout.BeginVertical("Box");

    GUILayout.Label("Build Settings", EditorStyles.boldLabel);
    // if (GUILayout.Button("Select Option"))
    // {
    //     GenericMenu menu = new GenericMenu();
    //     menu.AddItem(new GUIContent("Option 1"), false, OnOptionSelected, "Option1");
    //     menu.AddItem(new GUIContent("Option 2"), true, OnOptionSelected, "Option2"); // true for selected
    //     menu.ShowAsContext();
    // }    
    
    EditorGUILayout.Space(20);

    GUILayout.BeginHorizontal();
    GUILayout.Label($"Course Title", GUILayout.Width(200));
    // use the product name as the default course title
    courseTitle = GUILayout.TextField(courseTitle);
    GUILayout.EndHorizontal();

    // GUILayout.FlexibleSpace();

    EditorGUILayout.Space(10);
    
    // selectedPlatformIndex = GUILayout.SelectionGrid(selectedPlatformIndex, platformNames, 1);
    GUILayout.BeginHorizontal();
    GUILayout.Label("Course Slug (AssetBundle)", GUILayout.Width(200));

    selectedBundleIndex = EditorGUILayout.Popup(selectedBundleIndex, bundleNames);
    GUILayout.EndHorizontal();

    EditorGUILayout.Space(10);

    GUILayout.BeginHorizontal();
    GUILayout.Label($"Platforms", GUILayout.Width(200));


    GUILayout.BeginVertical();
    enablePlatformWindows = EditorGUILayout.Toggle("Windows", enablePlatformWindows);
    enablePlatformMacOS = EditorGUILayout.Toggle("MacOS", enablePlatformMacOS);
    
    if (IsUniversal()) {
      enablePlatformiOS = EditorGUILayout.Toggle("iOS", enablePlatformiOS);
      enablePlatformAndroid = EditorGUILayout.Toggle("Android", enablePlatformAndroid);
      enablePlatformWebGL = EditorGUILayout.Toggle("Web", enablePlatformWebGL);
    }
    GUILayout.EndVertical();
    
    GUILayout.EndHorizontal();

    if (emptyBundleNames) {
      GUIStyle customStyle = new GUIStyle(); 
      customStyle.fontSize = 11; // Set to your desired font size
      // customStyle.fontStyle = FontStyle.Bold; // Set to your desired font size
      customStyle.normal.textColor = Color.red; // Example: set text color to red
      customStyle.wordWrap = true;

      GUILayout.Label(
        "AssetBundle missing! Before you can export your course, you need to set a unique AssetBundle name on the scene you want to export as your course.",
        customStyle
      );
    }
    // selectedPlatformIndex = EditorGUILayout.Popup("Platform", selectedPlatformIndex, platformNames);

    EditorGUILayout.Space(10);
    
    GUILayout.BeginHorizontal();
    GUILayout.Label("Game Mode", GUILayout.Width(200));
    selectedGameMode = EditorGUILayout.Popup(selectedGameMode, gameModes);
    GUILayout.EndHorizontal();

    EditorGUILayout.Space(10);
    
    GUILayout.BeginHorizontal();
    GUILayout.Label("Output Folder", GUILayout.Width(200));
    if (string.IsNullOrEmpty(selectedFolderPath))
    {
      if (GUILayout.Button("Select Output Folder"))
      {
          string folderPath = EditorUtility.SaveFolderPanel("Select Output Folder", selectedFolderPath, bundleNames[selectedBundleIndex]);
          if (!string.IsNullOrEmpty(folderPath))
          {
              selectedFolderPath = folderPath;
          }
      }
    } else {
      GUILayout.Label(selectedFolderPath);
      if (GUILayout.Button("Clear"))
      {
        selectedFolderPath = "";
      }
    }
    GUILayout.EndHorizontal();


    GUILayout.FlexibleSpace();

    
    GUI.enabled = !string.IsNullOrEmpty(selectedFolderPath) && !emptyBundleNames;


    if (GUILayout.Button("Export"))
    {
        if (string.IsNullOrEmpty(selectedFolderPath))
        {
          Debug.LogError("Please select output folder first!");
        }
        else
        {
          // Debug.Log("platformName: " + platformNames[selectedPlatformIndex]);
          // Debug.Log("bundleName: " + bundleNames[selectedBundleIndex]);
          Debug.Log("selectedFolderPath: " + selectedFolderPath);
          if (!startExport)
          {
            startExport = true;
          }
        }
    }
    // Restore enabled state for other controls
    GUI.enabled = true;

    GUILayout.EndVertical();


    // if (GUILayout.Button("Select Folder"))
    // {
    //     string folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
    //     if (!string.IsNullOrEmpty(folderPath))
    //     {
    //         selectedFolderPath = folderPath;
    //         ScanAndGroupObjFiles();
    //     }
    // }
  }

  private bool IsUniversal()
  {
    var renderPipelineAsset = GraphicsSettings.currentRenderPipeline;
    if (renderPipelineAsset == null) {
      return false;
    }
    return renderPipelineAsset.GetType().Name.Contains("UniversalRenderPipeline");
  }

  private void RefreshBundleNames()
  {
      bundleNames = AssetDatabase.GetAllAssetBundleNames();
      if (bundleNames.Length == 0)
      {
        emptyBundleNames = true;
        bundleNames = new string[] { "No AssetBundles found" };
      } else {
        emptyBundleNames = false;
      }
      selectedPlatformIndex = 0;
      selectedBundleIndex = 0;
  }

  IEnumerator ExportAll() {
    yield return null;
    // export one per platform
    string bundleName = bundleNames[selectedBundleIndex];
    
    DisableAllCameras();
    yield return null;
    if (enablePlatformWindows) {
      ExportStart("win64", bundleName);
    }
    if (enablePlatformMacOS) {
      ExportStart("macos", bundleName);
    }
    if (enablePlatformiOS) {
      ExportStart("ios", bundleName);
    }
    if (enablePlatformAndroid) {
      ExportStart("android", bundleName);
    }
    if (enablePlatformWebGL) {
      ExportStart("webgl", bundleName);
    }
    yield return null;
    // foreach (var platform in platformNames)

    CourseDataFile data = new CourseDataFile();
    data.slug = bundleName.ToLower();
    data.version = 1;
    data.description = $"This course file was generated with OGS Developer Toolkit";
    data.name = courseTitle;
    data.gameMode = selectedGameMode;
    data.engine = 0;
    
    if (IsUniversal()) {
      data.engine = 1;
    }

    string jsonString = JsonUtility.ToJson(data, true);

    string courseJSONFilePath = Path.Combine(selectedFolderPath, "course.json");
    File.WriteAllText(courseJSONFilePath, jsonString);
    Debug.Log($"Export Complete! Wrote course file to: {selectedFolderPath}");
    
    RestoreCameraStates();

    isExporting = false;
    startExport = false;    
  }

  void ExportStart(string targetPlatform, string bundleName) {
    

    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    if (targetPlatform == "win64") {
      target = BuildTarget.StandaloneWindows64;
    } else if (targetPlatform == "macos") {
      target = BuildTarget.StandaloneOSX;
    } else if (targetPlatform == "ios") {
      target = BuildTarget.iOS;
    } else if (targetPlatform == "android") {
      target = BuildTarget.Android;
    } else if (targetPlatform == "webgl") {
      target = BuildTarget.WebGL;
    }
    // string assetBundleDirectory = "Assets/AssetBundles";
    // Debug.Log("Export asset bundle to: " + assetBundleDirectory);
    // if (!Directory.Exists(assetBundleDirectory))
    // {
    //     Directory.CreateDirectory(assetBundleDirectory);
    // }

    List<AssetBundleBuild> builds = new List<AssetBundleBuild>();
    
    var assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(bundleName);
    AssetBundleBuild build = new AssetBundleBuild();
    build.assetBundleName = $"{bundleName}.{targetPlatform}.unity3d";
    build.assetNames = assetPaths;
    builds.Add(build);
    Debug.Log("assetBundle to build:" + build.assetBundleName);

    BuildPipeline.BuildAssetBundles(selectedFolderPath, builds.ToArray(), BuildAssetBundleOptions.None, target);


    // // Build all AssetBundles
    // BuildPipeline.BuildAssetBundles(
    //     assetBundleDirectory,
    //     BuildAssetBundleOptions.None,
    //     target
    // );
    Debug.Log($"AssetBundles built with target {target} to: " + selectedFolderPath);

    
  }

  public void DisableAllCameras()
  {
    cameraObjectStates.Clear();
    foreach (Camera cam in Camera.allCameras)
    {
      GameObject camObj = cam.gameObject;
      cameraObjectStates[camObj] = camObj.activeSelf;
      camObj.SetActive(false);
    }
    camerasDeactivated = true;
  }

  /// <summary>
  /// Restores the enabled state of cameras as recorded during disabling.
  /// </summary>
  public void RestoreCameraStates()
  {
    if (!camerasDeactivated) return;

    foreach (var entry in cameraObjectStates)
    {
      if (entry.Key != null)
      {
        entry.Key.SetActive(entry.Value);
      }
    }
    cameraObjectStates.Clear();
    camerasDeactivated = false;
  }
}