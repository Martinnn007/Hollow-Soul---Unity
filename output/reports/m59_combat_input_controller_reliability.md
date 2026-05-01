# M59: Combat Input + Controller Reliability Pass

Generated UTC: 2026-05-01T00:42:29.486813+00:00

Status: Fallback report generated outside Unity because batchmode was blocked by Unity licensing initialization.
Action: Re-run Hollow/Generation/Generate Milestone 65 Assets from Unity to replace these fallback reports with asset-scanned reports, BetaContentLock_M63.asset, and BetaQaChecklist_M64.asset.
This document is useful for handoff/planning, but it is not a passing Unity validation report.

## Source Notes


M59 locks gameplay input around the Unity Input System.

- Keyboard target: WASD move, arrows aim, J light, K heavy, E interact, Tab swap, Q active, F card, Shift guard, Escape pause.
- DualShock 5 target: left stick move, right stick aim, Cross interact, L1 swap, R1 light, R2 heavy, Triangle active, Square card, L2 guard, Options pause.
- Gameplay routes should not use legacy `UnityEngine.Input`.
- Debug UI should prefer visible buttons/toggles where function keys are unreliable.
