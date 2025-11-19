using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;


[Serializable]
public class CourseDataFile
{
    public string name;
    public string course;
    public string version;
    public string description;
    public int gameMode;
}

public class OpenGolfSimExportWindow : EditorWindow
{
  private int selectedTab = 0;
  private string[] tabNames = { "Course Settings", "Export Course", "Help" };

  private string courseDescription = "";
  int selectedPlatformIndex = 0;
  int selectedGameMode = 0;
  int selectedBundleIndex = 0;
  private string selectedFolderPath = "";
  // string[] platformNames = {"default (Build Target)", "win64", "osx"};
  string[] platformNames = {"win64", "osx"};
  string[] gameModes = {"Range", "Range Game", "Course"};
  string[] bundleNames;
  bool startExport = false;
  bool isExporting = false;

  private void OnEnable()
  {
      RefreshBundleNames();
  }

  private void OnGUI()
  {
    CourseDataFile data = new CourseDataFile();
    // string version = PlayerSettings.bundleVersion;

    GUILayout.BeginHorizontal();
    GUILayout.FlexibleSpace(); // Pushes the content to the center
    Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(OpenGolfSimConstants.BannerPath, typeof(Texture));
    GUILayout.Box(banner, GUILayout.Width(400), GUILayout.Height(100));
    GUILayout.FlexibleSpace();
    GUILayout.EndHorizontal();
    GUILayout.Space(30);


    if (startExport) {
      GUIStyle centeredStyle = new GUIStyle();
      // Set the text alignment to middle center
      centeredStyle.alignment = TextAnchor.MiddleCenter;
      centeredStyle.fontSize = 24;
      centeredStyle.normal.textColor = Color.white;
      centeredStyle.padding = new RectOffset(0, 0, 20, 20);

      // EditorStyles.boldLabel;

      GUILayout.Label("Exporting...", centeredStyle);
      if (!isExporting) {
        isExporting = true;
        Debug.Log("Export start");
        data.description = courseDescription;
        ExportStart(platformNames[selectedPlatformIndex], bundleNames[selectedBundleIndex], data);
      }
      return;
    }

    GUILayout.BeginVertical("Box");

    GUILayout.Label("Platform", EditorStyles.boldLabel);
    // if (GUILayout.Button("Select Option"))
    // {
    //     GenericMenu menu = new GenericMenu();
    //     menu.AddItem(new GUIContent("Option 1"), false, OnOptionSelected, "Option1");
    //     menu.AddItem(new GUIContent("Option 2"), true, OnOptionSelected, "Option2"); // true for selected
    //     menu.ShowAsContext();
    // }    
    
    
    // selectedPlatformIndex = GUILayout.SelectionGrid(selectedPlatformIndex, platformNames, 1);
    selectedBundleIndex = EditorGUILayout.Popup("AssetBundle", selectedBundleIndex, bundleNames);
    selectedPlatformIndex = EditorGUILayout.Popup("Platform", selectedPlatformIndex, platformNames);
    
    selectedGameMode = EditorGUILayout.Popup("Game Mode", selectedGameMode, gameModes);

    EditorGUILayout.Space();
    if (GUILayout.Button("Select Output Folder"))
    {
        string folderPath = EditorUtility.SaveFolderPanel("Select Output Folder", selectedFolderPath, bundleNames[selectedBundleIndex]);
        if (!string.IsNullOrEmpty(folderPath))
        {
            selectedFolderPath = folderPath;
        }
    }
    GUILayout.Label(selectedFolderPath);

    
    EditorGUILayout.Space();
    
    GUILayout.Label("Course File", EditorStyles.boldLabel);
    GUILayout.Label($"Name:", GUILayout.Width(50));
    GUILayout.Label(PlayerSettings.productName, GUILayout.Width(150));

    GUILayout.Label($"Version: (change in player settings)", GUILayout.Width(50));
    GUILayout.Label(PlayerSettings.bundleVersion, GUILayout.Width(150));

    EditorGUILayout.Space();
    GUILayout.Label("Course Description: ");
    // courseDescription = GUILayout.TextField(courseDescription, GUILayout.Width(150));
    courseDescription = GUILayout.TextArea(courseDescription, GUILayout.Height(100));


    EditorGUILayout.Space();

    
    GUI.enabled = !string.IsNullOrEmpty(selectedFolderPath);

    if (GUILayout.Button("Export"))
    {
        if (string.IsNullOrEmpty(selectedFolderPath))
        {
          Debug.LogError("Please select output folder first!");
        }
        else
        {
          Debug.Log("platformName: " + platformNames[selectedPlatformIndex]);
          Debug.Log("bundleName: " + bundleNames[selectedBundleIndex]);
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

  private void RefreshBundleNames()
  {
      bundleNames = AssetDatabase.GetAllAssetBundleNames();
      if (bundleNames.Length == 0)
      {
          bundleNames = new string[] { "No AssetBundles found" };
      }
      selectedPlatformIndex = 0;
      selectedBundleIndex = 0;
  }

  void ExportStart(string targetPlatform, string bundleName, CourseDataFile data) {
    

    BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
    if (targetPlatform == "win64") {
      target = BuildTarget.StandaloneWindows64;
    } else if (targetPlatform == "osx") {
      target = BuildTarget.StandaloneOSX;
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
    build.assetBundleName = $"{bundleName}.unity3d";
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

    data.course = bundleName;
    data.version = PlayerSettings.bundleVersion;
    data.name = PlayerSettings.productName;
    data.gameMode = selectedGameMode;

    string jsonString = JsonUtility.ToJson(data, true);

    string courseJSONFilePath = Path.Combine(selectedFolderPath, "course.json");
    File.WriteAllText(courseJSONFilePath, jsonString);
    Debug.Log($"Wrote course file (v{data.version}) to: " + courseJSONFilePath);

    isExporting = false;
    startExport = false;
  }
}