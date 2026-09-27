using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates a readable world-space controls card that is also visible in the Scene view.</summary>
public static class ARCubeGuideBuilder
{
    public static void ApplyToOpenScene()
    {
        WalkAroundCube app = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var candidate = root.GetComponent<WalkAroundCube>();
            if (candidate != null) app = candidate;
        }
        if (app == null)
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                Debug.Log("AR_CUBE_GUIDE_DIAGNOSTIC: " + root.name + ": " + string.Join(", ", root.GetComponents<Component>().Select(c => c == null ? "missing component" : c.GetType().FullName)));
        if (app == null) throw new InvalidOperationException("AR Cube App was not found.");
        if (app.controlsGuide != null)
        {
            AddWritingControls(app);
            return;
        }

        var card = new GameObject("Controls Guide", typeof(RectTransform), typeof(Canvas));
        Undo.RegisterCreatedObjectUndo(card, "Add startup controls guide");
        var canvas = card.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = app.cameraRig.centerEyeAnchor.GetComponent<Camera>();
        var rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(900, 470);
        rect.localScale = Vector3.one * 0.0012f;
        rect.position = new Vector3(0, 1.95f, 1.8f);

        AddPanel(rect, "Background", new Vector2(900, 470), Vector2.zero, new Color(0.025f, 0.055f, 0.10f, 0.96f));
        AddPanel(rect, "Accent", new Vector2(900, 7), new Vector2(0, 231.5f), new Color(0.2f, 0.88f, 0.82f));
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        AddText(rect, font, "Title", "AR CUBE", 42, new Vector2(804, 58), new Vector2(0, 169), Color.white, FontStyle.Bold);
        AddText(rect, font, "Introduction", "Walk around the cube to explore every side.", 29, new Vector2(804, 48), new Vector2(0, 110), new Color(0.78f, 0.85f, 0.92f));
        AddRow(rect, font, "RIGHT STICK", "Rotate and tilt", 34);
        AddRow(rect, font, "A BUTTON", "Place in front of you", -29);
        AddRow(rect, font, "B BUTTON", "Hide / show this guide", -92);
        AddText(rect, font, "Footer", "The cube stays in place when you move.", 26, new Vector2(804, 42), new Vector2(0, -176), new Color(0.70f, 0.78f, 0.86f));

        Undo.RecordObject(app, "Assign controls guide");
        app.controlsGuide = rect;
        AddWritingControls(app);
        EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
        Debug.Log("AR_CUBE_GUIDE: startup controls card added to the scene.");
    }

    static void AddWritingControls(WalkAroundCube app)
    {
        var rect = (RectTransform)app.controlsGuide;
        rect.sizeDelta = new Vector2(900, 540);
        ((RectTransform)rect.Find("Background")).sizeDelta = rect.sizeDelta;
        SetRowPosition(rect, "Accent", 266.5f);
        SetRowPosition(rect, "Title", 204);
        SetRowPosition(rect, "Introduction", 145);
        SetRowPosition(rect, "RIGHT STICK", 70);
        SetRowPosition(rect, "Rotate and tilt", 70);
        SetRowPosition(rect, "A BUTTON", 7);
        SetRowPosition(rect, "Place in front of you", 7);
        SetRowPosition(rect, "B BUTTON", -56);
        SetRowPosition(rect, "Hide / show this guide", -56);
        SetRowPosition(rect, "Footer", -210);
        if (rect.Find("X BUTTON") == null)
            AddRow(rect, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), "X BUTTON", "Write on the cube", -119);
        Undo.RecordObject(app, "Assign writing status");
        app.writingStatus = rect.Find("Footer").GetComponent<Text>();
        app.writingStatus.text = "Type up to 60 characters. Select Done to save.";
        EditorSceneManager.MarkSceneDirty(app.gameObject.scene);
    }

    static void SetRowPosition(RectTransform parent, string name, float y)
    {
        // Labels can contain '/', which Transform.Find interprets as a hierarchy path.
        foreach (RectTransform child in parent)
            if (child.name == name) child.anchoredPosition = new Vector2(child.anchoredPosition.x, y);
    }

    static void AddRow(RectTransform parent, Font font, string control, string description, float y)
    {
        AddText(parent, font, control, control, 28, new Vector2(245, 48), new Vector2(-279.5f, y), new Color(0.2f, 0.88f, 0.82f), FontStyle.Bold);
        AddText(parent, font, description, description, 32, new Vector2(545, 48), new Vector2(129.5f, y), Color.white);
    }

    static void AddPanel(RectTransform parent, string name, Vector2 size, Vector2 position, Color colour)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        var image = panel.GetComponent<Image>();
        image.color = colour;
        image.raycastTarget = false;
    }

    static void AddText(RectTransform parent, Font font, string name, string content, int size,
        Vector2 bounds, Vector2 position, Color colour, FontStyle style = FontStyle.Normal)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rect = label.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = bounds;
        rect.anchoredPosition = position;
        var text = label.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.text = content;
        text.color = colour;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
    }
}
