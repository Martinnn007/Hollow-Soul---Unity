# M49 ArtPass Production Integration II

Generated: 2026-05-02T00:16:46.1782370Z

## Summary

- Total tracked visible roles: 48
- Production ready: 0
- Prototype fallback warnings: 37
- Missing bindings: 10
- Unsafe prefabs: 1
- Blocking failures: Yes

## Direct Replacement Workflow

- Replace the active `AP_*` or `VFX_*` prefab under `Assets/_Hollow/Prefabs/ArtPass/`.
- Keep `PresentationVisualMarker` on the prefab root with the matching `PresentationPrefabRole`.
- Do not add gameplay colliders or gameplay scripts to visual prefabs.
- Keep gameplay collision, damage, traversal, rewards, and room layout in runtime code/data only.
- Room Designer Scene Mode previews the same active ArtPass catalog as gameplay.

## Core Vertical-Slice Pack

| Group | Target | Status | Prefab | Warnings | Errors |
| --- | --- | --- | --- | --- | --- |
| Boss | Enemy Boss | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyBoss.prefab` | EnemyBoss still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Active | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorActive.prefab` | DoorActive still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Cleared | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorCleared.prefab` | DoorCleared still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Locked | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_DoorLocked.prefab` | DoorLocked still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Normal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyNormal.prefab` | EnemyNormal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Hub Return Portal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubReturnPortal.prefab` | HubReturnPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Hub Shop | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubShop.prefab` | HubShop still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Hub Shop Card | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_HubShopCard.prefab` | HubShopCard still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Next Branch Portal | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_NextBranchPortal.prefab` | NextBranchPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Player | Player | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_Player.prefab` | Player still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Projectiles | Enemy Projectile | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_EnemyProjectile.prefab` | EnemyProjectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Projectiles | Projectile | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_Projectile.prefab` | Projectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rewards | Boss Key Pickup | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_BossKeyPickup.prefab` | BossKeyPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rewards | Reward Pickup | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RewardPickup.prefab` | RewardPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rooms | Room Floor | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RoomFloor.prefab` | RoomFloor still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rooms | Room Obstacle Rock | PrototypeFallback | `Assets/_Hollow/Prefabs/ArtPass/AP_RoomObstacleRock.prefab` | RoomObstacleRock still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |

## All Visible Roles

| Group | Target | Status | Warnings | Errors |
| --- | --- | --- | --- | --- |
| Boss | Enemy Boss | PrototypeFallback | EnemyBoss still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Chests | Chest Golden | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for ChestGolden. |
| Chests | Chest Normal | UnsafePrefab | OK | Prefab Assets/_Hollow/Prefabs/ArtPass/AP_ChestBasic.prefab/Chest has unsafe renderer bounds (331.60, 109.20, 170.00). |
| Coins | Coin Copper | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for CoinCopper. |
| Coins | Coin Gold | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for CoinGold. |
| Coins | Coin Silver | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for CoinSilver. |
| Doors | Door Active | PrototypeFallback | DoorActive still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Cleared | PrototypeFallback | DoorCleared still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Locked | PrototypeFallback | DoorLocked still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Door Unavailable | PrototypeFallback | DoorUnavailable still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Doors | Secret Door Debug | PrototypeFallback | SecretDoorDebug still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Charger | PrototypeFallback | EnemyCharger still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Fast | PrototypeFallback | EnemyFast still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Flying | PrototypeFallback | EnemyFlying still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Heavy | PrototypeFallback | EnemyHeavy still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Normal | PrototypeFallback | EnemyNormal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Splitter | PrototypeFallback | EnemySplitter still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Enemies | Enemy Turret | PrototypeFallback | EnemyTurret still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Equipment | Armor | PrototypeFallback | Armor still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Equipment | Weapon Melee | PrototypeFallback | WeaponMelee still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Equipment | Weapon Ranged | PrototypeFallback | WeaponRanged still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hazards | Explosive Barrel | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for ExplosiveBarrel. |
| Hazards | Hazard Coin Drop | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for HazardCoinDrop. |
| Hazards | Room Hazard Spike | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for RoomHazardSpike. |
| Hazards | Standard Barrel | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for StandardBarrel. |
| Hub | Hub Return Portal | PrototypeFallback | HubReturnPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Hub Shop | PrototypeFallback | HubShop still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Hub Shop Card | PrototypeFallback | HubShopCard still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Hub | Next Branch Portal | PrototypeFallback | NextBranchPortal still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Items | Active Item Pickup | PrototypeFallback | ActiveItemPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Items | Consumable Card Pickup | PrototypeFallback | ConsumableCardPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Player | Player | PrototypeFallback | Player still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Projectiles | Enemy Projectile | PrototypeFallback | EnemyProjectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Projectiles | Projectile | PrototypeFallback | Projectile still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rewards | Boss Key Pickup | PrototypeFallback | BossKeyPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rewards | Reward Pickup | PrototypeFallback | RewardPickup still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rooms | Room Floor | PrototypeFallback | RoomFloor still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| Rooms | Room Obstacle Rock | PrototypeFallback | RoomObstacleRock still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Chest Open | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for VfxChestOpen. |
| VFX | VFX Coin Pickup | MissingBinding | OK | Presentation catalog is missing an active ArtPass prefab binding for VfxCoinPickup. |
| VFX | VFX Door Unlock | PrototypeFallback | VfxDoorUnlock still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Enemy Death | PrototypeFallback | VfxEnemyDeath still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Enemy Hit | PrototypeFallback | VfxEnemyHit still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Player Hit | PrototypeFallback | VfxPlayerHit still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Portal Complete | PrototypeFallback | VfxPortalComplete still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Projectile Fire | PrototypeFallback | VfxProjectileFire still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Reward Claim | PrototypeFallback | VfxRewardClaim still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
| VFX | VFX Room Clear | PrototypeFallback | VfxRoomClear still appears to use generated primitive placeholder art. This is allowed for M49 but should be replaced by production art later. | OK |
