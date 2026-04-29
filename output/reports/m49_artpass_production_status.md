# M49 ArtPass Production Integration II

Generated: 2026-04-29T07:51:19.997713Z

## Summary

- Total tracked visible roles: 41
- Production ready: 0
- Prototype fallback warnings: 37
- Missing bindings: 4
- Unsafe prefabs: 0

## Direct Replacement Workflow

- Replace active `AP_*` or `VFX_*` prefabs under `Assets/_Hollow/Prefabs/ArtPass/`.
- Keep `PresentationVisualMarker` on the prefab root with the matching role.
- Do not add gameplay colliders or gameplay scripts to visual prefabs.
- Room Designer Scene Mode previews the same active ArtPass catalog as gameplay.

## Core Vertical-Slice Pack

| Group | Target | Status | Prefab |
| --- | --- | --- | --- |
| Boss | Enemy Boss | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyBoss.prefab` |
| Doors | Door Active | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorActive.prefab` |
| Doors | Door Cleared | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorCleared.prefab` |
| Doors | Door Locked | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorLocked.prefab` |
| Enemies | Enemy Normal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyNormal.prefab` |
| Hub | Hub Return Portal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubReturnPortal.prefab` |
| Hub | Hub Shop | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubShop.prefab` |
| Hub | Hub Shop Card | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubShopCard.prefab` |
| Hub | Next Branch Portal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_NextBranchPortal.prefab` |
| Player | Player | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_Player.prefab` |
| Projectiles | Enemy Projectile | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyProjectile.prefab` |
| Projectiles | Projectile | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_Projectile.prefab` |
| Rewards | Boss Key Pickup | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_BossKeyPickup.prefab` |
| Rewards | Reward Pickup | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RewardPickup.prefab` |
| Rooms | Room Floor | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RoomFloor.prefab` |
| Rooms | Room Obstacle Rock | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RoomObstacleRock.prefab` |

## All Visible Roles

| Group | Target | Status | Notes |
| --- | --- | --- | --- |
| Boss | Enemy Boss | PrototypeFallback | EnemyBoss still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Doors | Door Active | PrototypeFallback | DoorActive still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Doors | Door Cleared | PrototypeFallback | DoorCleared still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Doors | Door Locked | PrototypeFallback | DoorLocked still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Doors | Door Unavailable | PrototypeFallback | DoorUnavailable still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Doors | Secret Door Debug | PrototypeFallback | SecretDoorDebug still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Charger | PrototypeFallback | EnemyCharger still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Fast | PrototypeFallback | EnemyFast still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Flying | PrototypeFallback | EnemyFlying still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Heavy | PrototypeFallback | EnemyHeavy still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Normal | PrototypeFallback | EnemyNormal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Splitter | PrototypeFallback | EnemySplitter still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Enemies | Enemy Turret | PrototypeFallback | EnemyTurret still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Equipment | Armor | PrototypeFallback | Armor still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Equipment | Weapon Melee | PrototypeFallback | WeaponMelee still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Equipment | Weapon Ranged | PrototypeFallback | WeaponRanged still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Hazards | Explosive Barrel | MissingBinding | Expected active ArtPass prefab is currently missing on disk: Assets/_Hollow/Prefabs/ArtPass/AP_ExplosiveBarrel.prefab. Run the Unity M49 generator once licensing is available. |
| Hazards | Hazard Coin Drop | MissingBinding | Expected active ArtPass prefab is currently missing on disk: Assets/_Hollow/Prefabs/ArtPass/AP_HazardCoinDrop.prefab. Run the Unity M49 generator once licensing is available. |
| Hazards | Room Hazard Spike | MissingBinding | Expected active ArtPass prefab is currently missing on disk: Assets/_Hollow/Prefabs/ArtPass/AP_RoomHazardSpike.prefab. Run the Unity M49 generator once licensing is available. |
| Hazards | Standard Barrel | MissingBinding | Expected active ArtPass prefab is currently missing on disk: Assets/_Hollow/Prefabs/ArtPass/AP_StandardBarrel.prefab. Run the Unity M49 generator once licensing is available. |
| Hub | Hub Return Portal | PrototypeFallback | HubReturnPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Hub | Hub Shop | PrototypeFallback | HubShop still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Hub | Hub Shop Card | PrototypeFallback | HubShopCard still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Hub | Next Branch Portal | PrototypeFallback | NextBranchPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Items | Active Item Pickup | PrototypeFallback | ActiveItemPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Items | Consumable Card Pickup | PrototypeFallback | ConsumableCardPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Player | Player | PrototypeFallback | Player still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Projectiles | Enemy Projectile | PrototypeFallback | EnemyProjectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Projectiles | Projectile | PrototypeFallback | Projectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Rewards | Boss Key Pickup | PrototypeFallback | BossKeyPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Rewards | Reward Pickup | PrototypeFallback | RewardPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Rooms | Room Floor | PrototypeFallback | RoomFloor still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| Rooms | Room Obstacle Rock | PrototypeFallback | RoomObstacleRock still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Door Unlock | PrototypeFallback | VfxDoorUnlock still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Enemy Death | PrototypeFallback | VfxEnemyDeath still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Enemy Hit | PrototypeFallback | VfxEnemyHit still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Player Hit | PrototypeFallback | VfxPlayerHit still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Portal Complete | PrototypeFallback | VfxPortalComplete still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Projectile Fire | PrototypeFallback | VfxProjectileFire still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Reward Claim | PrototypeFallback | VfxRewardClaim still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
| VFX | VFX Room Clear | PrototypeFallback | VfxRoomClear still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. |
