using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auto-build settings - Automatically adds scenes to build settings
/// </summary>
[InitializeOnLoad]
public class AutoBuildSettings
{
    static AutoBuildSettings()
    {
        EditorApplication.update += CheckBuildSettings;
    }

    static void CheckBuildSettings()
    {
        EditorApplication.update -= CheckBuildSettings;

        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        bool changed = false;

        // Check for senlin scene
        bool hasSenlin = false;
        foreach (var scene in currentScenes)
        {
            if (scene.path.Contains("senlin"))
            {
                hasSenlin = true;
                break;
            }
        }

        if (!hasSenlin)
        {
            string senlinPath = "Assets/Scenes/senlin.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(senlinPath) != null)
            {
                AddSceneToBuildSettings(senlinPath);
                changed = true;
                Debug.Log("[AutoBuildSettings] Added senlin scene to build settings");
            }
        }

        // Check for SampleScene
        bool hasSampleScene = false;
        foreach (var scene in currentScenes)
        {
            if (scene.path.Contains("SampleScene"))
            {
                hasSampleScene = true;
                break;
            }
        }

        if (!hasSampleScene)
        {
            string samplePath = "Assets/Scenes/SampleScene.unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(samplePath) != null)
            {
                AddSceneToBuildSettings(samplePath);
                changed = true;
                Debug.Log("[AutoBuildSettings] Added SampleScene to build settings");
            }
        }

        if (changed)
        {
            EditorUtility.DisplayDialog("Build Settings Updated", 
                "Scenes have been added to build settings:\n" +
                "- senlin\n" +
                "- SampleScene", "OK");
        }
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
        System.Array.Resize(ref currentScenes, currentScenes.Length + 1);
        currentScenes[currentScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = currentScenes;
    }

    [MenuItem("Tools/Witch of the Wood/Update Build Settings")]
    static void UpdateBuildSettings()
    {
        CheckBuildSettings();
    }
}