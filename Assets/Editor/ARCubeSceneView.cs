using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Makes the AR scene easy to find and inspect without a headset.</summary>
[InitializeOnLoad]
public static class ARCubeSceneView
{
    const string ScenePath = "Assets/Scenes/ARCube.unity";
    const string SessionKey = "QuestVision.ARCubeSceneView.Shown";
    const string RequestPath = "Library/ar-cube-show-request";

    static ARCubeSceneView()
    {
        if (!Application.isBatchMode && !SessionState.GetBool(SessionKey, false))
            EditorApplication.update += ShowWhenReady;
    }

    static void ShowWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= ShowWhenReady;
        SessionState.SetBool(SessionKey, true);
        // Restore the AR scene for an empty editor. Otherwise preserve the
        // user's saved scene choice unless they requested this view explicitly.
        if (!File.Exists(RequestPath) && !string.IsNullOrEmpty(SceneManager.GetActiveScene().path))
            return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                Debug.Log("AR_CUBE_EDITOR: Existing unsaved scene preserved. Use Quest Vision > Show AR Cube in Scene View when ready.");
                return;
            }
        }

        ShowCube();
    }

    [MenuItem("Quest Vision/Show AR Cube in Scene View")]
    public static void ShowCube()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("AR_CUBE_EDITOR: Stop Play mode first to inspect the saved AR scene.");
            return;
        }

        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject cube = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "Walk Around Cube")
            {
                cube = root;
                break;
            }
        }

        if (cube == null)
        {
            Debug.LogError("AR_CUBE_EDITOR: Walk Around Cube is missing from ARCube.unity.");
            return;
        }

        SceneVisibilityManager.instance.Show(cube, true);
        Selection.activeGameObject = cube;
        var view = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        view.in2DMode = false;
        view.sceneLighting = true;
        view.LookAt(cube.transform.position, Quaternion.Euler(20, -35, 0), 1.0f);
        view.Focus();
        view.Repaint();
        File.WriteAllText("Library/ar-cube-scene-view-status.txt",
            "Scene: " + SceneManager.GetActiveScene().path + "\n" +
            "Selected: " + cube.name + "\n" +
            "Active: " + cube.activeInHierarchy + "\n" +
            "Renderer enabled: " + cube.GetComponent<Renderer>().enabled + "\n" +
            "View centred on: " + cube.transform.position + "\n");
        Debug.Log("AR_CUBE_EDITOR: Opened ARCube.unity and centred the Scene view on Walk Around Cube.");
        if (File.Exists(RequestPath))
            File.Delete(RequestPath);
    }
}
