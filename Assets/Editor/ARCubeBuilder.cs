using System;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ARCubeBuilder
{
    const string ScenePath = "Assets/Scenes/ARCube.unity";
    // AR Cube in the Trustworthy AI-enhanced IoT Lab Meta developer team.
    const string MetaAppId = "1305150979358479";

    [MenuItem("Quest Vision/Build AR Cube")]
    public static void Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Switch to the Android build target first.");

        PlayerSettings.productName = "AR Cube";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.denizkarya.arcube");
        PlayerSettings.bundleVersion = "1.4.2";
        PlayerSettings.Android.bundleVersionCode = 7;
        // GameActivity fails to present TouchScreenKeyboard on this Quest.
        // https://developers.meta.com/horizon/feedback/vr/investigations/2279537085906028/
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        PlayerSettings.SplashScreen.show = false;

        var config = OVRProjectConfig.CachedProjectConfig;
        config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
        // Composited passthrough does not require access to raw camera images.
        config.isPassthroughCameraAccessEnabled = false;
        config.requiresSystemKeyboard = true;
        config.systemLoadingScreenBackground = OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough;
        OVRProjectConfig.CommitProjectConfig(config);
        OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);
        ConfigureAndroidLauncher();

        // Refresh the runtime script's importer after a cached project is copied.
        AssetDatabase.ImportAsset("Assets/Scripts/WalkAroundCube.cs", ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        var runtimeScript = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/WalkAroundCube.cs");
        Debug.Log("AR_CUBE_SCRIPT: " + (runtimeScript == null ? "asset missing" : runtimeScript.GetClass()?.FullName ?? "class missing") +
            "; GUID=" + AssetDatabase.AssetPathToGUID("Assets/Scripts/WalkAroundCube.cs"));

        if (!File.Exists(ScenePath))
            CreateScene();
        else if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(ScenePath);
        }
        ARCubeColours.ApplyToOpenScene();
        ARCubeGuideBuilder.ApplyToOpenScene();
        ARCubeTextBuilder.ApplyToOpenScene();
        ARCubeControlsChecks.Validate();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        QuestProjectSetup.Validate();
        OVRProjectSetupCLI.GenerateProjectSetupReport();

        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/ARCube.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        var summary = report.summary;
        string result = $"AR_CUBE_BUILD: {summary.result}; errors={summary.totalErrors}; warnings={summary.totalWarnings}";
        File.WriteAllText("Builds/ar-cube-build-summary.txt", result + Environment.NewLine);
        Debug.Log(result);
        if (summary.result != BuildResult.Succeeded)
            throw new BuildFailedException(result);
    }

    static void ConfigureAndroidLauncher()
    {
        const string path = "Assets/Plugins/Android/AndroidManifest.xml";
        const string android = "http://schemas.android.com/apk/res/android";
        var manifest = new XmlDocument();
        manifest.Load(path);
        var activity = (XmlElement)manifest.SelectSingleNode("/manifest/application/activity");
        if (activity == null) throw new BuildFailedException("Android activity is missing.");
        activity.SetAttribute("name", android, "com.unity3d.player.UnityPlayerActivity");
        activity.SetAttribute("theme", android, "@style/UnityThemeSelector");
        // Set both launcher labels explicitly rather than relying on inheritance.
        activity.SetAttribute("label", android, "@string/app_name");
        foreach (XmlElement intent in activity.SelectNodes("intent-filter"))
            intent.SetAttribute("label", android, "@string/app_name");
        var application = (XmlElement)manifest.SelectSingleNode("/manifest/application");
        var namespaces = new XmlNamespaceManager(manifest.NameTable);
        namespaces.AddNamespace("android", android);
        var identity = (XmlElement)application.SelectSingleNode("meta-data[@android:name='com.oculus.app_id']", namespaces)
            ?? manifest.CreateElement("meta-data");
        identity.SetAttribute("name", android, "com.oculus.app_id");
        // An escaped space preserves the long numeric ID as an Android string.
        identity.SetAttribute("value", android, @"\ " + MetaAppId);
        application.AppendChild(identity);
        manifest.Save(path);
    }

    static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
        var rigObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var rig = rigObject.GetComponent<OVRCameraRig>();
        var manager = rigObject.GetComponent<OVRManager>();
        manager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
        manager.isInsightPassthroughEnabled = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(manager);

        foreach (var camera in rigObject.GetComponentsInChildren<Camera>(true))
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);

        // Meta XR SDK 207 renders this component as a background underlay.
        var passthrough = new GameObject("Room Passthrough").AddComponent<OVRPassthroughLayer>();
        passthrough.textureOpacity = 1;
        passthrough.hidden = false;

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Walk Around Cube";
        cube.transform.position = new Vector3(0, 1.2f, 1.5f);
        cube.transform.localScale = Vector3.one * 0.45f;
        var material = new Material(Shader.Find("Standard"))
        {
            name = "AR Cube Blue",
            color = new Color(0.035f, 0.5f, 0.95f, 1)
        };
        material.SetFloat("_Glossiness", 0.25f);
        AssetDatabase.CreateAsset(material, "Assets/Materials/ARCubeBlue.mat");
        cube.GetComponent<Renderer>().sharedMaterial = material;
        cube.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        cube.GetComponent<Renderer>().receiveShadows = false;

        var light = new GameObject("Cube Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(40, -35, 0);
        light.shadows = LightShadows.None;

        var controller = new GameObject("AR Cube App").AddComponent<WalkAroundCube>();
        controller.cameraRig = rig;
        controller.cube = cube.transform;
        controller.passthrough = passthrough;
        EditorSceneManager.SaveScene(scene, ScenePath);
    }
}
