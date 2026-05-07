# M105 Unity Behavior Family Migration Report

- Schema version: `104`.
- Migrated families: critters, chasers, weapon users, ranged/firearm, magic/ghost.
- Runtime mode: all current non-boss enemies resolve `UnityBehaviorGraph`.
- Hollow source of truth: action profiles, attack profiles, active windows, NavMesh, threat director, saves.
- Graph contracts:
  - `m105_family_chasers_unity_behavior` / `ChaserFamily` / fallback `EmergencyOnly`.
  - `m105_family_critters_unity_behavior` / `CritterFamily` / fallback `EmergencyOnly`.
  - `m105_family_weapon_users_unity_behavior` / `WeaponUserFamily` / fallback `EmergencyOnly`.
  - `m105_family_ranged_firearm_unity_behavior` / `RangedFirearmFamily` / fallback `EmergencyOnly`.
  - `m105_family_magic_ghost_unity_behavior` / `MagicGhostFamily` / fallback `EmergencyOnly`.
- Docs: `Docs/Hollow_M105_Unity_Behavior_Family_Migration.md`.
