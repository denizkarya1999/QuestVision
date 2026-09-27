# AR Cube

On the headset, open **Library → Unknown Sources → AR Cube**. Apps installed directly from a computer are listed under Unknown Sources. If that tab is missing, close and reopen Library.

After tracking starts, a 45 cm cube appears 1.5 metres in front of you, slightly below eye level. Each face has a different colour: coral, mint, yellow, violet, cyan, and orange. Your real room is visible through passthrough. Walk around the cube to see its sides; it stays in place until you press **A** on the right controller to reposition it in front of you.

A controls card appears above the cube at startup:

- **Right thumbstick left/right:** turn the cube.
- **Right thumbstick up/down:** tilt the cube.
- **A:** place the cube in front of you again and reset its orientation.
- **B:** hide or show the controls card. Reopening it places the card in front of you.
- **X (left controller):** open the Quest keyboard to write on the cube. Select **Done** to save, or dismiss the keyboard to cancel.

Releasing the thumbstick stops rotation immediately. Rotating changes the cube's orientation without moving its position in the room.

You can enter up to 60 characters. The same writing appears on all six faces and rotates with the cube. Press **X** to edit it again; delete the text and select **Done** to clear it. Your writing is saved locally on the headset between launches. Cube controls pause while the keyboard is open. If it fails to appear, the app restores the controls after five seconds. While the app has controller focus, **X** or **B** cancels a pending keyboard request.

The app uses physical movement. Once the APK is installed and launched, USB is no longer needed. Use your existing headset boundary and a clear area around the cube.

The cube's placement lasts for the current session. It does not scan the room or save a spatial anchor, and it does not read raw camera images.

Version 1.4 (keyboard compatibility and recovery fix) was installed and launched on the connected Quest 2 on September 26, 2026. The build passed keyboard timeout and recovery regressions, rotation, guide, text limit, and face orientation checks with zero build errors. Headset logs confirmed keyboard presentation at 20:35:02, saving text to all six faces at 20:35:12, and thumbstick rotation afterward (device log times). You confirmed that it works in the headset. You previously confirmed that the cube appeared over the real room and stayed fixed as you moved around it.

Version 1.4.1 adds explicit **AR Cube** labels to the Android launch activity and its launch intent. It was installed on September 26, 2026; the installed launcher now contains both labels. You confirmed that the Quest library now displays **AR Cube**.

## Project files

To inspect the cube in Unity, stop Play mode, then choose **Quest Vision → Show AR Cube in Scene View**. This opens the AR scene, selects **Walk Around Cube**, and centres the Scene view on it. Use the installed headset app to test passthrough and physical movement.

- Scene: `Assets/Scenes/ARCube.unity`
- Runtime: `Assets/Scripts/WalkAroundCube.cs`
- Startup guide: `Assets/Editor/ARCubeGuideBuilder.cs` (creates the scene's **Controls Guide** canvas)
- Cube face writing: `Assets/Editor/ARCubeTextBuilder.cs` (creates six face canvases; runtime keyboard handling is in `WalkAroundCube.cs`)
- Keyboard recovery: `Assets/Scripts/CubeKeyboardWatchdog.cs`
- Build checks: `Assets/Editor/ARCubeControlsChecks.cs`
- Face colours: `Assets/Materials/CubeFaces/` — select a material in Unity to change its colour.
- Colour setup: `Assets/Editor/ARCubeColours.cs` (menu **Quest Vision → Colour Cube Faces**)
- Build menu: **Quest Vision → Build AR Cube**
- APK: `Builds/ARCube.apk`
- Android package: `com.denizkarya.arcube`

The original VR starter scene remains in `Assets/Scenes/QuestStarter.unity`.

The passthrough configuration follows [Meta's official setup](https://developers.meta.com/horizon/documentation/unity/unity-passthrough-gs/): enable passthrough on the camera rig, add an OVR Passthrough Layer, remove the skybox, and clear the camera to transparent black.

## Keyboard compatibility

The Android build uses **Activity** as its Application Entry Point. GameActivity caused the system keyboard to time out on this Quest 2 while reporting a visible status, leaving the old typing flow stuck. Meta has a [matching compatibility report](https://developers.meta.com/horizon/feedback/vr/investigations/2279537085906028/). The build deliberately keeps Activity even though Meta/OpenXR setup checks recommend GameActivity. System keyboard support and the `oculus.software.overlay_keyboard` manifest feature remain enabled.

## Quest system-menu name

The headset library displays **AR Cube** (user confirmed). The user separately reports **App name unavailable** beside **Quit** in the Meta/Oculus system menu. The installed application, activity, and launch intent have valid AR Cube labels. A [documented Horizon OS behavior](https://viro-community.readme.io/docs/horizonos-setup-guide) connects this system-overlay label to missing `com.oculus.app_id` metadata. AR Cube was registered in the Meta Developer Dashboard with App ID **1305150979358479**, and version **1.4.2** adds that identity to the manifest. This version was installed and launched successfully. The user confirmed that the system menu still says **App name unavailable**. Headset logs report no Meta library entry/entitlement record for the installed package. Creating the App ID and adding the manifest metadata did not resolve the system-menu label. A Meta-hosted private test build is the next troubleshooting route; none has been uploaded. See `META-APP.md` for the registration and dashboard links.
