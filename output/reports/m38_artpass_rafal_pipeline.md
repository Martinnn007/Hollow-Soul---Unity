# M38 ArtPass Integration Sprint With Rafal Pipeline

- Generated: 2026-04-28T02:36:13.2346970Z
- Target catalog: `Assets/_Hollow/Data/ArtPass/M38/ArtPassTargetCatalog_M38.asset`
- Intake folder: `Assets/_Hollow/Art/Intake/Rafal/M38`
- Runtime wrapper folder: `Assets/_Hollow/Prefabs/ArtPass`
- Total targets: 37

## Priority Counts

- Critical: 16
- High: 12
- Medium: 9

## Critical Runtime Targets

- Player Body (Player) - Readable one-piece hero silhouette with subtle soul glow.
- Enemy Normal (EnemyNormal) - Small corrupted toy/soul chaser silhouette.
- Boss Stone Warden (EnemyBoss) - Large guardian silhouette with readable charge/burst telegraph surfaces.
- Room Floor Tile (RoomFloor) - Dark toy-diorama floor module that remains readable under grid/lighting.
- Rock Obstacle (RoomObstacleRock) - 1m gameplay blocker visual, bottom sits exactly at y=0.
- Door Active (DoorActive) - Open/usable branch door state.
- Door Locked (DoorLocked) - Clearly locked boss-key door state.
- Door Cleared (DoorCleared) - Cleared/unlocked door state with calm green read.
- Player Projectile (Projectile) - Readable small projectile core, not visually confused with enemy shots.
- Enemy Projectile (EnemyProjectile) - Danger-colored projectile core with readable ownership.
- Reward Pickup (RewardPickup) - Generic pickup that reads as valuable from top-down and perspective.
- Boss Key Pickup (BossKeyPickup) - Distinct key reward; must not look like a normal pickup.
- Hub Shop Stand (HubShop) - Compact shop stand readable beside card offers.
- Hub Shop Card (HubShopCard) - Reusable visible offer card frame behind generated text.
- Branch Portal (NextBranchPortal) - Open branch portal, readable from hub camera.
- Next World Portal (HubReturnPortal) - Fourth right-side portal for deeper world/final extraction states.

## What Programming Can Do Without Final Art

- Keep binding generated ArtPass wrappers to runtime roles.
- Validate no visual prefab takes over gameplay collision or scripts.
- Replace AP_* generated placeholders with Rafal-provided prefabs as they arrive.

## What Needs Rafal Input

- Final silhouettes, textures/material mood, VFX timing, and basic animation poses.
- Any target marked Critical should be prioritized before secondary equipment pickup wrappers.
