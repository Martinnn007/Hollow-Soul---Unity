# Hollow M25 Vertical Slice Content Lock

- Result: Failed
- Generated: 2026-05-03T14:19:55.0897540Z
- Unity: 6000.4.1f1
- Git: main @ bd21a1f
- Branch: `m20_branch_features_v1`
- Seed: `15001`
- Rooms: `8`
- Connections: `20`
- Fixture rooms: `5`
- Approved designer rooms: `46`
- Shop offers: `3`
- Next-branch portals: `3`

| Check | Result | Notes | Remediation |
| --- | --- | --- | --- |
| lock-definition | Passed | M25 lock asset pins branch identity, seed, catalogs, platform QA, and slice counts. | OK |
| branch-content | Passed | Locked branch generated with 8 rooms, 20 directional connections, 5 fixtures, and 46 approved rooms.<br>Boss key source: room_05; secret: room_06; boss: boss_01. | OK |
| hub-shop-portals | Passed | Inter-branch hub exposes three shop offers and three seeded next-branch portal choices. | OK |
| artpass-lock | Failed | ArtPass palette does not resolve material role DesignerSpawnReward.; ArtPass palette does not resolve material role VfxDebug.; ArtPass palette does not resolve material role CombatTelegraphSafe.; ArtPass palette does not resolve material role CombatTelegraphWarning.; ArtPass palette does not resolve material role CombatTelegraphDanger.; ArtPass palette does not resolve material role RoomHazardSpike.; ArtPass palette does not resolve material role RoomBarrel.; ArtPass palette does not resolve material role RoomExplosiveBarrel.; ArtPass palette does not resolve material role DesignerSpike.; ArtPass palette does not resolve material role DesignerBarrel.; ArtPass palette does not resolve material role DesignerExplosiveBarrel.; ArtPass palette does not resolve material role HazardCoinDrop.; ArtPass palette does not resolve material role ChestNormal.; ArtPass palette does not resolve material role ChestGolden.; ArtPass palette does not resolve material role CoinCopper.; ArtPass palette does not resolve material role CoinSilver.; ArtPass palette does not resolve material role CoinGold.; ArtPass palette does not resolve material role DesignerChest.; ArtPass palette does not resolve material role ProjectilePower.; ArtPass palette does not resolve material role EnemySpittingPod.; ArtPass palette does not resolve material role EnemyRat.; ArtPass palette does not resolve material role EnemySpider.; ArtPass palette does not resolve material role EnemyHollowBird.; ArtPass palette does not resolve material role EnemyHollowBeast.; ArtPass palette does not resolve material role EnemySkeletonSword.; ArtPass palette does not resolve material role EnemySkeletonSpear.; ArtPass palette does not resolve material role EnemyKnight.; ArtPass palette does not resolve material role EnemyGiant.; ArtPass palette does not resolve material role EnemyHollowArcher.; ArtPass palette does not resolve material role EnemyPowderGunner.; ArtPass palette does not resolve material role EnemyKnifeThrower.; ArtPass palette does not resolve material role EnemyRepeaterTurret.; ArtPass palette does not resolve material role EnemyClockworkSentry.; ArtPass palette does not resolve material role EnemyHollowAcolyte.; ArtPass palette does not resolve material role EnemyWraith.; ArtPass palette does not resolve material role EnemySoulEater.; ArtPass palette does not resolve material role EnemyCurseBinder.; ArtPass palette does not resolve material role EnemyGraveLantern.; ArtPass audio cue PlayerInvulnerable must have a placeholder clip.; ArtPass audio cue KnockbackImpact must have a placeholder clip.; ArtPass audio cue EnemyWindup must have a placeholder clip.; ArtPass audio cue EnemyCorpseGhost must have a placeholder clip.; ArtPass audio cue DamageBlocked must have a placeholder clip.; ArtPass audio cue ShieldGuardStart must have a placeholder clip.; ArtPass audio cue ShieldBlock must have a placeholder clip.; ArtPass audio cue ShieldParryCounter must have a placeholder clip.; ArtPass audio cue ShieldUnavailable must have a placeholder clip.; ArtPass audio cue HazardHit must have a placeholder clip.; ArtPass audio cue BarrelBreak must have a placeholder clip.; ArtPass audio cue BarrelExplode must have a placeholder clip.; ArtPass audio cue HazardCoinDrop must have a placeholder clip.; ArtPass audio cue ChestOpen must have a placeholder clip.; ArtPass audio cue CoinPickup must have a placeholder clip. | Regenerate M23 ArtPass assets and repair catalog bindings. |
| platform-checklist | Passed | Windows, Vision Pro bounded, and Vision Pro immersive have equal vertical-slice checklist coverage. | OK |
| m0-m24-audit | Failed | M0-M24 validators failed: 6/25. | Milestone9Validator; Milestone11Validator; Milestone12Validator; Milestone15Validator; Milestone17Validator; Milestone23Validator |

## Manual QA Checklist
- Windows: start New Run with the locked seed, clear combat rooms, collect rewards, unlock the boss door, defeat boss, enter hub, buy one shop card, and inspect all three next-branch portals.
- Windows: quit after a checkpoint and Continue to confirm room/reward/key/shop/hub state restores.
- Vision Pro bounded: repeat route smoke with tabletop scale 0.1, HUD/minimap unscaled, readable door/shop/portal cards, and no ArtPass visual collider takeover.
- Vision Pro immersive: repeat route smoke at full world scale, verify comfort posture/readability, boss/projectile clarity, and next-branch portal placement.
- All platforms: confirm transient designer/sample sessions remain excluded from run saves and profile mutation.
