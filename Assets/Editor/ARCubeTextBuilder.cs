using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Attaches outward-facing text to each cube face, so the writing rotates with it.</summary>
public static class ARCubeTextBuilder
{
    public static void ApplyToOpenScene()
    {
        WalkAroundCube app = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var candidate = root.GetComponent<WalkAroundCube>();
            if (candidate != null) app = candidate;
        }
        if (app == null) throw new InvalidOperationException("AR Cube App was not found.");
        if (app.faceLabels != null && app.faceLabels.Length == 6 && Array.TrueForAll(app.faceLabels, label => label != null)) return;

        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        Vector3[] up = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
        string[] faces = { "Right", "Left", "Top", "Bottom", "Front", "Back" };
        var labels = new Text[6];
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        for (int i = 0; i < 6; i++)
        {
            var face = new GameObject("Writing - " + faces[i], typeof(RectTransform), typeof(Canvas));
            Undo.RegisterCreatedObjectUndo(face, "Add cube writing");
            var rect = face.GetComponent<RectTransform>();
            rect.SetParent(app.cube, false);
            rect.sizeDelta = new Vector2(840, 760);
            rect.localPosition = normals[i] * 0.503f;
            rect.localRotation = Quaternion.LookRotation(-normals[i], up[i]);
            rect.localScale = Vector3.one * 0.001f;
            var canvas = face.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = app.cameraRig.centerEyeAnchor.GetComponent<Camera>();

            var labelObject = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.sizeDelta = new Vector2(800, 720);
            var label = labelObject.GetComponent<Text>();
            label.font = font;
            label.fontSize = 78;
            label.fontStyle = FontStyle.Bold;
            label.text = "Press X to write";
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 36;
            label.resizeTextMaxSize = 78;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.supportRichText = false;
            label.raycastTarget = false;
            var outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.025f, 0.04f, 0.07f, 1f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            labels[i] = label;
        }
        Undo.RecordObject(app, "Assign cube face labels");
        app.faceLabels = labels;
        EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
        Debug.Log("AR_CUBE_TEXT: six face labels created.");
    }
}
