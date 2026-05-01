# M56: ArtPass Wrapper Calibration + Asset Intake QA

Generated UTC: 2026-05-01T00:42:29.486813+00:00

Status: Fallback report generated outside Unity because batchmode was blocked by Unity licensing initialization.
Action: Re-run Hollow/Generation/Generate Milestone 65 Assets from Unity to replace these fallback reports with asset-scanned reports, BetaContentLock_M63.asset, and BetaQaChecklist_M64.asset.
This document is useful for handoff/planning, but it is not a passing Unity validation report.

## Source Notes


M56 establishes the beta ArtPass wrapper contract.

- Every `AP_*` / `VFX_*` prefab should be visible at root scale `1,1,1`.
- Rendered art should be centered on X/Z and sit on local `y = 0`.
- Visual prefabs must not own gameplay colliders or gameplay scripts.
- Catalog bindings must resolve the active prefab used by gameplay and Room Designer Scene Mode.
- Generator output: `output/reports/m56_artpass_prefab_calibration.*` and `output/pdf/Hollow_M56_ArtPass_Wrapper_Calibration_Asset_Intake_QA.pdf`.
