# M84 Weapon-User Enemies Report

- Added enemy definitions: Skeleton Sword, Skeleton Spear, Knight, Giant.
- Added runtime kind: `WeaponMelee`.
- Added guard tiers: `Small`, `Medium`, `Heavy`.
- Knight uses `Medium` shield reduction in V1.
- Encounter ids: m84_skeleton_patrol, m84_spear_lane, m84_knight_shield_line, m84_giant_pressure, m84_weapon_battlefield.
- Battlefield room ids: m84_skeleton_patrol_field, m84_spear_lane_field, m84_knight_shield_line_field, m84_giant_pressure_field, m84_mixed_weapon_battlefield.
- Catalogue Markdown: `Docs/Hollow_M84_Weapon_User_Enemies.md`.
- Catalogue PDF target: `output/pdf/Hollow_M84_Weapon_User_Enemies.pdf`.
- Local PDF extraction verification: passed with `pypdf`.
- Unity batchmode generator/test execution: blocked by Unity licensing client reconnect failures on 2026-05-03; no C# compile errors were reported before the licensing loop.
