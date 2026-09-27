using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

public static class QuestProjectSetup
{
    const string ScenePath = "Assets/Scenes/QuestStarter.unity";
    const BuildTargetGroup Platform = BuildTargetGroup.Android;

    [MenuItem("Quest Vision/Configure Quest 2 and Quest 3")]
    public static void Configure()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            throw new InvalidOperationException("Switch to the Android build target first.");

        PlayerSettings.companyName = "Deniz Karya";
        PlayerSettings.productName = "Quest Vision";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.denizkarya.questvision");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)32;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
        PlayerSettings.Android.preferredInstallLocation = AndroidPreferredInstallLocation.Auto;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
        PlayerSettings.MTRendering = true;
        PlayerSettings.graphicsJobs = true;
        PlayerSettings.graphicsJobMode = GraphicsJobMode.Legacy;
        PlayerSettings.stereoRenderingPath = StereoRenderingPath.Instancing;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 4;
        QualitySettings.pixelLightCount = 1;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        Directory.CreateDirectory("Assets/XR/Settings");
        AssetDatabase.Refresh();
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget allSettings) || allSettings == null)
        {
            allSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(allSettings, "Assets/XR/Settings/XRGeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, allSettings, true);
        }
        if (!allSettings.HasManagerSettingsForBuildTarget(Platform))
            allSettings.CreateDefaultManagerSettingsForBuildTarget(Platform);
        var general = allSettings.SettingsForBuildTarget(Platform);
        general.InitManagerOnStart = true;
        var manager = allSettings.ManagerSettingsForBuildTarget(Platform);
        manager.automaticLoading = true;
        manager.automaticRunning = true;
        if (!XRPackageMetadataStore.AssignLoader(manager, "UnityEngine.XR.OpenXR.OpenXRLoader", Platform))
            throw new InvalidOperationException("Could not assign the Android OpenXR loader.");
        EditorUtility.SetDirty(allSettings);
        EditorUtility.SetDirty(general);
        EditorUtility.SetDirty(manager);

        FeatureHelpers.RefreshFeatures(Platform);
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(Platform);
        openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        openXR.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
        Enable("com.unity.openxr.feature.metaquest");
        Enable("com.meta.openxr.feature.metaxr");
        Enable("com.unity.openxr.feature.input.handtrackingsubsystem");
        EnableFeature(openXR.GetFeature<OculusTouchControllerProfile>());
        EnableFeature(openXR.GetFeature<MetaQuestTouchPlusControllerProfile>());
        EnableFeature(openXR.GetFeature<Meta.XR.MetaXRSubsampledLayout>());
        EditorUtility.SetDirty(openXR);

        var meta = OVRProjectConfig.CachedProjectConfig;
        meta.targetDeviceTypes = new List<OVRProjectConfig.DeviceType>
        {
            OVRProjectConfig.DeviceType.Quest2,
            OVRProjectConfig.DeviceType.Quest3,
            OVRProjectConfig.DeviceType.Quest3S
        };
        meta.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
        meta.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
        meta.sceneSupport = OVRProjectConfig.FeatureSupport.Supported;
        meta.systemLoadingScreenBackground = OVRProjectConfig.SystemLoadingScreenBackground.ContextualPassthrough;
        // The starter scene uses no camera or room data. Enable permission only
        // when adding a feature that actually needs it and requests consent.
        meta.isPassthroughCameraAccessEnabled = false;
        OVRProjectConfig.CommitProjectConfig(meta);
        OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);

        CreateStarterScene();
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("QUEST_CONFIGURE: complete");
        Validate();
        Directory.CreateDirectory("Builds");
        OVRProjectSetupCLI.GenerateProjectSetupReport();
    }

    static void Enable(string id) => EnableFeature(FeatureHelpers.GetFeatureWithIdForBuildTarget(Platform, id));

    static void EnableFeature(OpenXRFeature feature)
    {
        if (feature == null) throw new InvalidOperationException("An expected OpenXR feature is missing.");
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    static void CreateStarterScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Materials");
        AssetDatabase.Refresh();
        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
        if (prefab == null) throw new InvalidOperationException("Meta XR camera rig prefab is missing.");
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var ovrManager = rig.GetComponent<OVRManager>();
        ovrManager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
        PrefabUtility.RecordPrefabInstancePropertyModifications(ovrManager);
        foreach (var camera in rig.GetComponentsInChildren<Camera>())
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.04f, 0.075f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.GetComponent<Renderer>().sharedMaterial = Material("Floor", new Color(0.12f, 0.15f, 0.2f));
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Starter Cube";
        cube.transform.position = new Vector3(0, 1.4f, 2);
        cube.transform.localScale = Vector3.one * 0.4f;
        cube.GetComponent<Renderer>().sharedMaterial = Material("Cube", new Color(0.12f, 0.65f, 0.95f));
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);
        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static Material Material(string name, Color color)
    {
        var material = new Material(Shader.Find("Standard")) { name = name, color = color };
        AssetDatabase.CreateAsset(material, $"Assets/Materials/{name}.mat");
        return material;
    }

    [MenuItem("Quest Vision/Validate OpenXR")]
    public static void Validate()
    {
        var issues = new List<OpenXRFeature.ValidationRule>();
        OpenXRProjectValidation.GetCurrentValidationIssues(issues, Platform);
        foreach (var issue in issues)
            Debug.Log($"QUEST_VALIDATION: {(issue.error ? "ERROR" : "recommendation")}: {issue.message}");
        if (issues.Any(issue => issue.error))
            throw new InvalidOperationException("Resolve the OpenXR validation errors before building.");
        Debug.Log("QUEST_VALIDATION: no blocking errors");
    }

    [MenuItem("Quest Vision/Build Android APK")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Validate();
        Directory.CreateDirectory("Builds");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/QuestVision.apk",
            target = BuildTarget.Android,
            options = BuildOptions.Development
        });
        var result = report.summary;
        var summary = $"QUEST_BUILD: {result.result}; errors={result.totalErrors}; warnings={result.totalWarnings}; bytes={result.totalSize}";
        File.WriteAllText("Builds/build-summary.txt", summary + Environment.NewLine);
        Debug.Log(summary);
        if (result.result != BuildResult.Succeeded)
            throw new BuildFailedException(summary);
    }

    public static void ConfigureAndBuild()
    {
        Configure();
        Build();
    }
}
