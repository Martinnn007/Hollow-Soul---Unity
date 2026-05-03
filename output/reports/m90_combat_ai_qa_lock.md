# M90 Combat AI QA Lock Report

- Added M90 generator, validator, tests, docs, and report contracts.
- Updated stale M83 assertions to match the current M82-M89 behavior stack:
  - prey startle may use lateral/flying local movement instead of only straight retreat;
  - sentinel quiet disturbance holds when the player is outside attackable space;
  - mindless hearing respects authored sensitivity and distance;
  - AllyAlert now participates in the M83 default stimulus tier contract.
- Navigation backend target: `LocalSteering`.
- Contact policy target: all current roster bodies are `ActiveOnly` with no passive hazard.
- M72 priority target: only `Tactical` and `Cunning` get intelligence tie bonuses.
- M90 docs: `Docs/Hollow_M90_Combat_AI_QA_Lock.md`.
- M90 report: `output/reports/m90_combat_ai_qa_lock.md`.
- Unity QA status: blocked on 2026-05-03 by licensing initialization timeout in batch mode, before tests could run. M90 remains unlocked until Unity can compile and run the focused regressions.
- Next recommended milestone: `M91 Preferred Distance + Commitment Tuning V2`.
