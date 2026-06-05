using System;
using System.Collections.Generic;
using System.Linq;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    [DefaultExecutionOrder(50)]
    public sealed class PlayerHeldWeaponVisualController : MonoBehaviour
    {
        public const string MeleeHandSocketName = "MainCharacter_RightHandWeaponSocket";
        public const string RangedHandSocketName = "MainCharacter_RangedHandWeaponSocket";
        public const string MeleeHolsterSocketName = "MainCharacter_BackMeleeHolsterSocket";
        public const string RangedHolsterSocketName = "MainCharacter_HipRangedHolsterSocket";
        public const string RangedMuzzleSocketName = "MainCharacter_RangedMuzzleSocket";
        public const string ShieldForearmSocketName = "MainCharacter_LeftForearmShieldSocket";
        public const string ShieldBackSocketName = "MainCharacter_BackShieldSocket";
        public const string ActiveMeleeWeaponVisualName = "ActiveMeleeWeaponVisual";
        public const string ActiveRangedWeaponVisualName = "ActiveRangedWeaponVisual";
        public const string HolsteredMeleeWeaponVisualName = "HolsteredMeleeWeaponVisual";
        public const string HolsteredRangedWeaponVisualName = "HolsteredRangedWeaponVisual";
        public const string EquippedShieldVisualName = "EquippedShieldVisual";

        public static readonly Vector3 DefaultMeleeSocketLocalPosition = new(0.03f, 0f, 0.02f);
        public static readonly Vector3 DefaultMeleeSocketLocalEuler = new(90f, 0f, 0f);
        public static readonly Vector3 DefaultMeleeSocketLocalScale = new(0.75f, 0.75f, 0.75f);
        public static readonly Vector3 DefaultRangedHandSocketLocalPosition = new(0.045f, -0.015f, 0.025f);
        public static readonly Vector3 DefaultRangedHandSocketLocalEuler = Vector3.zero;
        public static readonly Vector3 DefaultRangedHandSocketLocalScale = Vector3.one;
        public static readonly Vector3 DefaultMeleeHolsterSocketLocalPosition = new(-0.22f, 0.98f, -0.18f);
        public static readonly Vector3 DefaultMeleeHolsterSocketLocalEuler = new(35f, -35f, 145f);
        public static readonly Vector3 DefaultMeleeHolsterSocketLocalScale = new(0.72f, 0.72f, 0.72f);
        public static readonly Vector3 DefaultRangedHolsterSocketLocalPosition = new(0.32f, 1.08f, -0.18f);
        public static readonly Vector3 DefaultRangedHolsterSocketLocalEuler = new(0f, 90f, 18f);
        public static readonly Vector3 DefaultRangedHolsterSocketLocalScale = new(0.78f, 0.78f, 0.78f);
        public static readonly Vector3 DefaultShieldForearmSocketLocalPosition = new(0.015f, 0.02f, 0.02f);
        public static readonly Vector3 DefaultShieldForearmSocketLocalEuler = new(0f, 0f, 0f);
        public static readonly Vector3 DefaultShieldForearmSocketLocalScale = new(0.72f, 0.72f, 0.72f);
        public static readonly Vector3 DefaultShieldBackSocketLocalPosition = new(0f, 0.08f, -0.13f);
        public static readonly Vector3 DefaultShieldBackSocketLocalEuler = new(0f, 180f, 0f);
        public static readonly Vector3 DefaultShieldBackSocketLocalScale = new(0.82f, 0.82f, 0.82f);

        private const string MeshyVisualRootName = "MainCharacter_VisualRoot";
        private const string MeshyModelName = "MainCharacter_MeshyModel";
        private const string RightHandBoneName = "RightHand";
        private const string LeftForearmBoneName = "LeftForeArm";
        private const string BackShieldBoneName = "Spine02";
        private const float LightAttackDurationSeconds = 0.14f;
        private const float HeavyAttackDurationSeconds = 0.22f;
        private const float MuzzleFlashDurationSeconds = 0.1f;
        private const float MinimumScaleMagnitude = 0.0001f;
        private const float MinimumEquipmentMaxDimensionMeters = 0.05f;
        private const float MaximumAbsurdEquipmentMaxDimensionMeters = 3f;
        private const float MaximumShieldMaxDimensionMeters = 1.2f;
        private const float MaximumMeleeMaxDimensionMeters = 2.2f;
        private const float MaximumRangedMaxDimensionMeters = 1.5f;
        private static readonly string[] KnownEquipmentWrapperNames =
        {
            ActiveMeleeWeaponVisualName,
            ActiveRangedWeaponVisualName,
            HolsteredMeleeWeaponVisualName,
            HolsteredRangedWeaponVisualName,
            EquippedShieldVisualName
        };

        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private PlayerAnimationProfileController animationProfileController;
        [SerializeField] private Transform meleeHandSocket;
        [SerializeField] private Transform rangedHandSocket;
        [SerializeField] private Transform meleeHolsterSocket;
        [SerializeField] private Transform rangedHolsterSocket;
        [SerializeField] private Transform rangedMuzzleSocket;
        [SerializeField] private Transform shieldForearmSocket;
        [SerializeField] private Transform shieldBackSocket;
        [SerializeField] private Vector3 meleeSocketLocalPosition = DefaultMeleeSocketLocalPosition;
        [SerializeField] private Vector3 meleeSocketLocalEuler = DefaultMeleeSocketLocalEuler;
        [SerializeField] private Vector3 meleeSocketLocalScale = DefaultMeleeSocketLocalScale;

        private Transform heldRoot;
        private GameObject activeVisual;
        private GameObject holsteredMeleeVisual;
        private GameObject holsteredRangedVisual;
        private GameObject equippedShieldVisual;
        private GameObject muzzleFlash;
        private WeaponSlot visibleSlot;
        private bool hasVisibleSlot;
        private Vector2 currentFacing = Vector2.up;
        private Vector2 attackFacing = Vector2.up;
        private WeaponSlot attackSlot;
        private AttackKind attackKind;
        private float attackAgeSeconds;
        private float attackDurationSeconds;
        private float muzzleFlashAgeSeconds;
        private PlayerAnimationProfileController subscribedProfileController;
        private int equipmentNormalizationPassCount;

        public Transform MeleeHandSocket => meleeHandSocket;

        public Transform RangedHandSocket => rangedHandSocket;

        public Transform MeleeHolsterSocket => meleeHolsterSocket;

        public Transform RangedHolsterSocket => rangedHolsterSocket;

        public Transform ShieldForearmSocket => shieldForearmSocket;

        public Transform ShieldBackSocket => shieldBackSocket;

        public Transform CurrentShieldSocket => equippedShieldVisual != null ? equippedShieldVisual.transform.parent : null;

        public Transform ActiveMuzzleTransform => rangedMuzzleSocket;

        public GameObject ActiveWeaponVisual => activeVisual;

        public GameObject HolsteredMeleeVisual => holsteredMeleeVisual;

        public GameObject HolsteredRangedVisual => holsteredRangedVisual;

        public GameObject EquippedShieldVisual => equippedShieldVisual;

        public int EquipmentNormalizationPassCount => equipmentNormalizationPassCount;

        public bool CurrentProfileAllowsShieldInHand =>
            animationProfileController != null && animationProfileController.AllowsShieldInHand;

        public bool IsUsingHandAttachedMeleeVisual =>
            visibleSlot == WeaponSlot.Melee &&
            activeVisual != null &&
            meleeHandSocket != null &&
            activeVisual.transform.parent == meleeHandSocket;

        public bool IsUsingHandAttachedRangedVisual =>
            visibleSlot == WeaponSlot.Ranged &&
            activeVisual != null &&
            rangedHandSocket != null &&
            activeVisual.transform.parent == rangedHandSocket &&
            IsRangedHandSocketHandAttached();

        public void Bind(PlayerWeaponController nextWeaponController)
        {
            if (weaponController == nextWeaponController)
            {
                RefreshVisual(force: false);
                RefreshAllEquipmentVisualTransforms();
                return;
            }

            Unsubscribe();
            weaponController = nextWeaponController;
            if (weaponController != null)
            {
                weaponController.ActiveWeaponSlotChanged += HandleActiveWeaponSlotChanged;
                weaponController.WeaponActionAnimationRequested += HandleWeaponActionAnimationRequested;
                weaponController.WeaponAttackVisualRequested += HandleWeaponAttackVisualRequested;
                currentFacing = PlayerWeaponVisualPosePolicy.SafeAim(weaponController.LastAimDirection);
            }

            RefreshVisual(force: true);
            RefreshAllEquipmentVisualTransforms();
        }

        public void BindMeleeHandSocket(Transform socket)
        {
            meleeHandSocket = socket;
            ApplySocketDefaults(meleeHandSocket, meleeSocketLocalPosition, meleeSocketLocalEuler, meleeSocketLocalScale);
            if (weaponController != null || hasVisibleSlot || activeVisual != null)
            {
                RefreshVisual(force: true);
                RefreshAllEquipmentVisualTransforms();
            }
        }

        public void BindWeaponSockets(
            Transform nextMeleeHandSocket,
            Transform nextRangedHandSocket,
            Transform nextMeleeHolsterSocket,
            Transform nextRangedHolsterSocket,
            Transform nextRangedMuzzleSocket)
        {
            BindWeaponSockets(
                nextMeleeHandSocket,
                nextRangedHandSocket,
                nextMeleeHolsterSocket,
                nextRangedHolsterSocket,
                nextRangedMuzzleSocket,
                null,
                null);
        }

        public void BindWeaponSockets(
            Transform nextMeleeHandSocket,
            Transform nextRangedHandSocket,
            Transform nextMeleeHolsterSocket,
            Transform nextRangedHolsterSocket,
            Transform nextRangedMuzzleSocket,
            Transform nextShieldForearmSocket,
            Transform nextShieldBackSocket)
        {
            meleeHandSocket = nextMeleeHandSocket;
            rangedHandSocket = nextRangedHandSocket;
            meleeHolsterSocket = nextMeleeHolsterSocket;
            rangedHolsterSocket = nextRangedHolsterSocket;
            rangedMuzzleSocket = nextRangedMuzzleSocket;
            shieldForearmSocket = nextShieldForearmSocket;
            shieldBackSocket = nextShieldBackSocket;
            ApplyKnownSocketDefaults();
            if (weaponController != null || hasVisibleSlot || activeVisual != null)
            {
                RefreshVisual(force: true);
                RefreshAllEquipmentVisualTransforms();
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeProfileController();
            RefreshAllEquipmentVisualTransforms();
        }

        private void OnDisable()
        {
            UnsubscribeProfileController();
        }

        private void OnValidate()
        {
            meleeSocketLocalScale = ValidScale(meleeSocketLocalScale, DefaultMeleeSocketLocalScale);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            UnsubscribeProfileController();
        }

        private void Update()
        {
            if (weaponController == null)
            {
                var resolvedWeapon = GetComponent<PlayerWeaponController>();
                if (resolvedWeapon != null)
                {
                    Bind(resolvedWeapon);
                }

                if (weaponController == null)
                {
                    return;
                }
            }

            RefreshVisual(force: false);
            RefreshAllEquipmentVisualTransforms();
            currentFacing = ResolveVisualFacing();
            TickAttack(Time.deltaTime);
            TickMuzzleFlash(Time.deltaTime);
        }

        private void LateUpdate()
        {
            RefreshAllEquipmentVisualTransforms();
            ApplyPose();
        }

        public void ForceRangedAimPose(Vector2 direction)
        {
            currentFacing = PlayerWeaponVisualPosePolicy.SafeAim(direction);
            if (visibleSlot == WeaponSlot.Ranged)
            {
                attackFacing = currentFacing;
            }

            ApplyPose();
        }

        public bool TryResolveRangedMuzzlePose(
            Vector2 aimDirection,
            float lateralOffsetMeters,
            Transform localSpace,
            out Vector3 localOrigin,
            out Vector2 localDirection)
        {
            localOrigin = default;
            localDirection = PlayerWeaponVisualPosePolicy.SafeAim(aimDirection);
            EnsureRoot();
            EnsureSockets();
            if (rangedMuzzleSocket == null)
            {
                return false;
            }

            ForceRangedAimPose(aimDirection);
            var forwardWorld = rangedMuzzleSocket.forward;
            forwardWorld.y = 0f;
            if (forwardWorld.sqrMagnitude < 0.000001f)
            {
                forwardWorld = LocalPlanarToWorld(localDirection);
            }
            else
            {
                forwardWorld.Normalize();
            }

            var sideWorld = new Vector3(-forwardWorld.z, 0f, forwardWorld.x);
            if (sideWorld.sqrMagnitude > 0.000001f)
            {
                sideWorld.Normalize();
            }

            var worldOrigin = rangedMuzzleSocket.position + sideWorld * lateralOffsetMeters;
            localOrigin = localSpace != null ? localSpace.InverseTransformPoint(worldOrigin) : worldOrigin;
            var localForward = localSpace != null ? localSpace.InverseTransformDirection(forwardWorld) : forwardWorld;
            localForward.y = 0f;
            if (localForward.sqrMagnitude > 0.000001f)
            {
                localForward.Normalize();
                localDirection = new Vector2(localForward.x, localForward.z);
            }

            return true;
        }

        public void NormalizeEquipmentVisualScales()
        {
            RefreshAllEquipmentVisualTransforms();
        }

        public void RefreshAllEquipmentVisualTransforms()
        {
            EnsureSockets();
            SubscribeProfileController();
            var slot = hasVisibleSlot
                ? visibleSlot
                : weaponController != null
                    ? weaponController.ActiveWeaponSlot
                    : WeaponSlot.Melee;
            AdoptOrPruneEquipmentWrappers(slot);
            RefreshEquipmentWrapperScales();
            equipmentNormalizationPassCount++;
        }

        private void HandleActiveWeaponSlotChanged(WeaponSlot slot)
        {
            RefreshVisual(force: true);
            RefreshAllEquipmentVisualTransforms();
        }

        private void HandleAnimationProfileChanged(PlayerAnimationProfileDefinition profile)
        {
            RefreshVisual(force: true);
            RefreshAllEquipmentVisualTransforms();
        }

        private void HandleWeaponActionAnimationRequested(
            WeaponSlot slot,
            AttackKind kind,
            Vector2 direction,
            float actionDurationSeconds)
        {
            BeginAttackPose(slot, kind, direction, actionDurationSeconds);
        }

        private void HandleWeaponAttackVisualRequested(WeaponSlot slot, AttackKind kind, Vector2 direction)
        {
            BeginAttackPose(
                slot,
                kind,
                direction,
                kind == AttackKind.Heavy ? HeavyAttackDurationSeconds : LightAttackDurationSeconds);

            if (slot == WeaponSlot.Ranged)
            {
                SpawnMuzzleFlash(kind);
            }
        }

        private void BeginAttackPose(WeaponSlot slot, AttackKind kind, Vector2 direction, float durationSeconds)
        {
            attackSlot = slot;
            attackKind = kind;
            attackFacing = PlayerWeaponVisualPosePolicy.SafeAim(direction);
            currentFacing = attackFacing;
            attackAgeSeconds = 0f;
            attackDurationSeconds = Mathf.Max(0.01f, durationSeconds);
            RefreshAllEquipmentVisualTransforms();
            ApplyPose();
        }

        private void RefreshVisual(bool force)
        {
            EnsureRoot();
            EnsureSockets();
            SubscribeProfileController();
            var nextSlot = weaponController != null ? weaponController.ActiveWeaponSlot : WeaponSlot.Ranged;
            var nextParent = ActiveParentFor(nextSlot);
            AdoptOrPruneEquipmentWrappers(nextSlot);
            EnsureShieldVisual(ShieldParentFor(nextSlot));
            RefreshAllEquipmentVisualTransforms();
            if (nextParent == null)
            {
                return;
            }

            var needsRebuild = force ||
                !hasVisibleSlot ||
                visibleSlot != nextSlot ||
                activeVisual == null ||
                activeVisual.transform.parent != nextParent ||
                (nextSlot == WeaponSlot.Melee && holsteredRangedVisual == null) ||
                (nextSlot == WeaponSlot.Ranged && holsteredMeleeVisual == null);

            if (!needsRebuild)
            {
                return;
            }

            ClearVisuals();
            visibleSlot = nextSlot;
            hasVisibleSlot = true;
            activeVisual = CreateWeaponVisual(
                nextSlot,
                nextParent,
                $"Active{nextSlot}WeaponVisual",
                ActivePresentationScale(nextSlot));

            if (nextSlot == WeaponSlot.Melee)
            {
                holsteredRangedVisual = CreateWeaponVisual(
                    WeaponSlot.Ranged,
                    rangedHolsterSocket,
                    "HolsteredRangedWeaponVisual",
                    DefaultRangedHolsterSocketLocalScale);
            }
            else
            {
                holsteredMeleeVisual = CreateWeaponVisual(
                    WeaponSlot.Melee,
                    meleeHolsterSocket,
                    "HolsteredMeleeWeaponVisual",
                    DefaultMeleeHolsterSocketLocalScale);
            }

            RefreshAllEquipmentVisualTransforms();
            ApplyPose();
        }

        private void EnsureRoot()
        {
            if (heldRoot != null)
            {
                return;
            }

            var root = new GameObject("HeldWeaponRoot");
            root.transform.SetParent(transform, false);
            heldRoot = root.transform;
        }

        private void EnsureSockets()
        {
            EnsureRoot();
            meleeHandSocket ??= FindDescendant(transform, MeleeHandSocketName);
            var rightHand = FindCharacterBone(RightHandBoneName);
            if (meleeHandSocket == null)
            {
                meleeHandSocket = CreateSocket(rightHand != null ? rightHand : heldRoot, MeleeHandSocketName);
            }
            else if (rightHand != null && meleeHandSocket.parent != rightHand)
            {
                meleeHandSocket.SetParent(rightHand, false);
            }

            rangedHandSocket ??= FindDescendant(transform, RangedHandSocketName);
            if (rangedHandSocket == null)
            {
                rangedHandSocket = CreateSocket(rightHand != null ? rightHand : heldRoot, RangedHandSocketName);
            }
            else if (rightHand != null && rangedHandSocket.parent != rightHand)
            {
                rangedHandSocket.SetParent(rightHand, false);
            }

            var visualRoot = FindDescendant(transform, MeshyVisualRootName) ?? transform;
            meleeHolsterSocket ??= FindDescendant(transform, MeleeHolsterSocketName);
            meleeHolsterSocket ??= CreateSocket(visualRoot, MeleeHolsterSocketName);
            rangedHolsterSocket ??= FindDescendant(transform, RangedHolsterSocketName);
            rangedHolsterSocket ??= CreateSocket(visualRoot, RangedHolsterSocketName);
            var leftForearm = FindCharacterBone(LeftForearmBoneName);
            shieldForearmSocket ??= FindDescendant(transform, ShieldForearmSocketName);
            if (shieldForearmSocket == null)
            {
                shieldForearmSocket = CreateSocket(leftForearm != null ? leftForearm : visualRoot, ShieldForearmSocketName);
            }
            else if (leftForearm != null && shieldForearmSocket.parent != leftForearm)
            {
                shieldForearmSocket.SetParent(leftForearm, false);
            }

            var backShieldBone = FindCharacterBone(BackShieldBoneName);
            shieldBackSocket ??= FindDescendant(transform, ShieldBackSocketName);
            if (shieldBackSocket == null)
            {
                shieldBackSocket = CreateSocket(backShieldBone != null ? backShieldBone : visualRoot, ShieldBackSocketName);
            }
            else if (backShieldBone != null && shieldBackSocket.parent != backShieldBone)
            {
                shieldBackSocket.SetParent(backShieldBone, false);
            }

            rangedMuzzleSocket ??= FindDescendant(transform, RangedMuzzleSocketName);
            if (rangedMuzzleSocket == null)
            {
                rangedMuzzleSocket = CreateSocket(rangedHandSocket, RangedMuzzleSocketName);
            }

            if (rangedMuzzleSocket.parent != rangedHandSocket)
            {
                rangedMuzzleSocket.SetParent(rangedHandSocket, false);
            }

            ApplyKnownSocketDefaults();
        }

        private void ApplyKnownSocketDefaults()
        {
            ApplySocketDefaults(meleeHandSocket, meleeSocketLocalPosition, meleeSocketLocalEuler, meleeSocketLocalScale);
            ApplySocketDefaults(
                meleeHolsterSocket,
                DefaultMeleeHolsterSocketLocalPosition,
                DefaultMeleeHolsterSocketLocalEuler,
                DefaultMeleeHolsterSocketLocalScale);
            ApplySocketDefaults(
                rangedHolsterSocket,
                DefaultRangedHolsterSocketLocalPosition,
                DefaultRangedHolsterSocketLocalEuler,
                DefaultRangedHolsterSocketLocalScale);
            ApplySocketDefaults(
                shieldForearmSocket,
                DefaultShieldForearmSocketLocalPosition,
                DefaultShieldForearmSocketLocalEuler,
                DefaultShieldForearmSocketLocalScale);
            ApplySocketDefaults(
                shieldBackSocket,
                DefaultShieldBackSocketLocalPosition,
                DefaultShieldBackSocketLocalEuler,
                DefaultShieldBackSocketLocalScale);

            if (rangedHandSocket != null)
            {
                ApplySocketDefaults(
                    rangedHandSocket,
                    DefaultRangedHandSocketLocalPosition,
                    DefaultRangedHandSocketLocalEuler,
                    DefaultRangedHandSocketLocalScale);
            }

            if (rangedMuzzleSocket != null)
            {
                rangedMuzzleSocket.localPosition = PlayerWeaponVisualPosePolicy.MuzzleLocalPosition();
                rangedMuzzleSocket.localRotation = Quaternion.identity;
                rangedMuzzleSocket.localScale = Vector3.one;
            }
        }

        private void ClearVisuals()
        {
            DestroyVisual(muzzleFlash);
            muzzleFlash = null;

            DestroyVisual(activeVisual);
            DestroyVisual(holsteredMeleeVisual);
            DestroyVisual(holsteredRangedVisual);
            activeVisual = null;
            holsteredMeleeVisual = null;
            holsteredRangedVisual = null;
        }

        private void ApplyPose()
        {
            if (!hasVisibleSlot || activeVisual == null)
            {
                return;
            }

            EnsureSockets();
            var slot = visibleSlot;
            var direction2 = ActiveAttackProgress > 0f && attackSlot == slot
                ? attackFacing
                : currentFacing;
            direction2 = PlayerWeaponVisualPosePolicy.SafeAim(direction2);
            var attackProgress = ActiveAttackProgress > 0f && attackSlot == slot ? ActiveAttackProgress : 0f;
            var attackPositionOffset = PlayerWeaponVisualPosePolicy.AttackLocalPositionOffset(
                slot,
                attackKind,
                direction2,
                attackProgress);
            var attackRotationOffset = PlayerWeaponVisualPosePolicy.AttackLocalRotationOffset(
                slot,
                attackKind,
                attackProgress);
            var aimRotation = PlayerWeaponVisualPosePolicy.AimRotation(direction2) * attackRotationOffset;

            if (slot == WeaponSlot.Ranged && rangedHandSocket != null)
            {
                if (IsRangedHandSocketHandAttached())
                {
                    var rangedWorldRotation = transform.rotation * PlayerWeaponVisualPosePolicy.AimRotation(direction2);
                    rangedHandSocket.rotation = rangedWorldRotation;
                    rangedHandSocket.localScale = DefaultRangedHandSocketLocalScale;
                    activeVisual.transform.localPosition = Vector3.zero;
                    activeVisual.transform.localRotation = Quaternion.identity;
                    ApplyEquipmentWrapperScale(activeVisual.transform, ActivePresentationScale(slot));
                }
                else
                {
                    SetPlayerSpacePose(
                        rangedHandSocket,
                        PlayerWeaponVisualPosePolicy.HeldLocalPosition(slot, direction2) + attackPositionOffset,
                        aimRotation);
                    activeVisual.transform.localPosition = Vector3.zero;
                    activeVisual.transform.localRotation = Quaternion.identity;
                    ApplyEquipmentWrapperScale(activeVisual.transform, ActivePresentationScale(slot));
                }

                RefreshEquipmentWrapperScales();
                return;
            }

            var parent = activeVisual.transform.parent;
            if (parent == null)
            {
                return;
            }

            var worldPosition = parent.position + transform.TransformDirection(attackPositionOffset);
            var worldRotation = transform.rotation * aimRotation;
            activeVisual.transform.position = worldPosition;
            activeVisual.transform.rotation = worldRotation;
            ApplyEquipmentWrapperScale(activeVisual.transform, ActivePresentationScale(slot));
            RefreshEquipmentWrapperScales();
        }

        private void SetPlayerSpacePose(Transform target, Vector3 localPosition, Quaternion localRotation)
        {
            if (target == null)
            {
                return;
            }

            var worldPosition = transform.TransformPoint(localPosition);
            var worldRotation = transform.rotation * localRotation;
            if (target.parent == null)
            {
                target.position = worldPosition;
                target.rotation = worldRotation;
                return;
            }

            target.localPosition = target.parent.InverseTransformPoint(worldPosition);
            target.localRotation = Quaternion.Inverse(target.parent.rotation) * worldRotation;
            target.localScale = DefaultRangedHandSocketLocalScale;
        }

        private void TickAttack(float deltaTime)
        {
            if (attackDurationSeconds <= 0f || attackAgeSeconds >= attackDurationSeconds)
            {
                return;
            }

            attackAgeSeconds = Mathf.Min(attackDurationSeconds, attackAgeSeconds + Mathf.Max(0f, deltaTime));
        }

        private void TickMuzzleFlash(float deltaTime)
        {
            if (muzzleFlash == null)
            {
                return;
            }

            muzzleFlashAgeSeconds += Mathf.Max(0f, deltaTime);
            var remaining = 1f - Mathf.Clamp01(muzzleFlashAgeSeconds / MuzzleFlashDurationSeconds);
            muzzleFlash.transform.localScale = Vector3.one * Mathf.Lerp(0.04f, 0.16f, remaining);
            if (muzzleFlashAgeSeconds < MuzzleFlashDurationSeconds)
            {
                return;
            }

            DestroyVisual(muzzleFlash);
            muzzleFlash = null;
        }

        private void SpawnMuzzleFlash(AttackKind kind)
        {
            EnsureSockets();
            DestroyVisual(muzzleFlash);
            muzzleFlash = null;

            var parent = ActiveMuzzleTransform != null ? ActiveMuzzleTransform : rangedHandSocket != null ? rangedHandSocket : heldRoot;
            if (parent == null)
            {
                return;
            }

            muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = kind == AttackKind.Heavy ? "HeldWeaponMuzzleFlash.Heavy" : "HeldWeaponMuzzleFlash.Light";
            muzzleFlash.transform.SetParent(parent, false);
            muzzleFlash.transform.localPosition = Vector3.zero;
            muzzleFlash.transform.localRotation = Quaternion.identity;
            muzzleFlash.transform.localScale = Vector3.one * (kind == AttackKind.Heavy ? 0.18f : 0.12f);
            MaterialResolver.ApplyTo(muzzleFlash, MaterialRole.Projectile);
            var collider = muzzleFlash.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }

            muzzleFlashAgeSeconds = 0f;
        }

        private GameObject CreateWeaponVisual(
            WeaponSlot slot,
            Transform parent,
            string name,
            Vector3 intendedPresentationScale)
        {
            if (parent == null)
            {
                return null;
            }

            var wrapper = new GameObject(name);
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.localPosition = Vector3.zero;
            wrapper.transform.localRotation = Quaternion.identity;
            ApplyEquipmentWrapperScale(wrapper.transform, intendedPresentationScale);

            var model = PresentationPrefabResolver.InstantiateVisual(RoleFor(slot), wrapper.transform, Vector3.zero, Vector3.one);
            if (model != null)
            {
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = PlayerWeaponVisualPosePolicy.ModelCanonicalLocalRotation(slot);
                model.transform.localScale = Vector3.one;
            }

            NormalizeEquipmentWrapper(wrapper.transform, intendedPresentationScale, RoleFor(slot));
            return wrapper;
        }

        private void EnsureShieldVisual(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            if (equippedShieldVisual == null)
            {
                equippedShieldVisual = new GameObject(EquippedShieldVisualName);
                equippedShieldVisual.transform.SetParent(parent, false);
                var model = PresentationPrefabResolver.InstantiateVisual(
                    PresentationPrefabRole.Armor,
                    equippedShieldVisual.transform,
                    Vector3.zero,
                    Vector3.one);
                if (model != null)
                {
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;
                }
            }
            else if (equippedShieldVisual.transform.parent != parent)
            {
                equippedShieldVisual.transform.SetParent(parent, false);
            }

            equippedShieldVisual.transform.localPosition = Vector3.zero;
            equippedShieldVisual.transform.localRotation = Quaternion.identity;
            NormalizeEquipmentWrapper(
                equippedShieldVisual.transform,
                ShieldPresentationScale(parent),
                PresentationPrefabRole.Armor);
            equippedShieldVisual.SetActive(true);
        }

        private void RefreshEquipmentWrapperScales()
        {
            if (activeVisual != null && hasVisibleSlot)
            {
                NormalizeEquipmentWrapper(activeVisual.transform, ActivePresentationScale(visibleSlot), RoleFor(visibleSlot));
            }

            if (holsteredMeleeVisual != null)
            {
                NormalizeEquipmentWrapper(
                    holsteredMeleeVisual.transform,
                    DefaultMeleeHolsterSocketLocalScale,
                    PresentationPrefabRole.WeaponMelee);
            }

            if (holsteredRangedVisual != null)
            {
                NormalizeEquipmentWrapper(
                    holsteredRangedVisual.transform,
                    DefaultRangedHolsterSocketLocalScale,
                    PresentationPrefabRole.WeaponRanged);
            }

            if (equippedShieldVisual != null)
            {
                NormalizeEquipmentWrapper(
                    equippedShieldVisual.transform,
                    ShieldPresentationScale(equippedShieldVisual.transform.parent),
                    PresentationPrefabRole.Armor);
            }
        }

        private Vector3 ActivePresentationScale(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee
                ? ValidScale(meleeSocketLocalScale, DefaultMeleeSocketLocalScale)
                : DefaultRangedHandSocketLocalScale;
        }

        private Vector3 ShieldPresentationScale(Transform parent)
        {
            if (parent == shieldForearmSocket)
            {
                return DefaultShieldForearmSocketLocalScale;
            }

            if (parent == shieldBackSocket)
            {
                return DefaultShieldBackSocketLocalScale;
            }

            return Vector3.one;
        }

        private static void ApplyEquipmentWrapperScale(Transform wrapper, Vector3 intendedPresentationScale)
        {
            if (wrapper == null)
            {
                return;
            }

            wrapper.localScale = ComputeEquipmentWrapperLocalScale(wrapper.parent, intendedPresentationScale);
        }

        private static void NormalizeEquipmentWrapper(
            Transform wrapper,
            Vector3 intendedPresentationScale,
            PresentationPrefabRole role)
        {
            if (wrapper == null)
            {
                return;
            }

            ApplyEquipmentWrapperScale(wrapper, intendedPresentationScale);
            wrapper.hasChanged = true;

            if (!TryGetRendererBounds(wrapper.gameObject, out var bounds))
            {
                return;
            }

            var maxDimension = MaxDimension(bounds.size);
            if (maxDimension <= MinimumScaleMagnitude)
            {
                return;
            }

            var targetMax = MaxDimensionForRole(role);
            if (maxDimension > targetMax)
            {
                var factor = targetMax / maxDimension;
                wrapper.localScale *= factor;
                wrapper.hasChanged = true;
                return;
            }

            if (maxDimension < MinimumEquipmentMaxDimensionMeters)
            {
                var factor = MinimumEquipmentMaxDimensionMeters / maxDimension;
                wrapper.localScale *= factor;
                wrapper.hasChanged = true;
            }
        }

        public static Vector3 ComputeEquipmentWrapperLocalScale(Transform parent, Vector3 intendedPresentationScale)
        {
            var safeIntendedScale = ValidScale(intendedPresentationScale, Vector3.one);
            if (parent == null)
            {
                return safeIntendedScale;
            }

            var parentScale = parent.lossyScale;
            return new Vector3(
                DivideScale(safeIntendedScale.x, parentScale.x),
                DivideScale(safeIntendedScale.y, parentScale.y),
                DivideScale(safeIntendedScale.z, parentScale.z));
        }

        private static float DivideScale(float intendedScale, float parentScale)
        {
            var parentMagnitude = Mathf.Abs(parentScale);
            return parentMagnitude < MinimumScaleMagnitude
                ? Mathf.Abs(intendedScale)
                : Mathf.Abs(intendedScale) / parentMagnitude;
        }

        private void AdoptOrPruneEquipmentWrappers(WeaponSlot slot)
        {
            EnsureSockets();
            var expected = ExpectedEquipmentWrappers(slot);
            var wrappers = FindKnownEquipmentWrappers();
            foreach (var group in wrappers.GroupBy(wrapper => wrapper.name))
            {
                if (!expected.TryGetValue(group.Key, out var spec))
                {
                    foreach (var stale in group)
                    {
                        DestroyVisual(stale.gameObject);
                    }

                    continue;
                }

                var keep = SelectEquipmentWrapperToKeep(group, spec);
                foreach (var candidate in group)
                {
                    if (candidate != keep)
                    {
                        DestroyVisual(candidate.gameObject);
                    }
                }

                if (keep == null)
                {
                    continue;
                }

                keep.SetParent(spec.Parent, worldPositionStays: false);
                keep.localPosition = Vector3.zero;
                keep.localRotation = Quaternion.identity;
                AssignEquipmentWrapperField(group.Key, keep.gameObject);
            }
        }

        private Dictionary<string, EquipmentWrapperSpec> ExpectedEquipmentWrappers(WeaponSlot slot)
        {
            var expected = new Dictionary<string, EquipmentWrapperSpec>(StringComparer.Ordinal)
            {
                [ActiveWrapperNameFor(slot)] = new(ActiveParentFor(slot), RoleFor(slot)),
                [EquippedShieldVisualName] = new(ShieldParentFor(slot), PresentationPrefabRole.Armor)
            };

            if (slot == WeaponSlot.Melee)
            {
                expected[HolsteredRangedWeaponVisualName] = new(
                    rangedHolsterSocket,
                    PresentationPrefabRole.WeaponRanged);
            }
            else
            {
                expected[HolsteredMeleeWeaponVisualName] = new(
                    meleeHolsterSocket,
                    PresentationPrefabRole.WeaponMelee);
            }

            return expected;
        }

        private static string ActiveWrapperNameFor(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? ActiveMeleeWeaponVisualName : ActiveRangedWeaponVisualName;
        }

        private Transform SelectEquipmentWrapperToKeep(
            IEnumerable<Transform> candidates,
            EquipmentWrapperSpec spec)
        {
            var candidateArray = candidates.Where(candidate => candidate != null).ToArray();
            if (candidateArray.Length == 0 || spec.Parent == null)
            {
                return null;
            }

            return candidateArray.FirstOrDefault(candidate => IsCurrentWrapper(candidate.gameObject)) ??
                candidateArray.FirstOrDefault(candidate => candidate.parent == spec.Parent && ContainsRole(candidate.gameObject, spec.Role)) ??
                candidateArray.FirstOrDefault(candidate => ContainsRole(candidate.gameObject, spec.Role)) ??
                candidateArray.FirstOrDefault(candidate => candidate.parent == spec.Parent) ??
                candidateArray[0];
        }

        private bool IsCurrentWrapper(GameObject candidate)
        {
            return candidate != null &&
                (candidate == activeVisual ||
                    candidate == holsteredMeleeVisual ||
                    candidate == holsteredRangedVisual ||
                    candidate == equippedShieldVisual);
        }

        private Transform[] FindKnownEquipmentWrappers()
        {
            return GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(transform =>
                    transform != null &&
                    transform.gameObject != null &&
                    KnownEquipmentWrapperNames.Contains(transform.name, StringComparer.Ordinal))
                .ToArray();
        }

        private void AssignEquipmentWrapperField(string wrapperName, GameObject wrapper)
        {
            switch (wrapperName)
            {
                case ActiveMeleeWeaponVisualName:
                    if (visibleSlot == WeaponSlot.Melee || !hasVisibleSlot)
                    {
                        activeVisual = wrapper;
                        visibleSlot = WeaponSlot.Melee;
                        hasVisibleSlot = true;
                    }
                    break;
                case ActiveRangedWeaponVisualName:
                    if (visibleSlot == WeaponSlot.Ranged || !hasVisibleSlot)
                    {
                        activeVisual = wrapper;
                        visibleSlot = WeaponSlot.Ranged;
                        hasVisibleSlot = true;
                    }
                    break;
                case HolsteredMeleeWeaponVisualName:
                    holsteredMeleeVisual = wrapper;
                    break;
                case HolsteredRangedWeaponVisualName:
                    holsteredRangedVisual = wrapper;
                    break;
                case EquippedShieldVisualName:
                    equippedShieldVisual = wrapper;
                    break;
            }
        }

        private void ResolveReferences()
        {
            weaponController ??= GetComponent<PlayerWeaponController>();
            animationProfileController ??= GetComponent<PlayerAnimationProfileController>();
        }

        private void SubscribeProfileController()
        {
            ResolveReferences();
            if (subscribedProfileController == animationProfileController)
            {
                return;
            }

            UnsubscribeProfileController();
            subscribedProfileController = animationProfileController;
            if (subscribedProfileController != null)
            {
                subscribedProfileController.ProfileChanged += HandleAnimationProfileChanged;
            }
        }

        private void UnsubscribeProfileController()
        {
            if (subscribedProfileController != null)
            {
                subscribedProfileController.ProfileChanged -= HandleAnimationProfileChanged;
                subscribedProfileController = null;
            }
        }

        private Transform ActiveParentFor(WeaponSlot slot)
        {
            EnsureSockets();
            return slot == WeaponSlot.Melee ? meleeHandSocket : rangedHandSocket;
        }

        private Transform ShieldParentFor(WeaponSlot slot)
        {
            EnsureSockets();
            animationProfileController ??= GetComponent<PlayerAnimationProfileController>();
            if (animationProfileController == null ||
                !animationProfileController.AllowsShieldInHand ||
                animationProfileController.RequiresTwoHandedWeapon ||
                ActiveWeaponIsDoubleHandedForPresentation(slot))
            {
                return shieldBackSocket != null ? shieldBackSocket : heldRoot;
            }

            return shieldForearmSocket != null ? shieldForearmSocket : heldRoot;
        }

        private bool ActiveWeaponIsDoubleHandedForPresentation(WeaponSlot slot)
        {
            var weapon = ResolveWeaponForSlot(slot);
            return weapon != null && weapon.IsDoubleHandedForPresentation;
        }

        private WeaponDefinition ResolveWeaponForSlot(WeaponSlot slot)
        {
            if (weaponController == null || weaponController.WeaponCatalog == null)
            {
                return null;
            }

            var weaponId = slot == WeaponSlot.Melee ? weaponController.MeleeWeaponId : weaponController.RangedWeaponId;
            return weaponController.WeaponCatalog.Resolve(weaponId, slot);
        }

        private Vector2 ResolveVisualFacing()
        {
            if (weaponController == null)
            {
                return currentFacing;
            }

            return weaponController.HasVisualAimCommitment
                ? PlayerWeaponVisualPosePolicy.SafeAim(weaponController.VisualAimDirection)
                : PlayerWeaponVisualPosePolicy.SafeAim(weaponController.LastAimDirection);
        }

        private void Unsubscribe()
        {
            if (weaponController == null)
            {
                return;
            }

            weaponController.ActiveWeaponSlotChanged -= HandleActiveWeaponSlotChanged;
            weaponController.WeaponActionAnimationRequested -= HandleWeaponActionAnimationRequested;
            weaponController.WeaponAttackVisualRequested -= HandleWeaponAttackVisualRequested;
        }

        private float ActiveAttackProgress => attackDurationSeconds > 0f && attackAgeSeconds < attackDurationSeconds
            ? Mathf.Clamp01(attackAgeSeconds / attackDurationSeconds)
            : 0f;

        private static PresentationPrefabRole RoleFor(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? PresentationPrefabRole.WeaponMelee : PresentationPrefabRole.WeaponRanged;
        }

        private static bool ContainsRole(GameObject root, PresentationPrefabRole role)
        {
            return root != null &&
                root.GetComponentsInChildren<PresentationVisualMarker>(includeInactive: true)
                    .Any(marker => marker != null && marker.Role == role);
        }

        private Transform FindCharacterBone(string boneName)
        {
            var visualRoot = FindDescendant(transform, MeshyVisualRootName);
            var modelRoot = visualRoot != null ? FindDescendant(visualRoot, MeshyModelName) : null;
            var searchRoot = modelRoot != null ? modelRoot : visualRoot != null ? visualRoot : transform;
            return FindDescendant(searchRoot, boneName);
        }

        private bool IsRangedHandSocketHandAttached()
        {
            return rangedHandSocket != null &&
                rangedHandSocket.parent != null &&
                IsNormalizedBoneNameMatch(NormalizeTransformName(rangedHandSocket.parent.name), RightHandBoneName);
        }

        private Vector3 LocalPlanarToWorld(Vector2 direction)
        {
            var local = PlayerWeaponVisualPosePolicy.PlanarForward(direction);
            var parent = transform.parent;
            return parent != null ? parent.TransformDirection(local).normalized : transform.TransformDirection(local).normalized;
        }

        private static Transform CreateSocket(Transform parent, string socketName)
        {
            if (parent == null)
            {
                return null;
            }

            var socketObject = new GameObject(socketName);
            socketObject.transform.SetParent(parent, false);
            return socketObject.transform;
        }

        private static void ApplySocketDefaults(
            Transform socket,
            Vector3 localPosition,
            Vector3 localEuler,
            Vector3 localScale)
        {
            if (socket == null)
            {
                return;
            }

            socket.localPosition = localPosition;
            socket.localRotation = Quaternion.Euler(localEuler);
            socket.localScale = localScale;
        }

        private static Vector3 ValidScale(Vector3 value, Vector3 fallback)
        {
            return new Vector3(
                Mathf.Abs(value.x) < 0.0001f ? fallback.x : value.x,
                Mathf.Abs(value.y) < 0.0001f ? fallback.y : value.y,
                Mathf.Abs(value.z) < 0.0001f ? fallback.z : value.z);
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name == childName ||
                    IsNormalizedBoneNameMatch(NormalizeTransformName(child.name), childName))
                {
                    return child;
                }
            }

            return null;
        }

        private static string NormalizeTransformName(string transformName)
        {
            if (string.IsNullOrEmpty(transformName))
            {
                return string.Empty;
            }

            var namespaceSeparator = transformName.LastIndexOf(':');
            return namespaceSeparator >= 0 && namespaceSeparator < transformName.Length - 1
                ? transformName[(namespaceSeparator + 1)..]
                : transformName;
        }

        private static bool IsNormalizedBoneNameMatch(string normalizedName, string expectedName)
        {
            if (string.Equals(normalizedName, expectedName, System.StringComparison.Ordinal))
            {
                return true;
            }

            return expectedName switch
            {
                BackShieldBoneName => string.Equals(normalizedName, "Spine2", System.StringComparison.Ordinal),
                _ => false
            };
        }

        private static void DestroyVisual(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(visual);
            }
            else
            {
                DestroyImmediate(visual);
            }
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
        {
            bounds = default;
            if (root == null)
            {
                return false;
            }

            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true)
                .Where(renderer => renderer != null && renderer.enabled)
                .ToArray();
            if (renderers.Length == 0)
            {
                return false;
            }

            bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            return true;
        }

        private static float MaxDimension(Vector3 size)
        {
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        public static bool IsAbsurdEquipmentBounds(PresentationPrefabRole role, Vector3 boundsSize)
        {
            var maxDimension = MaxDimension(boundsSize);
            return maxDimension < MinimumEquipmentMaxDimensionMeters ||
                maxDimension > MaximumAbsurdEquipmentMaxDimensionMeters ||
                maxDimension > MaxDimensionForRole(role);
        }

        public static float MaxDimensionForRole(PresentationPrefabRole role)
        {
            return role switch
            {
                PresentationPrefabRole.Armor => MaximumShieldMaxDimensionMeters,
                PresentationPrefabRole.WeaponMelee => MaximumMeleeMaxDimensionMeters,
                PresentationPrefabRole.WeaponRanged => MaximumRangedMaxDimensionMeters,
                _ => MaximumAbsurdEquipmentMaxDimensionMeters
            };
        }

        private readonly struct EquipmentWrapperSpec
        {
            public EquipmentWrapperSpec(Transform parent, PresentationPrefabRole role)
            {
                Parent = parent;
                Role = role;
            }

            public Transform Parent { get; }

            public PresentationPrefabRole Role { get; }
        }
    }
}
