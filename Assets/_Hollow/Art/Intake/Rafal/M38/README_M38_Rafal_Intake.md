# M38 Rafal ArtPass Intake

Drop raw Blender/FBX/texture/source files into the matching target folders here. Runtime-ready wrappers still need to live under `Assets/_Hollow/Prefabs/ArtPass/` and remain visual-only.

Rules:
- Keep pivots centered and meter-scale sane.
- Do not add gameplay colliders or gameplay scripts to visual prefabs.
- Use low-poly, low-hierarchy geometry suitable for Vision Pro bounded tabletop.
- Keep names stable: `AP_<PresentationPrefabRole>` for runtime wrappers.

Critical targets:
- player_body: Player Body -> Assets/_Hollow/Prefabs/ArtPass/AP_Player.prefab
- enemy_normal: Enemy Normal -> Assets/_Hollow/Prefabs/ArtPass/AP_EnemyNormal.prefab
- boss_stone_warden: Boss Stone Warden -> Assets/_Hollow/Prefabs/ArtPass/AP_EnemyBoss.prefab
- floor_tile: Room Floor Tile -> Assets/_Hollow/Prefabs/ArtPass/AP_RoomFloor.prefab
- rock_obstacle: Rock Obstacle -> Assets/_Hollow/Prefabs/ArtPass/AP_RoomObstacleRock.prefab
- door_active: Door Active -> Assets/_Hollow/Prefabs/ArtPass/AP_DoorActive.prefab
- door_locked: Door Locked -> Assets/_Hollow/Prefabs/ArtPass/AP_DoorLocked.prefab
- door_cleared: Door Cleared -> Assets/_Hollow/Prefabs/ArtPass/AP_DoorCleared.prefab
- player_projectile: Player Projectile -> Assets/_Hollow/Prefabs/ArtPass/AP_Projectile.prefab
- enemy_projectile: Enemy Projectile -> Assets/_Hollow/Prefabs/ArtPass/AP_EnemyProjectile.prefab
- reward_pickup: Reward Pickup -> Assets/_Hollow/Prefabs/ArtPass/AP_RewardPickup.prefab
- boss_key: Boss Key Pickup -> Assets/_Hollow/Prefabs/ArtPass/AP_BossKeyPickup.prefab
- hub_shop: Hub Shop Stand -> Assets/_Hollow/Prefabs/ArtPass/AP_HubShop.prefab
- hub_shop_card: Hub Shop Card -> Assets/_Hollow/Prefabs/ArtPass/AP_HubShopCard.prefab
- branch_portal: Branch Portal -> Assets/_Hollow/Prefabs/ArtPass/AP_NextBranchPortal.prefab
- next_world_portal: Next World Portal -> Assets/_Hollow/Prefabs/ArtPass/AP_HubReturnPortal.prefab
