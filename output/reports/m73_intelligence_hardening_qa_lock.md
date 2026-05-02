# M73 Intelligence Hardening + QA Lock Report

Status: blocked on Unity batchmode licensing before full QA lock.

## Completed Hardening

- M72 compile blocker fixed: runtime enemies now carry a spawn index used by instinct wander seeding.
- Attack-priority intelligence bonus is strict to Tactical and Cunning only.
- Prey/endangered contact behavior has explicit coverage.
- Continue/save snapshots can use live runtime intelligence and disposition values for the active room.
- PDF validation uses pypdf text extraction through `tools/verify_m72_enemy_intelligence_pdf.py`.

## Verification

- `python3 tools/verify_m72_enemy_intelligence_pdf.py`: passed (`ok pages=2 chars=2592`).
- Current Unity editor log scan: no current `error CS`, `CS0103`, or `spawnIndex` compile errors found after the script reload.
- Full EditMode batch run attempted with `-runTests -testPlatform EditMode`; no test XML was produced.

## Blocker

Unity batchmode did not reach compilation or tests. It failed during licensing initialization:

`[Licensing::Module] Error: Licensing initialization failed after 74.83s`

M73 is therefore not considered fully locked until Unity licensing is healthy and the full EditMode suite passes.

## Required Rerun

Run the full EditMode suite and confirm these suites pass as part of the all-up result:

- `Hollow.Tests.EditMode.Milestone72EnemyIntelligenceTests`
- `Hollow.Tests.EditMode.Milestone70PlayerEnemyKnockbackBodyWeightTests`
- `Hollow.Tests.EditMode.Milestone53BossRosterFrameworkTests`
