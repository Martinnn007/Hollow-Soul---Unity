using System.Collections.Generic;
using System.Linq;
using Hollow.Core.App;
using Hollow.Entities;
using Hollow.Platform;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    [DefaultExecutionOrder(-100)]
    public sealed class ArenaModeController : MonoBehaviour
    {
        private const float NextWaveDelaySeconds = 3f;

        [SerializeField] private ArenaModePresetDefinition[] presets;
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private RoomCombatController roomCombatController;
        [SerializeField] private PlaceholderPlayerController playerController;
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private bool showSetupOnStart = true;
        [SerializeField] private HollowPlatformKind platformKind = HollowPlatformKind.WindowsStandard3D;

        private readonly ArenaModeScoreTracker scoreTracker = new();
        private ArenaModeRuntimeSettings currentSettings;
        private ImportedRoomRuntimeAsset currentRoomAsset;
        private ArenaModeScreen screen;
        private int currentWaveIndex = -1;
        private float nextWaveStartTime;
        private bool arenaRunning;
        private bool arenaComplete;
        private bool waitingForNextWave;
        private AppShellRoute returnRoute = AppShellRoute.MainMenu;
        private string selectedCharacterId = "balanced";

        public IReadOnlyList<ArenaModePresetDefinition> Presets => ResolvePresets();

        public ArenaModeRuntimeSettings CurrentSettings => currentSettings;

        public bool IsArenaRunning => arenaRunning;

        public bool IsArenaComplete => arenaComplete;

        public int CurrentWaveNumber => Mathf.Max(0, currentWaveIndex + 1);

        public int EnemiesRemaining => roomCombatController != null ? roomCombatController.EnemiesRemaining() : 0;

        public ArenaModeScoreTracker ScoreTracker => scoreTracker;

        public bool IsEditorOrDevelopmentLaunch => Debug.isDebugBuild || Application.isEditor;

        public HollowPlatformKind PlatformKind => platformKind;

        public string SelectedCharacterId => selectedCharacterId;

#if UNITY_EDITOR
        public void ConfigureArenaPresetsForEditor(ArenaModePresetDefinition[] nextPresets, bool nextShowSetupOnStart)
        {
            presets = nextPresets ?? System.Array.Empty<ArenaModePresetDefinition>();
            showSetupOnStart = nextShowSetupOnStart;
        }
#endif

        private void Awake()
        {
            ResolveReferences();
            roomCombatController?.ConfigureAutoInitialize(false);
        }

        private void Start()
        {
            ResolveReferences();
            var hadHandoff = ArenaModeHandoff.TryConsume(out var presetId, out var autoStart, out var nextReturnRoute, out var nextPlatformKind, out var nextSelectedCharacterId);
            returnRoute = hadHandoff ? nextReturnRoute : AppShellRoute.MainMenu;
            if (hadHandoff)
            {
                platformKind = nextPlatformKind;
                selectedCharacterId = string.IsNullOrWhiteSpace(nextSelectedCharacterId) ? "balanced" : nextSelectedCharacterId;
            }

            ApplyPlatformKind(platformKind);
            ConfigureGameplayCameraFollow();
            EnsureScreen();
            currentSettings = ResolvePreset(presetId)?.CreateRuntimeSettings()
                ?? ResolvePresets().FirstOrDefault()?.CreateRuntimeSettings()
                ?? CreateFallbackSettings();

            if (hadHandoff && autoStart)
            {
                StartArena(currentSettings.Clone());
                return;
            }

            if (showSetupOnStart)
            {
                screen.ShowSetup(currentSettings.Clone());
            }
        }

        private void Update()
        {
            if (!arenaRunning || !waitingForNextWave || Time.time < nextWaveStartTime)
            {
                screen?.RefreshOverlay();
                return;
            }

            waitingForNextWave = false;
            SpawnNextWave();
        }

        private void OnDestroy()
        {
            if (roomCombatController != null)
            {
                roomCombatController.RoomCleared -= OnRoomCleared;
            }
        }

        public void StartArena(ArenaModeRuntimeSettings settings)
        {
            ResolveReferences();
            if (roomRuntimeRoot == null || roomCombatController == null || playerController == null)
            {
                Debug.LogError("Arena Mode cannot start: room, combat, or player references are missing.");
                return;
            }

            currentSettings = (settings ?? CreateFallbackSettings()).Clone();
            currentSettings.EnsurePlayableDefaults();
            arenaRunning = true;
            arenaComplete = false;
            waitingForNextWave = false;
            currentWaveIndex = -1;
            roomCombatController.RoomCleared -= OnRoomCleared;
            roomCombatController.RoomCleared += OnRoomCleared;

            currentRoomAsset = ResolveRoomAsset(currentSettings);
            roomRuntimeRoot.BuildFrom(currentRoomAsset);
            playerController.transform.localPosition = roomRuntimeRoot.SafeStartLocalPosition;
            playerController.ConfigureDefault();
            roomCombatController.BeginRoom(roomRuntimeRoot, playerController, alreadyCleared: true, RoomCombatEncounterKind.Standard, RoomCombatEncounterContext.Empty);
            ApplyPlayerOverrides();
            ConfigureGameplayCameraFollow();
            scoreTracker.Reset(playerController);
            screen?.ShowCombatOverlay();
            SpawnNextWave();
        }

        public void StopArenaToSetup()
        {
            arenaRunning = false;
            arenaComplete = false;
            waitingForNextWave = false;
            currentWaveIndex = -1;
            screen?.ShowSetup(currentSettings?.Clone() ?? CreateFallbackSettings());
        }

        public void QuitArena()
        {
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(returnRoute);
            }

            SceneLoaderService.LoadRouteAsync(returnRoute);
        }

        public void SpawnManualGroup(string spawnKind, int count)
        {
            if (!arenaRunning || roomCombatController == null || currentSettings == null || currentSettings.CuratedLocked)
            {
                return;
            }

            var group = ArenaModeDefaults.CreateGroup(
                spawnKind,
                Mathf.Clamp(count, 1, 32),
                ArenaSpawnPattern.OuterRing,
                ArenaGroupingMode.LoosePack);
            var spawns = currentSettings.HasCuratedRoom && currentRoomAsset != null
                ? ArenaModeRuntimeRoomBuilder.BuildCuratedSpawnPoints(currentRoomAsset, new[] { group }, currentWaveIndex + 1)
                : ArenaModeRuntimeRoomBuilder.BuildSpawnPoints(currentSettings, new[] { group }, currentWaveIndex + 1);
            SpawnArenaEnemies(spawns, $"arena_manual_{Time.frameCount}");
        }

        public IReadOnlyList<string> AvailableSpawnKinds()
        {
            var catalog = roomCombatController != null ? roomCombatController.EnemyCatalog : null;
            var definitions = catalog != null && catalog.Definitions.Count > 0
                ? catalog.Definitions
                : EnemyCatalog.CreateRuntimeDefault().Definitions;
            return definitions
                .Where(definition => definition != null && definition.SpawnKind != "spawnEnemyBoss")
                .Select(definition => definition.SpawnKind)
                .Distinct()
                .OrderBy(kind => kind)
                .ToArray();
        }

        public string DisplayNameForSpawnKind(string spawnKind)
        {
            var definition = roomCombatController != null
                ? roomCombatController.EnemyCatalog?.Resolve(spawnKind)
                : EnemyCatalog.CreateRuntimeDefault().Resolve(spawnKind);
            return definition != null ? definition.DisplayName : spawnKind;
        }

        private void SpawnNextWave()
        {
            if (currentSettings == null)
            {
                return;
            }

            currentWaveIndex++;
            var groups = GroupsForWave(currentWaveIndex);
            if (groups.Count == 0)
            {
                CompleteArena();
                return;
            }

            var spawns = currentSettings.HasCuratedRoom && currentRoomAsset != null
                ? ArenaModeRuntimeRoomBuilder.BuildCuratedSpawnPoints(currentRoomAsset, groups, currentWaveIndex)
                : ArenaModeRuntimeRoomBuilder.BuildSpawnPoints(currentSettings, groups, currentWaveIndex);
            SpawnArenaEnemies(spawns, $"arena_wave_{currentWaveIndex + 1:00}");
        }

        private IReadOnlyList<ArenaModeEnemyGroupDefinition> GroupsForWave(int waveIndex)
        {
            if (currentSettings.Waves.Count == 0)
            {
                return System.Array.Empty<ArenaModeEnemyGroupDefinition>();
            }

            if (!currentSettings.SurvivalMode && waveIndex >= currentSettings.Waves.Count)
            {
                return System.Array.Empty<ArenaModeEnemyGroupDefinition>();
            }

            var wave = currentSettings.Waves[waveIndex % currentSettings.Waves.Count];
            var groups = wave.Groups.Select(group => group.Clone()).ToList();
            if (!currentSettings.SurvivalMode)
            {
                return groups;
            }

            var bonus = waveIndex / 2;
            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                group.Configure(group.SpawnKind, group.Count + bonus, group.SpawnPattern, group.GroupingMode, group.PatrolIntent, group.SpawnDelaySeconds);
            }

            if (waveIndex > 0 && waveIndex % 3 == 0 && groups.Count > 0)
            {
                groups.Add(groups[0].Clone());
            }

            return groups;
        }

        private void SpawnArenaEnemies(IReadOnlyList<ImportedSpawnPoint> spawns, string encounterId)
        {
            if (spawns == null || spawns.Count == 0)
            {
                return;
            }

            var context = new RoomCombatEncounterContext(encounterId, ArenaModeRuntimeRoomBuilder.SpawnKindsFor(spawns));
            var enemies = roomCombatController.SpawnAdditionalEnemies(spawns, context);
            scoreTracker.BindEnemies(enemies);
            screen?.RefreshOverlay();
        }

        private void OnRoomCleared(RoomCombatController controller)
        {
            if (!arenaRunning || waitingForNextWave)
            {
                return;
            }

            scoreTracker.RecordWaveClear();
            if (!currentSettings.SurvivalMode && currentWaveIndex + 1 >= currentSettings.Waves.Count)
            {
                CompleteArena();
                return;
            }

            waitingForNextWave = true;
            nextWaveStartTime = Time.time + NextWaveDelaySeconds;
        }

        private void CompleteArena()
        {
            arenaRunning = false;
            arenaComplete = true;
            waitingForNextWave = false;
            screen?.ShowArenaComplete();
        }

        private void ApplyPlayerOverrides()
        {
            var health = playerController.GetComponent<CombatantHealth>() ?? playerController.gameObject.AddComponent<CombatantHealth>();
            health.Configure(currentSettings.PlayerHp);

            var movement = playerController.GetComponent<PlayerMovementController>() ?? playerController.gameObject.AddComponent<PlayerMovementController>();
            movement.ConfigureDerivedStats(currentSettings.PlayerSpeedMetersPerSecond);

            var weapon = playerController.GetComponent<PlayerWeaponController>() ?? playerController.gameObject.AddComponent<PlayerWeaponController>();
            weapon.ConfigureBuildStats(
                nextCooldownMultiplier: 1f,
                nextRangedDamageBonus: currentSettings.PlayerDamageBonus,
                nextMeleeDamageBonus: 1 + currentSettings.PlayerDamageBonus,
                nextMaxStamina: 100f,
                nextStaminaRegenPerSecond: 18f,
                nextMeleeWeaponId: weapon.MeleeWeaponId,
                nextRangedWeaponId: weapon.RangedWeaponId,
                nextActiveWeaponSlot: weapon.ActiveWeaponSlot,
                nextCurrentStamina: weapon.MaxStamina,
                nextWeaponCatalog: weapon.WeaponCatalog);
        }

        private void ApplyPlatformKind(HollowPlatformKind nextPlatformKind)
        {
            var presentation = FindAnyObjectByType<PlatformPresentationRoot>();
            presentation?.Configure(nextPlatformKind);
        }

        private void ConfigureGameplayCameraFollow()
        {
            if (playerController == null)
            {
                return;
            }

            var targetCamera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (targetCamera == null)
            {
                return;
            }

            var rigMetadata = targetCamera.GetComponentInParent<CameraRigMetadata>();
            var host = rigMetadata != null ? rigMetadata.gameObject : targetCamera.gameObject;
            var follow = host.GetComponent<GameplayCameraFollowController>() ?? host.AddComponent<GameplayCameraFollowController>();
            follow.Configure(playerController.transform, platformKind);
        }

        private void EnsureScreen()
        {
            if (screen != null)
            {
                return;
            }

            var canvas = targetCanvas != null ? targetCanvas : FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("ArenaModeCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            targetCanvas = canvas;
            screen = canvas.GetComponent<ArenaModeScreen>() ?? canvas.gameObject.AddComponent<ArenaModeScreen>();
            screen.Bind(this);
        }

        private void ResolveReferences()
        {
            if (roomRuntimeRoot == null)
            {
                roomRuntimeRoot = GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindAnyObjectByType<RoomRuntimeRoot>();
            }

            if (roomCombatController == null)
            {
                roomCombatController = GetComponent<RoomCombatController>() ?? FindAnyObjectByType<RoomCombatController>();
            }

            if (playerController == null)
            {
                playerController = GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindAnyObjectByType<PlaceholderPlayerController>();
            }
        }

        private static ImportedRoomRuntimeAsset ResolveRoomAsset(ArenaModeRuntimeSettings settings)
        {
            if (settings?.CuratedRoomRuntimeJson != null)
            {
                if (HollowRuntimeV2Importer.TryImport(settings.CuratedRoomRuntimeJson.text, out var curatedRoom, out var error))
                {
                    return curatedRoom;
                }

                Debug.LogError($"Arena curated room '{settings.CuratedRoomRuntimeJson.name}' failed to import. Falling back to generated arena. {error}");
            }

            return ArenaModeRuntimeRoomBuilder.BuildRoom(settings);
        }

        private IReadOnlyList<ArenaModePresetDefinition> ResolvePresets()
        {
            if (presets != null && presets.Any(preset => preset != null))
            {
                return presets.Where(preset => preset != null).ToArray();
            }

            return System.Array.Empty<ArenaModePresetDefinition>();
        }

        private ArenaModePresetDefinition ResolvePreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return null;
            }

            return ResolvePresets().FirstOrDefault(preset => preset != null && preset.PresetId == presetId);
        }

        private static ArenaModeRuntimeSettings CreateFallbackSettings()
        {
            var settings = new ArenaModeRuntimeSettings();
            settings.EnsurePlayableDefaults();
            return settings;
        }
    }
}
