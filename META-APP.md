# AR Cube — Meta app registration

- App name: **AR Cube**
- App ID: **1305150979358479**
- Developer team: **Trustworthy AI-enhanced IoT Lab**
- Platform: **Meta Horizon Store** (standalone Quest)
- Created: September 26, 2026
- Status when created: **Not released**, with no uploaded builds
- [App dashboard](https://developers.meta.com/horizon/manage/applications/1305150979358479/)
- [App ID page](https://developers.meta.com/horizon/manage/applications/1305150979358479/api/)

`Assets/Editor/ARCubeBuilder.cs` stores the public App ID and writes it into the Android manifest as application metadata named `com.oculus.app_id`. The value uses an escaped leading space so Android preserves the long number as a string. The package identifier stays `com.denizkarya.arcube`.

The app and launcher labels remain **AR Cube**. Meta's system Quit menu uses separate app identity information; version 1.4.2 adds the registered App ID to address the **App name unavailable** message. The user tested version 1.4.2 and confirmed that the system-menu label remains **App name unavailable**. The App ID is created and present in the APK, but headset logs report no Meta library record for the USB-installed package. No build has been uploaded to Meta, and this label is unresolved. The next troubleshooting route is a properly signed, non-development build in a private release channel. The current development APK is not ready for Store upload.
