# M86 Ranged + Firearm Enemies Report

- Added enemy definitions: Hollow Archer, Powder Gunner, Knife Thrower, Repeater Turret, Clockwork Sentry.
- Added profile-specific ranged behavior tree checks with `CanStartRangedAction`.
- Non-boss ranged runtime now supports single, fan, and radial projectile patterns from attack profiles.
- Encounter ids: m86_archer_gallery, m86_powder_checkpoint, m86_thrower_alley, m86_repeater_crossfire, m86_clockwork_pattern_hall.
- Curated ranged room ids: m86_archer_gallery_room, m86_powder_checkpoint_room, m86_thrower_alley_room, m86_repeater_crossfire_room, m86_clockwork_pattern_hall_room.
- Catalogue Markdown: `Docs/Hollow_M86_Ranged_Firearm_Enemies.md`.
- Catalogue PDF target: `output/pdf/Hollow_M86_Ranged_Firearm_Enemies.pdf`.
- Local PDF extraction verification script: `tools/verify_m86_ranged_firearm_enemies_pdf.py`.
- Unity batchmode validation and EditMode results should be recorded under `output/reports/`.
