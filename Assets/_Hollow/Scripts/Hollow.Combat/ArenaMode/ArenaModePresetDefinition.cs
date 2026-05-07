using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    [CreateAssetMenu(menuName = "Hollow/Arena Mode Preset", fileName = "ArenaPreset_New")]
    public sealed class ArenaModePresetDefinition : ScriptableObject
    {
        [SerializeField] private string presetId = "arena_custom";
        [SerializeField] private string displayName = "Custom Arena";
        [SerializeField] private ArenaRoomSize roomSize = ArenaRoomSize.Medium;
        [SerializeField] private ArenaLayoutStyle layoutStyle = ArenaLayoutStyle.Cover;
        [SerializeField] private ArenaObstaclePreset obstaclePreset = ArenaObstaclePreset.LightCover;
        [SerializeField] private bool survivalMode;
        [SerializeField] private TextAsset curatedRoomRuntimeJson;
        [SerializeField] private bool curatedLocked;
        [SerializeField] private int playerHp = 6;
        [SerializeField] private int playerDamageBonus;
        [SerializeField] private float playerSpeedMetersPerSecond = PlayerMovementController.DefaultSpeedMetersPerSecond;
        [SerializeField] private List<ArenaModeWaveDefinition> waves = new();

        public string PresetId => string.IsNullOrWhiteSpace(presetId) ? name : presetId;

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? PresetId : displayName;

        public ArenaRoomSize RoomSize => roomSize;

        public ArenaLayoutStyle LayoutStyle => layoutStyle;

        public ArenaObstaclePreset ObstaclePreset => obstaclePreset;

        public bool SurvivalMode => survivalMode;

        public TextAsset CuratedRoomRuntimeJson => curatedRoomRuntimeJson;

        public bool HasCuratedRoom => curatedRoomRuntimeJson != null;

        public bool CuratedLocked => curatedLocked;

        public int PlayerHp => Mathf.Clamp(playerHp, ArenaModeRuntimeSettings.MinPlayerHp, ArenaModeRuntimeSettings.MaxPlayerHp);

        public int PlayerDamageBonus => Mathf.Clamp(playerDamageBonus, ArenaModeRuntimeSettings.MinDamageBonus, ArenaModeRuntimeSettings.MaxDamageBonus);

        public float PlayerSpeedMetersPerSecond => Mathf.Clamp(playerSpeedMetersPerSecond, ArenaModeRuntimeSettings.MinPlayerSpeed, ArenaModeRuntimeSettings.MaxPlayerSpeed);

        public IReadOnlyList<ArenaModeWaveDefinition> Waves => waves ??= new List<ArenaModeWaveDefinition>();

        public void Configure(
            string nextPresetId,
            string nextDisplayName,
            ArenaRoomSize nextRoomSize,
            ArenaLayoutStyle nextLayoutStyle,
            ArenaObstaclePreset nextObstaclePreset,
            bool nextSurvivalMode,
            int nextPlayerHp,
            int nextPlayerDamageBonus,
            float nextPlayerSpeedMetersPerSecond,
            IEnumerable<ArenaModeWaveDefinition> nextWaves,
            TextAsset nextCuratedRoomRuntimeJson = null,
            bool nextCuratedLocked = false)
        {
            presetId = string.IsNullOrWhiteSpace(nextPresetId) ? "arena_custom" : nextPresetId;
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? presetId : nextDisplayName;
            roomSize = nextRoomSize;
            layoutStyle = nextLayoutStyle;
            obstaclePreset = nextObstaclePreset;
            survivalMode = nextSurvivalMode;
            curatedRoomRuntimeJson = nextCuratedRoomRuntimeJson;
            curatedLocked = nextCuratedLocked && curatedRoomRuntimeJson != null;
            playerHp = Mathf.Clamp(nextPlayerHp, ArenaModeRuntimeSettings.MinPlayerHp, ArenaModeRuntimeSettings.MaxPlayerHp);
            playerDamageBonus = Mathf.Clamp(nextPlayerDamageBonus, ArenaModeRuntimeSettings.MinDamageBonus, ArenaModeRuntimeSettings.MaxDamageBonus);
            playerSpeedMetersPerSecond = Mathf.Clamp(nextPlayerSpeedMetersPerSecond, ArenaModeRuntimeSettings.MinPlayerSpeed, ArenaModeRuntimeSettings.MaxPlayerSpeed);
            waves = nextWaves?.Where(wave => wave != null).Select(wave => wave.Clone()).ToList() ?? new List<ArenaModeWaveDefinition>();
        }

        public ArenaModeRuntimeSettings CreateRuntimeSettings()
        {
            var settings = new ArenaModeRuntimeSettings
            {
                PresetId = PresetId,
                DisplayName = DisplayName,
                RoomSize = RoomSize,
                LayoutStyle = LayoutStyle,
                ObstaclePreset = ObstaclePreset,
                SurvivalMode = SurvivalMode,
                CuratedRoomRuntimeJson = CuratedRoomRuntimeJson,
                CuratedLocked = CuratedLocked,
                PlayerHp = PlayerHp,
                PlayerDamageBonus = PlayerDamageBonus,
                PlayerSpeedMetersPerSecond = PlayerSpeedMetersPerSecond
            };

            foreach (var wave in Waves)
            {
                if (wave != null)
                {
                    settings.Waves.Add(wave.Clone());
                }
            }

            settings.EnsurePlayableDefaults();
            return settings;
        }

        public IReadOnlyList<string> ValidateForArena(EnemyCatalog catalog = null)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(PresetId))
            {
                errors.Add("Preset id is required.");
            }

            if (PlayerHp < ArenaModeRuntimeSettings.MinPlayerHp || PlayerHp > ArenaModeRuntimeSettings.MaxPlayerHp)
            {
                errors.Add($"Player HP must be {ArenaModeRuntimeSettings.MinPlayerHp}-{ArenaModeRuntimeSettings.MaxPlayerHp}.");
            }

            if (PlayerDamageBonus < ArenaModeRuntimeSettings.MinDamageBonus || PlayerDamageBonus > ArenaModeRuntimeSettings.MaxDamageBonus)
            {
                errors.Add($"Player damage bonus must be {ArenaModeRuntimeSettings.MinDamageBonus}-{ArenaModeRuntimeSettings.MaxDamageBonus}.");
            }

            if (PlayerSpeedMetersPerSecond < ArenaModeRuntimeSettings.MinPlayerSpeed || PlayerSpeedMetersPerSecond > ArenaModeRuntimeSettings.MaxPlayerSpeed)
            {
                errors.Add($"Player speed must be {ArenaModeRuntimeSettings.MinPlayerSpeed:0.0}-{ArenaModeRuntimeSettings.MaxPlayerSpeed:0.0} m/s.");
            }

            if (Waves.Count == 0 || Waves.All(wave => wave == null || wave.Groups.Count == 0))
            {
                errors.Add("At least one wave with one enemy group is required.");
            }

            if (CuratedLocked && CuratedRoomRuntimeJson == null)
            {
                errors.Add("Locked curated presets require a curated runtime room JSON.");
            }

            if (CuratedRoomRuntimeJson != null)
            {
                if (!HollowRuntimeV2Importer.TryImport(CuratedRoomRuntimeJson.text, out var curatedRoom, out var importError))
                {
                    errors.Add($"Curated room JSON failed to import: {importError}");
                }
                else
                {
                    if (curatedRoom.EnemySpawns == null || curatedRoom.EnemySpawns.Count == 0)
                    {
                        errors.Add("Curated room JSON must include at least one enemy spawn anchor.");
                    }

                    if (curatedRoom.SafeStart?.position == null)
                    {
                        errors.Add("Curated room JSON must include a player safe start.");
                    }

                    var navMeshCatalog = RoomNavMeshCatalogDefinition.LoadDefault();
                    if (navMeshCatalog == null)
                    {
                        errors.Add($"Curated room '{curatedRoom.Id}' cannot verify NavMesh: {RoomNavMeshCatalogDefinition.MissingCatalogMessage()}.");
                    }
                    else if (!navMeshCatalog.TryGetNavMeshData(curatedRoom.Id, out var navMeshData) || navMeshData == null)
                    {
                        errors.Add($"Curated room '{curatedRoom.Id}' is missing its Unity NavMesh bake: {RoomNavMeshCatalogDefinition.MissingBakeMessage(curatedRoom.Id)}.");
                    }
                }
            }

            var resolvedCatalog = catalog != null ? catalog : EnemyCatalog.CreateRuntimeDefault();
            foreach (var wave in Waves.Where(wave => wave != null))
            {
                foreach (var group in wave.Groups.Where(group => group != null))
                {
                    if (group.Count <= 0)
                    {
                        errors.Add($"{wave.DisplayName}: group {group.SpawnKind} must have count > 0.");
                    }

                    if (resolvedCatalog.Resolve(group.SpawnKind) == null)
                    {
                        errors.Add($"{wave.DisplayName}: unknown spawn kind '{group.SpawnKind}'.");
                    }
                }
            }

            return errors;
        }
    }

    [Serializable]
    public sealed class ArenaModeWaveDefinition
    {
        [SerializeField] private string displayName = "Wave 1";
        [SerializeField] private float spawnDelaySeconds;
        [SerializeField] private List<ArenaModeEnemyGroupDefinition> groups = new();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Wave" : displayName;

        public float SpawnDelaySeconds => Mathf.Max(0f, spawnDelaySeconds);

        public IReadOnlyList<ArenaModeEnemyGroupDefinition> Groups => groups ??= new List<ArenaModeEnemyGroupDefinition>();

        public void Configure(string nextDisplayName, float nextSpawnDelaySeconds, IEnumerable<ArenaModeEnemyGroupDefinition> nextGroups)
        {
            displayName = string.IsNullOrWhiteSpace(nextDisplayName) ? "Wave" : nextDisplayName;
            spawnDelaySeconds = Mathf.Max(0f, nextSpawnDelaySeconds);
            groups = nextGroups?.Where(group => group != null).Select(group => group.Clone()).ToList() ?? new List<ArenaModeEnemyGroupDefinition>();
        }

        public ArenaModeWaveDefinition Clone()
        {
            var clone = new ArenaModeWaveDefinition();
            clone.Configure(DisplayName, SpawnDelaySeconds, Groups);
            return clone;
        }
    }

    [Serializable]
    public sealed class ArenaModeEnemyGroupDefinition
    {
        [SerializeField] private string spawnKind = "spawnEnemyNormal";
        [SerializeField] private int count = 1;
        [SerializeField] private ArenaSpawnPattern spawnPattern = ArenaSpawnPattern.OuterRing;
        [SerializeField] private ArenaGroupingMode groupingMode = ArenaGroupingMode.LoosePack;
        [SerializeField] private ArenaPatrolIntent patrolIntent = ArenaPatrolIntent.None;
        [SerializeField] private float spawnDelaySeconds;

        public string SpawnKind => string.IsNullOrWhiteSpace(spawnKind) ? "spawnEnemyNormal" : spawnKind;

        public int Count => Mathf.Clamp(count, 1, 32);

        public ArenaSpawnPattern SpawnPattern => spawnPattern;

        public ArenaGroupingMode GroupingMode => groupingMode;

        public ArenaPatrolIntent PatrolIntent => patrolIntent;

        public float SpawnDelaySeconds => Mathf.Max(0f, spawnDelaySeconds);

        public void Configure(
            string nextSpawnKind,
            int nextCount,
            ArenaSpawnPattern nextSpawnPattern,
            ArenaGroupingMode nextGroupingMode,
            ArenaPatrolIntent nextPatrolIntent,
            float nextSpawnDelaySeconds = 0f)
        {
            spawnKind = string.IsNullOrWhiteSpace(nextSpawnKind) ? "spawnEnemyNormal" : nextSpawnKind;
            count = Mathf.Clamp(nextCount, 1, 32);
            spawnPattern = nextSpawnPattern;
            groupingMode = nextGroupingMode;
            patrolIntent = nextPatrolIntent;
            spawnDelaySeconds = Mathf.Max(0f, nextSpawnDelaySeconds);
        }

        public ArenaModeEnemyGroupDefinition Clone()
        {
            var clone = new ArenaModeEnemyGroupDefinition();
            clone.Configure(SpawnKind, Count, SpawnPattern, GroupingMode, PatrolIntent, SpawnDelaySeconds);
            return clone;
        }
    }

    public sealed class ArenaModeRuntimeSettings
    {
        public const int MinPlayerHp = 1;
        public const int MaxPlayerHp = 30;
        public const int MinDamageBonus = 0;
        public const int MaxDamageBonus = 10;
        public const float MinPlayerSpeed = 1f;
        public const float MaxPlayerSpeed = 8f;

        public string PresetId = "arena_custom";
        public string DisplayName = "Custom Arena";
        public ArenaRoomSize RoomSize = ArenaRoomSize.Medium;
        public ArenaLayoutStyle LayoutStyle = ArenaLayoutStyle.Cover;
        public ArenaObstaclePreset ObstaclePreset = ArenaObstaclePreset.LightCover;
        public bool SurvivalMode;
        public TextAsset CuratedRoomRuntimeJson;
        public bool CuratedLocked;
        public int PlayerHp = RoomCombatController.PlayerMaxHealth;
        public int PlayerDamageBonus;
        public float PlayerSpeedMetersPerSecond = PlayerMovementController.DefaultSpeedMetersPerSecond;
        public readonly List<ArenaModeWaveDefinition> Waves = new();

        public bool HasCuratedRoom => CuratedRoomRuntimeJson != null;

        public void EnsurePlayableDefaults()
        {
            PresetId = string.IsNullOrWhiteSpace(PresetId) ? "arena_custom" : PresetId;
            DisplayName = string.IsNullOrWhiteSpace(DisplayName) ? PresetId : DisplayName;
            PlayerHp = Mathf.Clamp(PlayerHp, MinPlayerHp, MaxPlayerHp);
            PlayerDamageBonus = Mathf.Clamp(PlayerDamageBonus, MinDamageBonus, MaxDamageBonus);
            PlayerSpeedMetersPerSecond = Mathf.Clamp(PlayerSpeedMetersPerSecond, MinPlayerSpeed, MaxPlayerSpeed);
            if (Waves.Count == 0)
            {
                Waves.Add(ArenaModeDefaults.CreateWave(
                    "Wave 1",
                    ArenaModeDefaults.CreateGroup("spawnEnemyNormal", 3, ArenaSpawnPattern.OuterRing, ArenaGroupingMode.LoosePack)));
            }
        }

        public ArenaModeRuntimeSettings Clone()
        {
            var clone = new ArenaModeRuntimeSettings
            {
                PresetId = PresetId,
                DisplayName = DisplayName,
                RoomSize = RoomSize,
                LayoutStyle = LayoutStyle,
                ObstaclePreset = ObstaclePreset,
                SurvivalMode = SurvivalMode,
                CuratedRoomRuntimeJson = CuratedRoomRuntimeJson,
                CuratedLocked = CuratedLocked && CuratedRoomRuntimeJson != null,
                PlayerHp = PlayerHp,
                PlayerDamageBonus = PlayerDamageBonus,
                PlayerSpeedMetersPerSecond = PlayerSpeedMetersPerSecond
            };

            foreach (var wave in Waves)
            {
                if (wave != null)
                {
                    clone.Waves.Add(wave.Clone());
                }
            }

            clone.EnsurePlayableDefaults();
            return clone;
        }
    }

    public static class ArenaModeDefaults
    {
        public static ArenaModeWaveDefinition CreateWave(string displayName, params ArenaModeEnemyGroupDefinition[] groups)
        {
            var wave = new ArenaModeWaveDefinition();
            wave.Configure(displayName, 0f, groups ?? Array.Empty<ArenaModeEnemyGroupDefinition>());
            return wave;
        }

        public static ArenaModeEnemyGroupDefinition CreateGroup(
            string spawnKind,
            int count,
            ArenaSpawnPattern pattern,
            ArenaGroupingMode grouping,
            ArenaPatrolIntent patrol = ArenaPatrolIntent.None)
        {
            var group = new ArenaModeEnemyGroupDefinition();
            group.Configure(spawnKind, count, pattern, grouping, patrol);
            return group;
        }
    }
}
