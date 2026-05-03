# M85 Creature Action Expansion Report

- Added creature enemies: Hollow Bird, Hollow Beast.
- Body-creature runtime owners: spawnEnemyNormal, spawnEnemyFlying, spawnEnemyFast, spawnEnemyHeavy, spawnEnemyCharger, spawnEnemySplitter, spawnEnemyRat, spawnEnemySpider, spawnEnemyHollowBird, spawnEnemyHollowBeast.
- Promoted creature actions: `short_backstep`, `warning_feint`, `fly_strafe`, `dive_feint`, `evasive_skitter`, `snap_combo`, `guarded_shove`, `slow_overhead_slam`, `short_recover_hop`, `shoulder_check`, `splitter_backstep`, `cleave_feint`, `skitter_retreat`, `panic_pounce`, `alarm_squeal`, `panic_flee`, `web_feint`, `swoop_peck`, `claw_dive`, `wing_retreat`, `caw_signal`, `leap_bite`, `body_check`, `leap_back`, `howl_signal`.
- Same-family signal stimulus: `CreatureSignal`.
- New runtime kinds: `CreatureMove`, `CreatureSignal`.
- Encounter ids: m85_hollow_bird_perch, m85_hollow_beast_den, m85_rat_spider_signal, m85_mixed_creature_scramble.
- Curated room ids: m85_hollow_bird_perch_room, m85_hollow_beast_den, m85_rat_spider_signal_room, m85_mixed_body_creature_scramble.
- Catalogue Markdown: `Docs/Hollow_M85_Creature_Action_Expansion.md`.
- Catalogue PDF target: `output/pdf/Hollow_M85_Creature_Action_Expansion.pdf`.
- Unity batchmode generator ran successfully via `Hollow.Editor.Generation.Milestone85AssetGenerator.Generate`.
- Unity batchmode validation ran successfully via `Hollow.Editor.Validation.Milestone85Validator.Validate`.
- PDF text extraction passed with `tools/verify_m85_creature_action_expansion_pdf.py`.
- Unity `-runTests` compiled scripts cleanly but did not emit `output/reports/m85-editmode-results.xml`; rerun the EditMode suite from the Unity Test Runner UI or once the batch Test Runner path is healthy.
