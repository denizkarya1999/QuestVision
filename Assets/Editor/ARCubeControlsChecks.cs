using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Checks control invariants and guide content before building the headset app.</summary>
public static class ARCubeControlsChecks
{
    public static void Validate()
    {
        // Reproduce a native keyboard stuck at Visible without an actual overlay.
        var failedKeyboard = new CubeKeyboardWatchdog(0);
        Check(failedKeyboard.Observe(4.9, false, true) == CubeKeyboardWatchdog.Recovery.None, "Allow time for keyboard startup.");
        Check(failedKeyboard.Observe(5, false, true) == CubeKeyboardWatchdog.Recovery.DidNotOpen, "A failed keyboard must release the controls after five seconds.");
        var visibleKeyboard = new CubeKeyboardWatchdog(0);
        Check(visibleKeyboard.Observe(0.2, true, false) == CubeKeyboardWatchdog.Recovery.None, "A presented keyboard must remain usable.");
        Check(visibleKeyboard.Observe(120, true, false) == CubeKeyboardWatchdog.Recovery.None, "Long typing sessions must not time out.");
        Check(visibleKeyboard.Observe(120.2, false, true) == CubeKeyboardWatchdog.Recovery.None, "Allow normal closing callbacks to arrive.");
        Check(visibleKeyboard.Observe(121.1, false, true) == CubeKeyboardWatchdog.Recovery.ClosedWithoutResult, "A closed keyboard must not trap the controls.");
        Check(PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.Activity, "Use the keyboard-compatible Android activity.");
        Quaternion start = Quaternion.Euler(15, 25, 0);
        Check(Quaternion.Angle(start, Rotate(start, new Vector2(0.1f, 0.1f), 1f / 72f)) < 0.01f, "Stick drift must not rotate the cube.");
        Check(Quaternion.Angle(start, Rotate(start, Vector2.right, 0f)) < 0.01f, "Zero elapsed time must not rotate the cube.");
        Quaternion yaw = Quaternion.identity;
        for (int i = 0; i < 72; i++) yaw = Rotate(yaw, Vector2.right, 1f / 72f);
        Check(Quaternion.Angle(yaw, Quaternion.Euler(0, 75, 0)) < 0.1f, "Full right stick must turn 75 degrees in one second.");
        Quaternion yaw90 = Quaternion.identity;
        for (int i = 0; i < 90; i++) yaw90 = Rotate(yaw90, Vector2.right, 1f / 90f);
        Check(Quaternion.Angle(yaw, yaw90) < 0.1f, "Rotation must be consistent across headset frame rates.");
        Quaternion pitch = Quaternion.identity;
        for (int i = 0; i < 72; i++) pitch = Rotate(pitch, Vector2.up, 1f / 72f);
        Check(Quaternion.Angle(pitch, Quaternion.Euler(-75, 0, 0)) < 0.1f, "Up on the stick must tilt the cube toward the viewer.");
        Check(Quaternion.Angle(start, Rotate(start, Vector2.one, 10f)) <= 5.4f, "Resuming after a pause must not jump the rotation.");

        WalkAroundCube app = null;
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            var candidate = root.GetComponent<WalkAroundCube>();
            if (candidate != null) app = candidate;
        }
        Check(app != null && app.controlsGuide != null, "Startup guide must be assigned.");
        Check(!app.cube.IsChildOf(app.cameraRig.transform), "Cube must stay independent of headset movement.");
        Check(app.controlsGuide.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "Guide must render in VR.");
        Check(OVRProjectConfig.CachedProjectConfig.requiresSystemKeyboard, "System keyboard support must be enabled.");
        Check(WalkAroundCube.NormalizeText(null) == "", "Empty input must clear the text.");
        Check(WalkAroundCube.NormalizeText("  Hello\r\nQuest\t!  ") == "Hello Quest!", "Typed text must normalize line breaks and control characters.");
        Check(WalkAroundCube.NormalizeText(new string('W', 61)).Length == 60, "Writing must stay within its character limit.");
        Check(WalkAroundCube.NormalizeText(new string('A', 59) + "\U0001F600Z") == new string('A', 59) + "\U0001F600", "Truncation must preserve complete Unicode characters.");
        Check(app.faceLabels != null && app.faceLabels.Length == 6, "Each cube face needs a text label.");
        foreach (var label in app.faceLabels)
        {
            Check(label != null && label.transform.IsChildOf(app.cube), "Writing must stay attached while the cube rotates.");
            Check(!label.supportRichText, "Typed text must display literally.");
            var face = label.transform.parent;
            Check(Vector3.Dot(-face.forward, (face.position - app.cube.position).normalized) > 0.99f, "Writing must face outward.");
            string previous = label.text;
            label.text = new string('W', WalkAroundCube.TextLimit);
            Check(label.preferredHeight <= label.rectTransform.rect.height + 1f, "Maximum-length text must fit the cube face.");
            label.text = previous;
        }
        foreach (var label in app.controlsGuide.GetComponentsInChildren<Text>(true))
        {
            Check(label.font != null && !string.IsNullOrEmpty(label.text), "Every guide label needs text and a font.");
            Check(label.preferredHeight <= label.rectTransform.rect.height + 1f, "Guide text must fit: " + label.name);
        }
        Debug.Log("AR_CUBE_CONTROLS_CHECKS: passed keyboard timeout and recovery regressions, rotation, placement, guide and text checks.");
    }

    static Quaternion Rotate(Quaternion rotation, Vector2 input, float dt)
        => WalkAroundCube.CalculateRotation(rotation, input, Vector3.right, 75f, dt);

    static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("AR Cube check failed: " + message);
    }
}
