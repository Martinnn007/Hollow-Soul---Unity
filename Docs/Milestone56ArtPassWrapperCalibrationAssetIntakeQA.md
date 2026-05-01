# M56: ArtPass Wrapper Calibration + Asset Intake QA

M56 establishes the beta ArtPass wrapper contract.

- Every `AP_*` / `VFX_*` prefab should be visible at root scale `1,1,1`.
- Rendered art should be centered on X/Z and sit on local `y = 0`.
- Visual prefabs must not own gameplay colliders or gameplay scripts.
- Catalog bindings must resolve the active prefab used by gameplay and Room Designer Scene Mode.
- Generator output: `output/reports/m56_artpass_prefab_calibration.*` and `output/pdf/Hollow_M56_ArtPass_Wrapper_Calibration_Asset_Intake_QA.pdf`.

