using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OGSTreePlanter))]
public class OpenGolfSimTreePlanerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw normal inspector
        DrawDefaultInspector();

        // Reference to the target script
        var script = (OGSTreePlanter)target;

        // Button to regenerate trees
        if (GUILayout.Button("Regenerate Trees"))
        {
            // Calls the planting method. Mark scene dirty so it's saved.
            script.Regenerate();
            EditorUtility.SetDirty(script);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
    }
}