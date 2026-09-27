using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Gives the existing cube six independently coloured, flat faces.</summary>
[InitializeOnLoad]
public static class ARCubeColours
{
    const string ScenePath = "Assets/Scenes/ARCube.unity";
    const string RequestPath = "Library/ar-cube-colour-request";
    const string ResultPath = "Library/ar-cube-colour-result.txt";

    static ARCubeColours()
    {
        if (!Application.isBatchMode)
            EditorApplication.update += ProcessRequestedUpdate;
    }

    static void ProcessRequestedUpdate()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= ProcessRequestedUpdate;
        if (!File.Exists(RequestPath)) return;
        bool build = File.ReadAllText(RequestPath).Trim() == "build";
        File.Delete(RequestPath);
        try
        {
            ApplyColours();
            if (build) ARCubeBuilder.Build();
            ARCubeSceneView.ShowCube();
            File.WriteAllText(ResultPath, build ? "Colour update and APK build completed.\n" : "Colour update completed.\n");
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "Failed: " + exception + "\n");
            Debug.LogException(exception);
        }
    }

    [MenuItem("Quest Vision/Colour Cube Faces")]
    public static void ApplyColours()
    {
        var scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        ApplyToOpenScene();
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    public static void ApplyToOpenScene()
    {
        GameObject cube = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == "Walk Around Cube") cube = root;
        if (cube == null) throw new InvalidOperationException("The AR cube could not be found.");

        if (!AssetDatabase.IsValidFolder("Assets/Meshes"))
            AssetDatabase.CreateFolder("Assets", "Meshes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/CubeFaces"))
            AssetDatabase.CreateFolder("Assets/Materials", "CubeFaces");

        const string meshPath = "Assets/Meshes/ARCubeColourFaces.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null)
        {
            mesh = CreateMesh();
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        string[] names = { "Coral", "Mint", "Yellow", "Violet", "Cyan", "Orange" };
        Color[] colours =
        {
            new Color(1f, 0.24f, 0.34f), new Color(0.14f, 0.85f, 0.48f),
            new Color(1f, 0.78f, 0.08f), new Color(0.61f, 0.23f, 1f),
            new Color(0.05f, 0.67f, 1f), new Color(1f, 0.40f, 0.08f)
        };
        var materials = new Material[6];
        for (int face = 0; face < materials.Length; face++)
        {
            string path = "Assets/Materials/CubeFaces/" + names[face] + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = names[face] };
                material.color = colours[face];
                material.SetFloat("_Glossiness", 0.2f);
                AssetDatabase.CreateAsset(material, path);
            }
            materials[face] = material;
        }

        Undo.RecordObjects(new UnityEngine.Object[] { cube.GetComponent<MeshFilter>(), cube.GetComponent<MeshRenderer>() }, "Colour cube faces");
        cube.GetComponent<MeshFilter>().sharedMesh = mesh;
        cube.GetComponent<MeshRenderer>().sharedMaterials = materials;
        EditorSceneManager.MarkSceneDirty(cube.scene);
        Debug.Log("AR_CUBE_COLOURS: six coloured faces applied; cube size and placement preserved.");
    }

    static Mesh CreateMesh()
    {
        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        Vector3[] right = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
        Vector3[] up = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
        var vertices = new Vector3[24];
        var vertexNormals = new Vector3[24];
        var uv = new Vector2[24];
        var mesh = new Mesh { name = "AR Cube — Six Faces", subMeshCount = 6 };

        for (int face = 0; face < 6; face++)
        {
            int i = face * 4;
            Vector3 centre = normals[face] * 0.5f;
            Vector3 r = right[face] * 0.5f;
            Vector3 u = up[face] * 0.5f;
            vertices[i] = centre - r - u;
            vertices[i + 1] = centre + r - u;
            vertices[i + 2] = centre + r + u;
            vertices[i + 3] = centre - r + u;
            for (int j = 0; j < 4; j++) vertexNormals[i + j] = normals[face];
            uv[i] = Vector2.zero;
            uv[i + 1] = Vector2.right;
            uv[i + 2] = Vector2.one;
            uv[i + 3] = Vector2.up;
        }
        mesh.vertices = vertices;
        mesh.normals = vertexNormals;
        mesh.uv = uv;
        for (int face = 0; face < 6; face++)
        {
            int i = face * 4;
            mesh.SetTriangles(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }, face);
        }
        mesh.RecalculateBounds();
        return mesh;
    }
}
