# Hollow M25 Vertical Slice Content Lock

- Result: Passed
- Generated: 2026-04-28T02:38:19.5878790Z
- Unity: 6000.4.1f1
- Git: main @ 2c3f556
- Branch: `m20_branch_features_v1`
- Seed: `15001`
- Rooms: `8`
- Connections: `16`
- Fixture rooms: `5`
- Approved designer rooms: `5`
- Shop offers: `3`
- Next-branch portals: `3`

| Check | Result | Notes | Remediation |
| --- | --- | --- | --- |
| lock-definition | Passed | M25 lock asset pins branch identity, seed, catalogs, platform QA, and slice counts. | OK |
| branch-content | Passed | Locked branch generated with 8 rooms, 16 directional connections, 5 fixtures, and 5 approved rooms.<br>Boss key source: room_04; secret: room_06; boss: boss_01. | OK |
| hub-shop-portals | Passed | Inter-branch hub exposes three shop offers and three seeded next-branch portal choices. | OK |
| artpass-lock | Passed | Required ArtPass roles/cues resolved without prototype fallback. Warnings: 0. | OK |
| platform-checklist | Passed | Windows, Vision Pro bounded, and Vision Pro immersive have equal vertical-slice checklist coverage. | OK |
| m0-m24-audit | Passed | M0-M24 validators passed: 25/25. | OK |

## Manual QA Checklist
- Windows: start New Run with the locked seed, clear combat rooms, collect rewards, unlock the boss door, defeat boss, enter hub, buy one shop card, and inspect all three next-branch portals.
- Windows: quit after a checkpoint and Continue to confirm room/reward/key/shop/hub state restores.
- Vision Pro bounded: repeat route smoke with tabletop scale 0.1, HUD/minimap unscaled, readable door/shop/portal cards, and no ArtPass visual collider takeover.
- Vision Pro immersive: repeat route smoke at full world scale, verify comfort posture/readability, boss/projectile clarity, and next-branch portal placement.
- All platforms: confirm transient designer/sample sessions remain excluded from run saves and profile mutation.
