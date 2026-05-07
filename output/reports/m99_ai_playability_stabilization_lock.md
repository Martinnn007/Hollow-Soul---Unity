# M99 AI Playability Stabilization Lock Report

## Changes
- Hardened `EnemyNavMeshAgentBridge` so missing/failed `NavMeshAgent` setup does not throw or freeze enemies.
- Added local-steering recovery from blocked Unity NavMesh requests in `EnemyNavigationAdapter`.
- Brightened `DoorUnavailable` fallback color for clearer inactive door readability.
- Made dynamic `NavMeshObstacle` carving best-effort and silent; static interactive blockers remain part of room NavMesh geometry.
- Added M99 EditMode smoke tests covering spawn resolution, macro room NavMesh runtime build, door/interactable integrity, and navigation recovery.

## Verification
- Static diff whitespace check passed with `git diff --check`.
- Unity batchmode test run was attempted for `Hollow.Tests.EditMode.Milestone99AiPlayabilityStabilizationTests`.
- Batchmode was blocked because the Unity editor already had this project open.

## Follow-Up Command
After closing the Unity editor:

```bash
/Applications/Unity/Hub/Editor/6000.4.1f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath "/Users/martinjedrzejewski/Documents/GitHub/Unity/Hollow Soul - Unity" -runTests -testPlatform EditMode -testFilter Hollow.Tests.EditMode.Milestone99AiPlayabilityStabilizationTests -logFile /tmp/hollow_m99_tests.log -quit
```
