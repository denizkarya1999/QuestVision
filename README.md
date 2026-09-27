# QuestVision

A Unity mixed reality playground for Meta Quest 2 and 3. Its current app, **AR Cube**, places a colourful cube in your real room so you can walk around it, rotate it with a controller, and write on its faces with the Quest keyboard.

The app has been tested on **Quest 2**. The project also targets **Quest 3 and Quest 3S**, which have not yet been tested with this app.

## Features

- Passthrough view of your room with a 45 cm cube that stays in place as you move.
- Six coloured faces: coral, mint, yellow, violet, cyan, and orange.
- Controller rotation and tilt, plus a button to reposition the cube.
- A controls card shown at startup.
- Virtual-keyboard text on all six faces, with up to 60 characters saved locally between launches.
- Keyboard timeout recovery and build checks for cube controls, text, and layout.

## Controls

| Input | Action |
| --- | --- |
| Physical movement | Walk around the cube |
| Right thumbstick left/right | Rotate the cube |
| Right thumbstick up/down | Tilt the cube |
| A | Reposition the cube in front of you and reset its orientation |
| B | Show or hide the controls card |
| X on the left controller | Open the virtual keyboard |
| Keyboard **Done** | Save the writing on all six faces |

Cube controls pause while typing. Dismiss the keyboard to cancel. See [AR-CUBE.md](AR-CUBE.md) for the complete guide and keyboard recovery behaviour.

## Open in Unity

1. Clone this repository, then use **Unity Hub → Add → Add project from disk** and select its folder.
2. Open with **Unity 6000.6.3f1**. Install the editor's **Android Build Support**, including its SDK, NDK, and OpenJDK modules.
3. Let Unity restore the packages listed in `Packages/manifest.json` and `Packages/packages-lock.json`.
4. Select **Android** in **File → Build Profiles** and switch to that platform.
5. Open `Assets/Scenes/ARCube.unity`, or choose **Quest Vision → Show AR Cube in Scene View** to open the scene and centre the view on the cube.

The project uses ARM64, IL2CPP, Vulkan, and OpenXR. The current Android minimum API is 32 and target API is 34. Passthrough and physical movement are tested in the headset; Unity's Scene view is useful for inspecting and editing the cube.

## Build and install

Choose **Quest Vision → Build AR Cube**. This runs the project's controls checks and creates a development build at `Builds/ARCube.apk`.

Enable Developer Mode on the headset, connect it over USB, and accept **Allow USB debugging**. With Android platform tools available, run these commands from the project folder:

```sh
adb devices
adb install -r Builds/ARCube.apk
adb shell am start -n com.denizkarya.arcube/com.unity3d.player.UnityPlayerActivity
```

The installed app appears as **AR Cube** under **Library → Unknown Sources**. USB is no longer needed after installation. Use the headset's boundary and a clear area for physical movement.

The Android entry point deliberately uses **Activity** for compatibility with the Quest system keyboard. See [AR-CUBE.md](AR-CUBE.md#keyboard-compatibility) before changing it to GameActivity.

## Project structure

| Path | Purpose |
| --- | --- |
| `Assets/Scenes/ARCube.unity` | Main mixed reality scene |
| `Assets/Scripts/WalkAroundCube.cs` | Placement, rotation, guide, keyboard, and saved text |
| `Assets/Scripts/CubeKeyboardWatchdog.cs` | Keyboard timeout and recovery logic |
| `Assets/Editor/ARCubeBuilder.cs` | Android build, launcher labels, and Meta app identity |
| `Assets/Editor/ARCubeControlsChecks.cs` | Pre-build checks for controls and writing |
| `Assets/Editor/ARCubeColours.cs` | Cube face colour setup |
| `Assets/Editor/ARCubeGuideBuilder.cs` | Startup controls card |
| `Assets/Editor/ARCubeTextBuilder.cs` | Text canvases for the six faces |
| `Assets/Materials/CubeFaces/` | Editable face colours |
| `Packages/` | Dependency versions and lockfile |
| `ProjectSettings/` | Shared Unity and Android settings |

The original VR starter is preserved in `Assets/Scenes/QuestStarter.unity`. Its **Configure Quest 2 and Quest 3** and **Build Android APK** menu actions are separate from the AR Cube workflow and change project settings.

## Main dependencies

| Component | Version |
| --- | --- |
| Meta XR Core SDK | 207.0.0 |
| Meta XR Interaction SDK with OVR integration | 207.0.0 |
| Mixed Reality Utility Kit | 207.0.0 |
| Unity OpenXR | 1.18.0 |
| XR Plugin Management | 4.6.1 |
| Unity Inference Engine | 2.6.1 |
| Android Logcat | 1.4.7 |

Dependencies are restored through Unity Package Manager and remain subject to their respective licenses.

## Current status and limitations

- **AR Cube 1.4.2** was built, installed, and launched on Quest 2. Physical movement, passthrough, keyboard writing, rotation, and the library name were confirmed during headset testing across the 1.4 releases.
- Cube placement lasts for the current session. Persistent spatial anchors and room scanning are not implemented.
- The project does not capture raw camera images or implement YOLO inference or detection boxes. Unity Inference Engine is included for future experiments.
- The Quest library shows **AR Cube**, but the system menu beside **Quit** still reports **App name unavailable** on the tested headset. Adding the registered Meta App ID did not resolve that separate label; see [META-APP.md](META-APP.md).
- Builds are development APKs for USB installation. No Meta Store or release-channel build has been uploaded.

## Local settings and app identity

Unity caches, APKs, build logs, user preferences, signing keys, and local Meta developer-agent/upload settings are excluded from Git. They are not needed to clone the source or restore its packages.

The Meta App ID recorded in the Android manifest is public configuration, not a secret. For your own registered app, create your own Meta App ID and update `MetaAppId` in `Assets/Editor/ARCubeBuilder.cs`. Also change the package identifier in that builder before building a separate app. Never commit an App Secret or access token. Registration details are documented in [META-APP.md](META-APP.md).
