# M89 Limited Alert Sharing Report

- Added `EnemyStimulusKind.AllyAlert`.
- Added enemy alert-sharing metadata: enabled, radius, cooldown, and minimum source awareness.
- Added local room broadcast through `RoomCombatController.EmitEnemyAllyAlert`.
- Added enemy-side non-recursive broadcast hooks for damage, loud disturbance, and sight/engagement escalation.
- Selected alert-capable enemies: Bone Turret, Spitting Pod, Skeleton Sword, Skeleton Spear, Knight, Hollow Archer, Powder Gunner, Knife Thrower, Repeater Turret, Clockwork Sentry, Hollow Acolyte, Soul Eater, Curse Binder, Grave Lantern.
- Documentation: `Docs/Hollow_M89_Limited_Alert_Sharing.md`.
- Report: `output/reports/m89_limited_alert_sharing.md`.
- M90 should perform a full combat AI QA lock across contact, active windows, movement, disturbance, alert sharing, and bosses.
