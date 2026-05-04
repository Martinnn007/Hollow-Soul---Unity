using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Entities;
using Hollow.Presentation;
using Hollow.RoomDesigner;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public enum EnemyPreviewLabPlayerPattern
    {
        HiddenNoStimulus = 0,
        Stationary = 1,
        Circle = 2,
        FigureEight = 3,
        ApproachRetreat = 4,
        SweepLane = 5,
        DeterministicWander = 6
    }

    [ExecuteAlways]
    public sealed class EnemyPreviewLabController : MonoBehaviour
    {
        public const string DefaultScenePath = "Assets/_Hollow/Scenes/EnemyPreviewLab/EnemyPreviewLab.unity";
        public const string DefaultSelectedSpawnKind = "spawnEnemyNormal";
        public const string DefaultEnemyCatalogPath = "Assets/_Hollow/Data/Enemies/EnemyCatalog.asset";

        [Header("Preview Target")]
        [SerializeField] private string selectedSpawnKind = DefaultSelectedSpawnKind;
        [SerializeField] private EnemyCatalog enemyCatalog;
        [SerializeField] private DifficultyTierDefinition difficultyTier;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject enemyProjectilePrefab;

        [Header("Simulation")]
        [SerializeField] private EnemyPreviewLabPlayerPattern playerPattern = EnemyPreviewLabPlayerPattern.Circle;
        [SerializeField] private float playerPatternRadiusMeters = 3.25f;
        [SerializeField] private float playerPatternSpeed = 0.65f;
        [SerializeField] private bool respawnOnPlay = true;
        [SerializeField] private bool rebuildRoomOnPlay = true;
        [SerializeField] private bool freezeEnemyInspectionMode;

        [Header("Overlays")]
        [SerializeField] private bool showRangeOverlays = true;
        [SerializeField] private bool showGridOverlay = true;
        [SerializeField] private bool showPathTracing = true;
        [SerializeField] private bool showAiBlackboard = true;
        [SerializeField] private bool showRuntimeStats = true;

        private readonly Dictionary<string, LineRenderer> overlayLines = new();
        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private EnemyRuntimeController activeEnemy;
        private Transform runtimeRoot;
        private Transform overlayRoot;
        private GameObject fallbackEnemyPrefab;
        private GameObject fallbackProjectilePrefab;
        private Material overlayMaterial;
        private float simulationTime;

        public string SelectedSpawnKind => string.IsNullOrWhiteSpace(selectedSpawnKind) ? DefaultSelectedSpawnKind : selectedSpawnKind;

        public EnemyDefinition SelectedDefinition => ResolveCatalog().Resolve(SelectedSpawnKind);

        public EnemyRuntimeController ActiveEnemy => activeEnemy;

        public EnemyPreviewLabPlayerPattern PlayerPattern => playerPattern;

        public bool ShowRangeOverlays => showRangeOverlays;

        public bool ShowGridOverlay => showGridOverlay;

        public bool ShowPathTracing => showPathTracing;

        public bool ShowAiBlackboard => showAiBlackboard;

        public bool ShowRuntimeStats => showRuntimeStats;

        public void SetSelectedSpawnKind(string spawnKind, bool respawnIfPlaying)
        {
            selectedSpawnKind = string.IsNullOrWhiteSpace(spawnKind) ? DefaultSelectedSpawnKind : spawnKind;
            if (Application.isPlaying && respawnIfPlaying)
            {
                RespawnPreviewEnemy();
            }
        }

        public void SetPlayerPattern(EnemyPreviewLabPlayerPattern pattern)
        {
            playerPattern = pattern;
        }

        public void SetOverlayToggles(bool ranges, bool grid, bool paths, bool aiBlackboard, bool stats)
        {
            showRangeOverlays = ranges;
            showGridOverlay = grid;
            showPathTracing = paths;
            showAiBlackboard = aiBlackboard;
            showRuntimeStats = stats;
            EnemyNavigationDebugOverlay.SetPathTracingEnabled(showPathTracing);
            EnemyAiDebugOverlay.SetBlackboardEnabled(showAiBlackboard);
        }

        public void RebuildPreviewRoom()
        {
            EnsureSceneObjects();
            roomRuntimeRoot.BuildFrom(CreatePreviewRoomAsset(SelectedSpawnKind));
        }

        public void RespawnPreviewEnemy()
        {
            EnsureSceneObjects();
            if (roomRuntimeRoot.LastBuiltAsset == null)
            {
                RebuildPreviewRoom();
            }

            ClearRuntimeChildren();
            simulationTime = 0f;
            playerController = CreatePlayer();
            var result = EnemySpawnService.SpawnEnemies(new EnemySpawnRequest(
                roomRuntimeRoot,
                runtimeRoot,
                ResolveEnemyPrefab(),
                ResolveProjectilePrefab(),
                playerController,
                ResolveCatalog(),
                difficultyTier != null ? difficultyTier : DifficultyTierDefinition.CreateRuntimeDeveloperSample(),
                new CombatDiagnosticsModel(),
                new RoomCombatEncounterContext("enemy_preview_lab", new[] { SelectedSpawnKind })));

            activeEnemy = result.Enemies.FirstOrDefault();
            if (activeEnemy != null)
            {
                activeEnemy.SetInspectionMode(freezeEnemyInspectionMode ? InspectionEntityMode.FrozenRuntime : InspectionEntityMode.LiveRuntime);
                activeEnemy.BeginEntryGrace(0.15f, Application.isPlaying ? Time.time : 0f);
            }

            UpdateDebugToggles();
            UpdateRuntimeOverlays();
        }

        public static ImportedRoomRuntimeAsset CreatePreviewRoomAsset(string spawnKind)
        {
            var width = 18;
            var height = 12;
            var bounds = Rect.MinMaxRect(-9f, -6f, 9f, 6f);
            var walkable = new List<Vector2Int>();
            for (var x = -8; x <= 8; x++)
            {
                for (var z = -5; z <= 5; z++)
                {
                    if ((x == -5 && z is 2 or 3) || (x == 5 && z is -3 or -2))
                    {
                        continue;
                    }

                    walkable.Add(new Vector2Int(x, z));
                }
            }

            var holes = new[]
            {
                new Vector2Int(-5, 2),
                new Vector2Int(-5, 3),
                new Vector2Int(5, -3),
                new Vector2Int(5, -2)
            };
            var obstacles = new[]
            {
                new RoomLayoutObstacle("rock_center_north", RoomDesignerCellKinds.Rock, new Vector3(-1.5f, 0.45f, 1.75f), new Vector3(1.45f, 0.9f, 1.45f), true),
                new RoomLayoutObstacle("rock_center_south", RoomDesignerCellKinds.Rock, new Vector3(1.65f, 0.45f, -1.65f), new Vector3(1.35f, 0.9f, 1.35f), true),
                new RoomLayoutObstacle("rock_west_lane", RoomDesignerCellKinds.Rock, new Vector3(-4.6f, 0.45f, -1.25f), new Vector3(1.15f, 0.9f, 1.15f), true),
                new RoomLayoutObstacle("rock_east_lane", RoomDesignerCellKinds.Rock, new Vector3(4.5f, 0.45f, 1.25f), new Vector3(1.2f, 0.9f, 1.2f), true)
            };
            var layout = new RoomLayout(
                width,
                height,
                bounds,
                walkable,
                holes,
                new[] { new RoomLayoutFloorRegion("floor_main", Vector3.zero, new Vector2(9f, 6f)) },
                obstacles);
            var footprint = new RoomInstanceFootprint(Vector2Int.zero, new[] { Vector2Int.zero }, new Vector2Int(width, height));
            var ports = new[]
            {
                new RoomDoorPort("north_0", "north", 0, Vector2Int.zero, new Vector2(0f, 6f), new Vector3(0f, 0f, 6f), "active"),
                new RoomDoorPort("south_0", "south", 0, Vector2Int.zero, new Vector2(0f, -6f), new Vector3(0f, 0f, -6f), "active"),
                new RoomDoorPort("east_0", "east", 0, Vector2Int.zero, new Vector2(9f, 0f), new Vector3(9f, 0f, 0f), "active"),
                new RoomDoorPort("west_0", "west", 0, Vector2Int.zero, new Vector2(-9f, 0f), new Vector3(-9f, 0f, 0f), "active")
            };
            var safeStart = Spawn("safe_start", RoomDesignerMarkerKinds.SafeStart, new Vector3(-5.8f, 0f, -2.25f));
            var enemySpawn = Spawn("preview_enemy", string.IsNullOrWhiteSpace(spawnKind) ? DefaultSelectedSpawnKind : spawnKind, new Vector3(2.6f, 0f, 0.8f));
            var rewardSpawn = Spawn("reward_anchor", "roomReward", new Vector3(6.7f, 0f, 4.2f));
            var hazards = new[]
            {
                new ImportedRoomHazard { id = "hazard_preview_spikes", kind = RoomDesignerCellKinds.Spike, center = ImportedVector(0f, 0f, 4.45f), radius = 0.45f }
            };

            return new ImportedRoomRuntimeAsset(
                "enemy_preview_lab",
                "Enemy Preview Lab",
                layout,
                footprint,
                ports,
                new[] { enemySpawn },
                new[] { rewardSpawn },
                safeStart,
                hazards,
                System.Array.Empty<ImportedRoomInteractiveObject>(),
                System.Array.Empty<ImportedRoomDecor>(),
                null);
        }

        private void Awake()
        {
            EnsureSceneObjects();
            EnsurePreviewRoomForEditMode();
            UpdateDebugToggles();
        }

        private void OnEnable()
        {
            EnsureSceneObjects();
            EnsurePreviewRoomForEditMode();
            UpdateDebugToggles();
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (rebuildRoomOnPlay)
            {
                RebuildPreviewRoom();
            }

            if (respawnOnPlay)
            {
                RespawnPreviewEnemy();
            }
        }

        private void Update()
        {
            UpdateDebugToggles();
            if (!Application.isPlaying)
            {
                return;
            }

            simulationTime += Time.deltaTime * Mathf.Max(0.05f, playerPatternSpeed);
            UpdatePlayerPattern();
            UpdateRuntimeOverlays();
        }

        private void OnValidate()
        {
            selectedSpawnKind = string.IsNullOrWhiteSpace(selectedSpawnKind) ? DefaultSelectedSpawnKind : selectedSpawnKind;
            playerPatternRadiusMeters = Mathf.Clamp(playerPatternRadiusMeters, 0.25f, 7.5f);
            playerPatternSpeed = Mathf.Clamp(playerPatternSpeed, 0.05f, 4f);
            UpdateDebugToggles();
        }

        private void OnDrawGizmos()
        {
            if (!showGridOverlay)
            {
                return;
            }

            DrawGridGizmos();
            DrawDefinitionGizmos();
        }

        private void EnsureSceneObjects()
        {
            roomRuntimeRoot = roomRuntimeRoot != null ? roomRuntimeRoot : GetComponentInChildren<RoomRuntimeRoot>(true);
            if (roomRuntimeRoot == null)
            {
                var roomObject = new GameObject("PreviewRoomRuntime");
                roomObject.transform.SetParent(transform, false);
                roomRuntimeRoot = roomObject.AddComponent<RoomRuntimeRoot>();
                roomRuntimeRoot.ConfigureDefault();
            }

            runtimeRoot = runtimeRoot != null ? runtimeRoot : transform.Find("RuntimeActors");
            if (runtimeRoot == null)
            {
                var runtimeObject = new GameObject("RuntimeActors");
                runtimeObject.transform.SetParent(transform, false);
                runtimeRoot = runtimeObject.transform;
            }

            overlayRoot = overlayRoot != null ? overlayRoot : transform.Find("RuntimeOverlays");
            if (overlayRoot == null)
            {
                var overlayObject = new GameObject("RuntimeOverlays");
                overlayObject.transform.SetParent(transform, false);
                overlayRoot = overlayObject.transform;
            }

            EnsureRenderRig();
        }

        private void EnsureRenderRig()
        {
            if (transform.Find("PreviewCamera") == null)
            {
                var cameraObject = new GameObject("PreviewCamera");
                cameraObject.transform.SetParent(transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 11.5f, -8.5f);
                cameraObject.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 7.4f;
                camera.clearFlags = CameraClearFlags.Skybox;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 80f;
                cameraObject.AddComponent<AudioListener>();
            }

            EnsureLight("KeyLight_Directional", LightType.Directional, Vector3.zero, Quaternion.Euler(45f, -35f, 0f), 1.2f, 0f, new Color(1f, 0.94f, 0.84f, 1f));
            EnsureLight("FillLight_Point", LightType.Point, new Vector3(0f, 5f, -3f), Quaternion.identity, 0.65f, 12f, new Color(0.45f, 0.62f, 1f, 1f));
            EnsureLight("RimLight_Point", LightType.Point, new Vector3(4.5f, 4f, 4.5f), Quaternion.identity, 0.85f, 9f, new Color(0.9f, 0.58f, 0.36f, 1f));
        }

        private void EnsureLight(string objectName, LightType type, Vector3 localPosition, Quaternion localRotation, float intensity, float range, Color color)
        {
            if (transform.Find(objectName) != null)
            {
                return;
            }

            var lightObject = new GameObject(objectName);
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = localPosition;
            lightObject.transform.localRotation = localRotation;
            var light = lightObject.AddComponent<Light>();
            light.type = type;
            light.intensity = intensity;
            light.range = range;
            light.color = color;
        }

        private void EnsurePreviewRoomForEditMode()
        {
            if (Application.isPlaying || roomRuntimeRoot == null || roomRuntimeRoot.LastBuiltAsset != null)
            {
                return;
            }

            roomRuntimeRoot.BuildFrom(CreatePreviewRoomAsset(SelectedSpawnKind));
        }

        private PlaceholderPlayerController CreatePlayer()
        {
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Preview.DummyPlayer";
            player.transform.SetParent(runtimeRoot, false);
            player.transform.localPosition = new Vector3(-5.8f, 0.9f, -2.25f);
            player.transform.localScale = new Vector3(0.56f, 0.9f, 0.56f);
            MaterialResolver.ApplyTo(player, MaterialRole.PlayerBody);
            PresentationPrefabResolver.InstantiateVisual(PresentationPrefabRole.Player, player.transform, Vector3.zero, Vector3.one);
            var collider = player.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            var controller = player.AddComponent<PlaceholderPlayerController>();
            controller.ConfigureDefault();
            var health = player.AddComponent<CombatantHealth>();
            health.Configure(12);
            return controller;
        }

        private void UpdatePlayerPattern()
        {
            if (playerController == null)
            {
                return;
            }

            var position = playerPattern switch
            {
                EnemyPreviewLabPlayerPattern.HiddenNoStimulus => new Vector3(0f, 0.9f, 80f),
                EnemyPreviewLabPlayerPattern.Stationary => new Vector3(-5.8f, 0.9f, -2.25f),
                EnemyPreviewLabPlayerPattern.Circle => new Vector3(Mathf.Cos(simulationTime) * playerPatternRadiusMeters, 0.9f, Mathf.Sin(simulationTime) * playerPatternRadiusMeters),
                EnemyPreviewLabPlayerPattern.FigureEight => new Vector3(Mathf.Sin(simulationTime) * playerPatternRadiusMeters, 0.9f, Mathf.Sin(simulationTime * 2f) * playerPatternRadiusMeters * 0.55f),
                EnemyPreviewLabPlayerPattern.ApproachRetreat => new Vector3(Mathf.Lerp(-5.8f, 3.2f, Mathf.PingPong(simulationTime * 0.35f, 1f)), 0.9f, -1.5f),
                EnemyPreviewLabPlayerPattern.SweepLane => new Vector3(Mathf.Sin(simulationTime) * 7.1f, 0.9f, Mathf.Sin(simulationTime * 0.37f) * 1.4f),
                EnemyPreviewLabPlayerPattern.DeterministicWander => new Vector3(Mathf.Sin(simulationTime * 0.83f) * 5.8f + Mathf.Sin(simulationTime * 1.71f) * 0.75f, 0.9f, Mathf.Cos(simulationTime * 0.61f) * 3.8f),
                _ => Vector3.zero
            };
            playerController.transform.localPosition = position;
            var visible = playerPattern != EnemyPreviewLabPlayerPattern.HiddenNoStimulus;
            foreach (var renderer in playerController.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible;
            }
        }

        private void UpdateRuntimeOverlays()
        {
            if (!showRangeOverlays || activeEnemy == null || overlayRoot == null)
            {
                SetAllOverlayLinesVisible(false);
                return;
            }

            SetAllOverlayLinesVisible(true);
            var enemy = activeEnemy;
            var origin = enemy.transform.position + Vector3.up * 0.04f;
            DrawRing("hearing", origin, enemy.HearingRadiusMeters, new Color(0.2f, 0.9f, 1f, 0.72f));
            DrawRing("sight", origin, enemy.SightRadiusMeters, new Color(1f, 0.86f, 0.2f, 0.72f));
            DrawRing("preferred_min", origin, enemy.PreferredRangeMinMeters, new Color(0.35f, 1f, 0.35f, 0.9f));
            DrawRing("preferred_max", origin, enemy.PreferredRangeMaxMeters, new Color(0.35f, 1f, 0.35f, 0.45f));
            DrawRing("attack", origin, enemy.Definition != null ? enemy.Definition.AttackRangeMeters : 0f, new Color(1f, 0.22f, 0.22f, 0.5f));
            DrawSightCone("sight_cone", origin, enemy.FacingDirection, enemy.SightRadiusMeters, enemy.SightAngleDegrees, new Color(1f, 0.86f, 0.2f, 0.82f));
            DrawPathLine("path_goal", enemy);
        }

        private void DrawPathLine(string id, EnemyRuntimeController enemy)
        {
            var line = ResolveLine(id, new Color(0.45f, 0.65f, 1f, 0.95f), 0.06f);
            if (!showPathTracing || enemy.LastNavigationPathStatus == EnemyPathStatus.NotRequested)
            {
                line.positionCount = 0;
                return;
            }

            line.positionCount = 3;
            line.SetPosition(0, enemy.transform.position + Vector3.up * 0.11f);
            line.SetPosition(1, enemy.transform.parent != null ? enemy.transform.parent.TransformPoint(enemy.LastNavigationNextWaypoint) + Vector3.up * 0.11f : enemy.LastNavigationNextWaypoint + Vector3.up * 0.11f);
            line.SetPosition(2, enemy.transform.parent != null ? enemy.transform.parent.TransformPoint(enemy.LastNavigationFinalGoal) + Vector3.up * 0.11f : enemy.LastNavigationFinalGoal + Vector3.up * 0.11f);
        }

        private void DrawRing(string id, Vector3 center, float radius, Color color)
        {
            var line = ResolveLine(id, color, 0.035f);
            if (radius <= 0f)
            {
                line.positionCount = 0;
                return;
            }

            const int segments = 96;
            line.loop = true;
            line.positionCount = segments;
            for (var index = 0; index < segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                line.SetPosition(index, center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private void DrawSightCone(string id, Vector3 center, Vector3 facing, float radius, float angleDegrees, Color color)
        {
            var line = ResolveLine(id, color, 0.04f);
            if (radius <= 0f || angleDegrees <= 0f)
            {
                line.positionCount = 0;
                return;
            }

            facing = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector3.forward;
            var left = Quaternion.Euler(0f, -angleDegrees * 0.5f, 0f) * facing;
            var right = Quaternion.Euler(0f, angleDegrees * 0.5f, 0f) * facing;
            line.loop = false;
            line.positionCount = 3;
            line.SetPosition(0, center);
            line.SetPosition(1, center + left * radius);
            line.SetPosition(2, center + right * radius);
        }

        private LineRenderer ResolveLine(string id, Color color, float width)
        {
            if (overlayLines.TryGetValue(id, out var existing) && existing != null)
            {
                existing.startColor = color;
                existing.endColor = color;
                existing.startWidth = width;
                existing.endWidth = width;
                return existing;
            }

            var lineObject = new GameObject($"Overlay.{id}");
            lineObject.transform.SetParent(overlayRoot, false);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = ResolveOverlayMaterial();
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            overlayLines[id] = line;
            return line;
        }

        private Material ResolveOverlayMaterial()
        {
            if (overlayMaterial != null)
            {
                return overlayMaterial;
            }

            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            overlayMaterial = shader != null ? new Material(shader) : null;
            if (overlayMaterial != null)
            {
                overlayMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return overlayMaterial;
        }

        private void SetAllOverlayLinesVisible(bool visible)
        {
            foreach (var line in overlayLines.Values)
            {
                if (line != null)
                {
                    line.enabled = visible;
                }
            }
        }

        private EnemyCatalog ResolveCatalog()
        {
            if (enemyCatalog != null)
            {
                return enemyCatalog;
            }

#if UNITY_EDITOR
            var assetCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyCatalog>(DefaultEnemyCatalogPath);
            if (assetCatalog != null)
            {
                return assetCatalog;
            }
#endif

            return EnemyCatalog.CreateRuntimeDefault();
        }

        private GameObject ResolveEnemyPrefab()
        {
            if (enemyPrefab != null)
            {
                return enemyPrefab;
            }

            if (fallbackEnemyPrefab == null)
            {
                fallbackEnemyPrefab = new GameObject("Preview.EnemyPrefabFallback");
                fallbackEnemyPrefab.transform.SetParent(transform, false);
                fallbackEnemyPrefab.SetActive(false);
                fallbackEnemyPrefab.AddComponent<EnemyRuntimeController>();
            }

            return fallbackEnemyPrefab;
        }

        private GameObject ResolveProjectilePrefab()
        {
            if (enemyProjectilePrefab != null)
            {
                return enemyProjectilePrefab;
            }

            if (fallbackProjectilePrefab == null)
            {
                fallbackProjectilePrefab = new GameObject("Preview.EnemyProjectilePrefabFallback");
                fallbackProjectilePrefab.transform.SetParent(transform, false);
                fallbackProjectilePrefab.SetActive(false);
                fallbackProjectilePrefab.AddComponent<EnemyProjectileController>();
            }

            return fallbackProjectilePrefab;
        }

        private void ClearRuntimeChildren()
        {
            if (runtimeRoot == null)
            {
                return;
            }

            for (var index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                var child = runtimeRoot.GetChild(index).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            activeEnemy = null;
            playerController = null;
        }

        private void UpdateDebugToggles()
        {
            EnemyNavigationDebugOverlay.SetPathTracingEnabled(showPathTracing);
            EnemyAiDebugOverlay.SetBlackboardEnabled(showAiBlackboard);
        }

        private void DrawGridGizmos()
        {
            Gizmos.color = new Color(0.35f, 0.45f, 0.55f, 0.22f);
            for (var x = -9; x <= 9; x++)
            {
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(x, 0.035f, -6f)), transform.TransformPoint(new Vector3(x, 0.035f, 6f)));
            }

            for (var z = -6; z <= 6; z++)
            {
                Gizmos.DrawLine(transform.TransformPoint(new Vector3(-9f, 0.035f, z)), transform.TransformPoint(new Vector3(9f, 0.035f, z)));
            }
        }

        private void DrawDefinitionGizmos()
        {
            var definition = SelectedDefinition;
            if (definition == null)
            {
                return;
            }

            var spawn = transform.TransformPoint(new Vector3(2.6f, 0.08f, 0.8f));
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(spawn, Mathf.Max(0.1f, definition.RadiusMeters));
            DrawGizmoCircle(spawn, definition.SightRadiusMeters, new Color(1f, 0.86f, 0.2f, 0.42f));
            DrawGizmoCircle(spawn, definition.HearingRadiusMeters, new Color(0.2f, 0.9f, 1f, 0.36f));
            DrawGizmoCircle(spawn, definition.PreferredRangeMinMeters, new Color(0.35f, 1f, 0.35f, 0.55f));
            DrawGizmoCircle(spawn, definition.PreferredRangeMaxMeters, new Color(0.35f, 1f, 0.35f, 0.35f));
        }

        private static void DrawGizmoCircle(Vector3 center, float radius, Color color)
        {
            if (radius <= 0f)
            {
                return;
            }

            Gizmos.color = color;
            var previous = center + new Vector3(radius, 0f, 0f);
            const int segments = 64;
            for (var index = 1; index <= segments; index++)
            {
                var angle = index / (float)segments * Mathf.PI * 2f;
                var next = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private static ImportedSpawnPoint Spawn(string id, string kind, Vector3 position)
        {
            return new ImportedSpawnPoint
            {
                id = id,
                kind = kind,
                position = ImportedVector(position.x, position.y, position.z)
            };
        }

        private static ImportedVector3 ImportedVector(float x, float y, float z)
        {
            return new ImportedVector3 { x = x, y = y, z = z };
        }
    }
}
