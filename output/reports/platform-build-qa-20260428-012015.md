# Hollow Platform Build QA

- Result: PassedWithEnvironmentBlocks
- Generated: 2026-04-28T01:20:15.0495920Z
- Unity: 6000.4.1f1
- Git: main @ 2c3f556
- Build root: `output/builds`

| Target | Platform | Result | Output | Notes | Remediation |
| --- | --- | --- | --- | --- | --- |
| m0-m23-audit | Editor | Passed | `output/reports/latest_m24_dependency_audit.json` | M0-M23 audit passed: 24/24. | OK |
| addressables-build | Local Addressables | Passed | `Library/com.unity.addressables/aa/OSX/settings.json` | Local Addressables content built successfully. | OK |
| editmode-tests | Unity Test Runner | Passed | `output/reports/m24-editmode-results.xml` | EditMode tests completed: 216/216 passed, 0 failed, 0 inconclusive, 0 skipped. | OK |
| playmode-smoke-tests | Editor Scene Smoke | Passed | `output/reports/m24-playmode-smoke-editor-probe.md` | Platform scene smoke probe passed for MainMenu, RoomDesigner, Windows, VisionOS bounded, and VisionOS immersive. | OK |
| windows-development-build | StandaloneWindows64 | BlockedByEnvironment | `output/builds/HollowSoul_M24_Windows/HollowSoul.exe` | Standalone Windows 64-bit build support is not installed in this Unity editor. | Install Unity Windows Build Support for Unity 6000.4.1f1.<br>Rerun Hollow/Platform QA/Build Windows Development M24. |
| visionos-readiness | visionOS Simulator/Readiness | Passed | `` | Vision Pro bounded/immersive scenes, polish profiles, and simulator tooling are present. | OK |

## Manual Device Checklist
- Windows: launch HollowSoul.exe, create/select profile, start New Run, move/shoot/clear one room, traverse a door, buy a shop card, quit and Continue.
- Windows: open Room Designer, create a 1x1 draft, move cursor, place/erase a rock/enemy marker, export JSON/USDA bundle.
- Vision Pro bounded: verify tabletop world scale is 0.1, HUD/minimap are readable and unscaled, and ArtPass visuals do not add gameplay colliders.
- Vision Pro immersive: verify full-scale world, comfort vignette profile metadata, camera posture, and readable combat spacing.
- All platforms: confirm save/profile state changes only occur in profile-backed sessions and transient designer/sample sessions stay safe.
