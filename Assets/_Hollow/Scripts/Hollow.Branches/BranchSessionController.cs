using System.Linq;
using Hollow.Combat;
using Hollow.Core.App;
using Hollow.Entities;
using Hollow.Input;
using Hollow.Rewards;
using Hollow.Rooms;
using Hollow.World;
using UnityEngine;

namespace Hollow.Branches
{
    public sealed class BranchSessionController : MonoBehaviour, IBranchSessionController
    {
        private const float RewardInteractionRadiusMeters = 1.25f;
        private const float PortalInteractionRadiusMeters = 1.5f;

        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private PlaceholderPlayerController playerController;
        [SerializeField] private RoomCombatController roomCombatController;
        [SerializeField] private GameObject rewardPickupPrefab;
        [SerializeField] private GameObject hubReturnPortalPrefab;

        private ImportedRoomRuntimeAsset roomAsset;
        private GameSessionState gameSessionState;
        private RoomRewardPickup currentRewardPickup;
        private HubReturnPortal currentHubPortal;
        private readonly RuntimeRewardCounter rewardCounter = new();

        public BranchSessionState State { get; private set; }

        public RuntimeRewardCounter RewardCounter => rewardCounter;

        public GameObject RewardPickupPrefab => rewardPickupPrefab;

        public GameObject HubReturnPortalPrefab => hubReturnPortalPrefab;

        public RoomRewardPickup CurrentRewardPickup => currentRewardPickup;

        public HubReturnPortal CurrentHubPortal => currentHubPortal;

        public bool HubReturnRequested { get; private set; }

        public ImportedRoomRuntimeAsset RoomAsset => roomAsset;

        public void Configure(GameObject nextRewardPickupPrefab, GameObject nextHubReturnPortalPrefab)
        {
            rewardPickupPrefab = nextRewardPickupPrefab;
            hubReturnPortalPrefab = nextHubReturnPortalPrefab;
        }

        public void Initialize(ImportedRoomRuntimeAsset nextRoomAsset, GameSessionState nextSessionState)
        {
            roomAsset = nextRoomAsset;
            gameSessionState = nextSessionState;
            ResolveReferences();

            if (roomAsset == null || roomRuntimeRoot == null || playerController == null || roomCombatController == null)
            {
                Debug.LogError("Branch session could not initialize because a required runtime reference is missing.");
                return;
            }

            roomCombatController.RoomCleared -= OnRoomCleared;
            roomCombatController.RoomCleared += OnRoomCleared;

            State = BranchSessionState.Create(BranchGenerator.CreateFiveRoomCross(roomAsset));
            LoadCurrentRoom(roomAsset.SafeStart?.position?.ToUnityVector3() ?? Vector3.zero);
        }

        private void OnDestroy()
        {
            if (roomCombatController != null)
            {
                roomCombatController.RoomCleared -= OnRoomCleared;
            }
        }

        private void Update()
        {
            if (State == null || !GameplayInputReader.ReadCurrent().InteractPressed)
            {
                return;
            }

            TryInteract();
        }

        public bool TryInteract()
        {
            return TryClaimReward() || TryUseHubReturnPortal() || TryTraverseNearestDoor();
        }

        public bool TryTraverse(string direction)
        {
            if (!BranchTraversalService.CanTraverse(State, direction, out var connection))
            {
                return false;
            }

            State.EnterRoom(connection.ToRoomId);
            LoadCurrentRoom(BranchTraversalService.EntryPositionFor(roomRuntimeRoot, connection.ToDirection));
            return true;
        }

        public BranchMiniMapModel CreateMiniMapModel()
        {
            return new BranchMiniMapModel(State);
        }

        private bool TryTraverseNearestDoor()
        {
            if (State == null || playerController == null || roomRuntimeRoot == null)
            {
                return false;
            }

            var playerPosition = playerController.transform.localPosition;
            var nearest = State.Graph.ConnectionsFrom(State.CurrentRoomId)
                .Where(connection => roomRuntimeRoot.TryGetDoorPort(connection.FromDirection, out _))
                .Select(connection =>
                {
                    roomRuntimeRoot.TryGetDoorPort(connection.FromDirection, out var port);
                    return new
                    {
                        Connection = connection,
                        Distance = Vector3.Distance(Flat(playerPosition), Flat(port.Position))
                    };
                })
                .OrderBy(candidate => candidate.Distance)
                .FirstOrDefault();

            if (nearest == null || nearest.Distance > BranchTraversalService.DoorInteractionRadiusMeters)
            {
                return false;
            }

            return TryTraverse(nearest.Connection.FromDirection);
        }

        private void LoadCurrentRoom(Vector3 playerLocalPosition)
        {
            DestroyTransientInteractables();
            roomRuntimeRoot.BuildFrom(roomAsset);
            playerController.transform.localPosition = playerLocalPosition;
            State.CurrentRoom.MarkVisited();
            roomCombatController.BeginRoom(roomRuntimeRoot, playerController, State.CurrentRoom.IsCleared);
            UpdateDoorVisuals();
            SpawnRewardIfNeeded();
            SpawnHubPortalIfReady();
        }

        private void OnRoomCleared(RoomCombatController _)
        {
            if (State == null)
            {
                return;
            }

            State.CurrentRoom.MarkCleared();
            if (State.CurrentRoom.Id != BranchRoomId.Origin)
            {
                State.CurrentRoom.MarkRewardPending();
            }

            UpdateDoorVisuals();
            SpawnRewardIfNeeded();
            SpawnHubPortalIfReady();
        }

        private bool TryClaimReward()
        {
            if (currentRewardPickup == null || playerController == null || State == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentRewardPickup.transform.localPosition)) > RewardInteractionRadiusMeters)
            {
                return false;
            }

            if (!currentRewardPickup.Claim())
            {
                return false;
            }

            State.CurrentRoom.MarkRewardClaimed();
            rewardCounter.IncrementClaimedRewards();
            DestroyRuntimeObject(currentRewardPickup.gameObject);
            currentRewardPickup = null;
            SpawnHubPortalIfReady();
            return true;
        }

        private bool TryUseHubReturnPortal()
        {
            if (currentHubPortal == null || playerController == null)
            {
                return false;
            }

            if (Vector3.Distance(Flat(playerController.transform.localPosition), Flat(currentHubPortal.transform.localPosition)) > PortalInteractionRadiusMeters)
            {
                return false;
            }

            HubReturnRequested = true;
            if (HollowBootstrap.Instance != null)
            {
                HollowBootstrap.Instance.AppStateMachine.TransitionTo(AppShellRoute.MainMenu);
                SceneLoaderService.LoadRouteAsync(AppShellRoute.MainMenu);
            }

            return true;
        }

        private void SpawnRewardIfNeeded()
        {
            if (State.CurrentRoom.RewardState != RoomRewardState.Pending || currentRewardPickup != null)
            {
                return;
            }

            var rewardObject = InstantiateOrCreate(rewardPickupPrefab, "RoomRewardPickup", PrimitiveType.Sphere, new Color(1f, 0.82f, 0.18f, 1f));
            rewardObject.transform.SetParent(playerController.transform.parent, false);
            rewardObject.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            rewardObject.transform.localScale = Vector3.one * 0.35f;
            currentRewardPickup = rewardObject.GetComponent<RoomRewardPickup>() ?? rewardObject.AddComponent<RoomRewardPickup>();
            currentRewardPickup.Configure(State.CurrentRoomId.Value);
        }

        private void SpawnHubPortalIfReady()
        {
            if (currentHubPortal != null || State == null || !State.AreAllRoomsCleared() || !State.AreAllRewardsClaimed())
            {
                return;
            }

            var portalObject = InstantiateOrCreate(hubReturnPortalPrefab, "HubReturnPortal", PrimitiveType.Cylinder, new Color(0.25f, 1f, 0.92f, 1f));
            portalObject.transform.SetParent(playerController.transform.parent, false);
            portalObject.transform.localPosition = new Vector3(0f, 0.18f, 0f);
            portalObject.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            currentHubPortal = portalObject.GetComponent<HubReturnPortal>() ?? portalObject.AddComponent<HubReturnPortal>();
        }

        private void UpdateDoorVisuals()
        {
            if (State == null || roomRuntimeRoot == null)
            {
                return;
            }

            foreach (var port in roomRuntimeRoot.DoorPorts)
            {
                var hasConnection = State.Graph.TryGetConnection(State.CurrentRoomId, port.Direction, out _);
                var visualState = hasConnection
                    ? State.CurrentRoom.IsCleared ? RoomDoorVisualState.Cleared : RoomDoorVisualState.Locked
                    : RoomDoorVisualState.Unavailable;
                roomRuntimeRoot.SetDoorVisualState(port.Direction, visualState);
            }
        }

        private void DestroyTransientInteractables()
        {
            if (currentRewardPickup != null)
            {
                DestroyRuntimeObject(currentRewardPickup.gameObject);
                currentRewardPickup = null;
            }

            if (currentHubPortal != null)
            {
                DestroyRuntimeObject(currentHubPortal.gameObject);
                currentHubPortal = null;
            }
        }

        private void ResolveReferences()
        {
            roomRuntimeRoot = roomRuntimeRoot != null ? roomRuntimeRoot : GetComponentInChildren<RoomRuntimeRoot>(includeInactive: true) ?? FindFirstObjectByType<RoomRuntimeRoot>();
            playerController = playerController != null ? playerController : GetComponentInChildren<PlaceholderPlayerController>(includeInactive: true) ?? FindFirstObjectByType<PlaceholderPlayerController>();
            roomCombatController = roomCombatController != null ? roomCombatController : GetComponent<RoomCombatController>() ?? FindFirstObjectByType<RoomCombatController>();
        }

        private static GameObject InstantiateOrCreate(GameObject prefab, string objectName, PrimitiveType primitiveType, Color color)
        {
            var instance = prefab != null ? Instantiate(prefab) : GameObject.CreatePrimitive(primitiveType);
            instance.name = objectName;
            ApplyColor(instance, color);
            var collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return instance;
        }

        private static void ApplyColor(GameObject target, Color color)
        {
            var renderer = target.GetComponentInChildren<Renderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            {
                color = color
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }

        private static void DestroyRuntimeObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
