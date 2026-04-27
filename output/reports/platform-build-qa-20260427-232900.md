# Hollow Platform Build QA

- Result: Failed
- Generated: 2026-04-27T23:29:00.8690930Z
- Unity: 6000.4.1f1
- Git: main @ 40c1d96
- Build root: `output/builds`

| Target | Platform | Result | Output | Notes | Remediation |
| --- | --- | --- | --- | --- | --- |
| m0-m23-audit | Editor | Failed | `output/reports/latest_m24_dependency_audit.json` | M0-M23 audit failed: 4/24. | Open latest_m24_dependency_audit.md for failing validator details. |
| addressables-build | Local Addressables | Passed | `Library/com.unity.addressables/aa/OSX/settings.json` | Local Addressables content built successfully. | OK |
| editmode-tests | Unity Test Runner | NotRun | `` | Run EditMode tests with `Unity -batchmode -projectPath <repo> -runTests -testPlatform editmode -testResults output/reports/m24-editmode-results.xml`. | Run this command as a separate Unity Test Runner invocation; the in-process QA gate records the expected command and the PlayMode/EditMode suites remain independently verifiable. |
| playmode-smoke-tests | Unity Test Runner | NotRun | `` | Run PlayMode smoke tests with `Unity -batchmode -projectPath <repo> -runTests -testPlatform playmode -testResults output/reports/m24-playmode-results.xml`. | Run this command as a separate Unity Test Runner invocation; the in-process QA gate records the expected command and the PlayMode/EditMode suites remain independently verifiable. |
| windows-development-build | StandaloneWindows64 | BlockedByEnvironment | `output/builds/HollowSoul_M24_Windows/HollowSoul.exe` | Standalone Windows 64-bit build support is not installed in this Unity editor. | Install Unity Windows Build Support for Unity 6000.4.1f1.<br>Rerun Hollow/Platform QA/Build Windows Development M24. |
| visionos-readiness | visionOS Simulator/Readiness | BlockedByEnvironment | `` | Vision Pro project readiness is present, but local simulator/build tooling is incomplete. | Install Xcode command line tools and verify `xcrun simctl` works for simulator QA. |

## Manual Device Checklist
- Windows: launch HollowSoul.exe, create/select profile, start New Run, move/shoot/clear one room, traverse a door, buy a shop card, quit and Continue.
- Windows: open Room Designer, create a 1x1 draft, move cursor, place/erase a rock/enemy marker, export JSON/USDA bundle.
- Vision Pro bounded: verify tabletop world scale is 0.1, HUD/minimap are readable and unscaled, and ArtPass visuals do not add gameplay colliders.
- Vision Pro immersive: verify full-scale world, comfort vignette profile metadata, camera posture, and readable combat spacing.
- All platforms: confirm save/profile state changes only occur in profile-backed sessions and transient designer/sample sessions stay safe.
