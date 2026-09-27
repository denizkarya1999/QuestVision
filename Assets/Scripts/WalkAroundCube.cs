using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;

/// <summary>Places a cube once in tracking space so the wearer can walk around it.</summary>
public sealed class WalkAroundCube : MonoBehaviour
{
    public OVRCameraRig cameraRig;
    public Transform cube;
    public OVRPassthroughLayer passthrough;
    public float placementDistance = 1.5f;
    public float rotationSpeed = 75f;
    public Transform controlsGuide;
    public Text[] faceLabels;
    public Text writingStatus;

    public const int TextLimit = 60;
    const string TextPreference = "ARCube.FaceText";

    const float StickDeadZone = 0.18f;

    bool placed;
    float trackedTime;
    bool rotating;
    TouchScreenKeyboard keyboard;
    CubeKeyboardWatchdog keyboardWatchdog;
    string cubeText;
    bool restoreGuideAfterKeyboard;
    float acceptControlsAfter;

    void Awake()
    {
        cube.gameObject.SetActive(false);
        if (controlsGuide != null) controlsGuide.gameObject.SetActive(false);
        cubeText = NormalizeText(PlayerPrefs.GetString(TextPreference, ""));
        ApplyFaceText(PlayerPrefs.HasKey(TextPreference) ? cubeText : "Press X to write");
        passthrough.passthroughLayerResumed.AddListener(OnPassthroughReady);
        Debug.Log("AR_CUBE: started; waiting for headset tracking");
    }

    void OnDestroy()
    {
        if (keyboard != null) keyboard.active = false;
        if (passthrough != null)
            passthrough.passthroughLayerResumed.RemoveListener(OnPassthroughReady);
    }

    void OnPassthroughReady(OVRPassthroughLayer layer)
    {
        Debug.Log("AR_CUBE: passthrough is visible");
    }

    void LateUpdate()
    {
        // The system keyboard takes input focus, so poll its result before the focus check.
        if (keyboard != null)
        {
            PollKeyboard();
            return;
        }
        if (Time.unscaledTime < acceptControlsAfter) return;
        var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        bool tracked = head.isValid &&
                       head.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked) && isTracked;
        if (!tracked || !OVRManager.hasInputFocus)
        {
            trackedTime = 0;
            rotating = false;
            return;
        }

        if (!placed)
        {
            // Wait for the floor-level tracking origin to settle before placing.
            trackedTime += Time.unscaledDeltaTime;
            if (trackedTime >= 0.5f)
            {
                PlaceCube();
                ShowGuide(true);
            }
            return;
        }
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            OpenKeyboard();
            return;
        }
        if (OVRInput.GetDown(OVRInput.RawButton.A))
        {
            PlaceCube();
        }

        if (OVRInput.GetDown(OVRInput.RawButton.B))
            ShowGuide(controlsGuide != null && !controlsGuide.gameObject.activeSelf);

        Vector2 stick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch);
        bool inputActive = stick.magnitude > StickDeadZone;
        if (inputActive != rotating)
        {
            Debug.Log(inputActive ? "AR_CUBE: rotating with right thumbstick" : "AR_CUBE: rotation stopped");
            rotating = inputActive;
        }
        cube.rotation = CalculateRotation(cube.rotation, stick, cameraRig.centerEyeAnchor.right,
            rotationSpeed, Time.unscaledDeltaTime);
    }

    void OpenKeyboard()
    {
        if (!TouchScreenKeyboard.isSupported)
        {
            SetWritingStatus("Keyboard unavailable. Try reopening the app.");
            ShowGuide(true);
            Debug.LogWarning("AR_CUBE: system keyboard is unavailable");
            return;
        }
        restoreGuideAfterKeyboard = controlsGuide != null && controlsGuide.gameObject.activeSelf;
        SetWritingStatus("Opening keyboard... X or B cancels.");
        ShowGuide(true);
        keyboardWatchdog = new CubeKeyboardWatchdog(Time.realtimeSinceStartupAsDouble);
        try
        {
            keyboard = TouchScreenKeyboard.Open(cubeText, TouchScreenKeyboardType.Default,
                false, false, false, false, "Write on the cube", TextLimit);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning("AR_CUBE: keyboard request failed: " + exception.GetType().Name);
            CancelKeyboard("Keyboard could not open. Press X to retry.");
            return;
        }
        if (keyboard == null)
        {
            CancelKeyboard("Keyboard could not open. Press X to retry.");
            return;
        }
        rotating = false;
        Debug.Log("AR_CUBE: system keyboard requested; waiting for presentation");
    }

    void PollKeyboard()
    {
        // A Visible status alone is not reliable: GameActivity can return it even
        // after the Android keyboard request times out and no keyboard is shown.
        if (keyboard.status == TouchScreenKeyboard.Status.Visible)
        {
            if (OVRManager.hasInputFocus &&
                (OVRInput.GetDown(OVRInput.RawButton.X) || OVRInput.GetDown(OVRInput.RawButton.B)))
            {
                CancelKeyboard("Editing cancelled. Press X to try again.");
                return;
            }
            bool wasPresented = keyboardWatchdog.WasPresented;
            bool presented = TouchScreenKeyboard.visible ||
                             (!OVRManager.hasInputFocus && OVRManager.hasVrFocus);
            var recovery = keyboardWatchdog.Observe(Time.realtimeSinceStartupAsDouble, presented, OVRManager.hasInputFocus);
            if (!wasPresented && keyboardWatchdog.WasPresented)
            {
                if (controlsGuide != null) controlsGuide.gameObject.SetActive(false);
                Debug.Log("AR_CUBE: keyboard presentation detected");
            }
            if (recovery != CubeKeyboardWatchdog.Recovery.None)
                CancelKeyboard(recovery == CubeKeyboardWatchdog.Recovery.DidNotOpen
                    ? "Keyboard did not open. Press X to retry."
                    : "Keyboard closed. Press X to try again.");
            return;
        }
        if (keyboard.status == TouchScreenKeyboard.Status.Done)
        {
            cubeText = NormalizeText(keyboard.text);
            ApplyFaceText(cubeText);
            PlayerPrefs.SetString(TextPreference, cubeText);
            PlayerPrefs.Save();
            SetWritingStatus("Saved on every face. Press X to edit again.");
            // Do not write the user's text to diagnostic logs.
            Debug.Log("AR_CUBE: cube text saved to all six faces");
        }
        else
        {
            SetWritingStatus("Editing cancelled. Press X to try again.");
            Debug.Log("AR_CUBE: keyboard dismissed without saving");
        }
        keyboard = null;
        keyboardWatchdog = null;
        acceptControlsAfter = Time.unscaledTime + 0.3f;
        ShowGuide(restoreGuideAfterKeyboard);
    }

    void CancelKeyboard(string message)
    {
        var pending = keyboard;
        keyboard = null;
        keyboardWatchdog = null;
        acceptControlsAfter = Time.unscaledTime + 0.3f;
        SetWritingStatus(message);
        ShowGuide(true);
        try { if (pending != null) pending.active = false; }
        catch (System.Exception exception)
        { Debug.LogWarning("AR_CUBE: keyboard cleanup: " + exception.GetType().Name); }
        Debug.Log("AR_CUBE: keyboard cancelled; cube controls restored");
    }

    void OnApplicationPause(bool paused)
    {
        if (paused && keyboard != null)
            CancelKeyboard("Editing cancelled. Press X to try again.");
    }

    void ApplyFaceText(string value)
    {
        if (faceLabels == null) return;
        foreach (var label in faceLabels)
        {
            if (label == null) continue;
            label.text = value;
            label.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(value));
        }
    }

    void SetWritingStatus(string message)
    {
        if (writingStatus != null) writingStatus.text = message;
    }

    public static string NormalizeText(string value)
    {
        value = (value ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
        var output = new StringBuilder();
        var elements = StringInfo.GetTextElementEnumerator(value);
        int count = 0;
        while (count < TextLimit && elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (char.IsControl(element[0])) continue;
            output.Append(element);
            count++;
        }
        return output.ToString().Trim();
    }

    // Changes orientation only: the cube remains at exactly the same room position.
    public static Quaternion CalculateRotation(Quaternion rotation, Vector2 stick,
        Vector3 viewerRight, float degreesPerSecond, float deltaTime)
    {
        float magnitude = Mathf.Min(stick.magnitude, 1f);
        if (magnitude <= StickDeadZone || deltaTime <= 0f) return rotation;
        Vector2 input = stick.normalized * ((magnitude - StickDeadZone) / (1f - StickDeadZone));
        Vector3 pitchAxis = Vector3.ProjectOnPlane(viewerRight, Vector3.up).normalized;
        if (pitchAxis.sqrMagnitude < 0.01f) pitchAxis = Vector3.right;
        float step = Mathf.Max(0f, degreesPerSecond) * Mathf.Min(deltaTime, 0.05f);
        return (Quaternion.AngleAxis(-input.y * step, pitchAxis) *
                Quaternion.AngleAxis(input.x * step, Vector3.up) * rotation).normalized;
    }

    void ShowGuide(bool visible)
    {
        if (controlsGuide == null) return;
        if (visible) PositionGuide();
        controlsGuide.gameObject.SetActive(visible);
        Debug.Log("AR_CUBE: controls guide " + (visible ? "visible; right stick rotates; A places; B toggles help; X writes" : "hidden"));
    }

    void PositionGuide()
    {
        var head = cameraRig.centerEyeAnchor;
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        controlsGuide.SetPositionAndRotation(head.position + forward * 1.8f + Vector3.up * 0.34f,
            Quaternion.LookRotation(forward, Vector3.up));
    }

    void PlaceCube()
    {
        var head = cameraRig.centerEyeAnchor;
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
            forward = Vector3.ProjectOnPlane(cameraRig.transform.forward, Vector3.up);
        forward.Normalize();

        Vector3 position = head.position + forward * placementDistance;
        position.y = Mathf.Max(0.6f, head.position.y - 0.35f);
        cube.SetPositionAndRotation(position,
            Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(0, 25, 0));
        cube.gameObject.SetActive(true);
        placed = true;
        if (controlsGuide != null && controlsGuide.gameObject.activeSelf) PositionGuide();
        Debug.Log($"AR_CUBE: placed at {position:F3}; walk around it; A repositions");
    }
}
