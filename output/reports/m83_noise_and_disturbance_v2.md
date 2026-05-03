# M83 Noise + Disturbance V2 Report

- Added `EnemyStimulusTier` and tier-aware stimulus APIs.
- Added enemy disturbance fields: hearing sensitivity, escalation threshold, and investigation duration.
- Updated player footsteps, rolls, light/heavy attacks, guard impacts, direct damage, proximity, and bumps to feed tiered disturbance.
- Ordinary body bumps remain harmless, emit `Bump`, and apply light separation.
- Boss runtime remains unchanged.
- Documentation: `Docs/Hollow_M83_Noise_And_Disturbance_V2.md`.
- Report: `output/reports/m83_noise_and_disturbance_v2.md`.
