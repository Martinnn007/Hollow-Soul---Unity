using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerHeldWeaponVisualController : MonoBehaviour
    {
        public const string MeleeHandSocketName = "MainCharacter_RightHandWeaponSocket";

        private const string MeshyVisualRootName = "MainCharacter_VisualRoot";
        private const string MeshyModelName = "MainCharacter_MeshyModel";
        private const string RightHandBoneName = "RightHand";
        private const float MeleeHeightMeters = 0.78f;
        private const float RangedHeightMeters = 0.74f;
        private const float MeleeForwardOffsetMeters = 0.34f;
        private const float RangedForwardOffsetMeters = 0.44f;
        private const float MeleeSideOffsetMeters = 0.24f;
        private const float RangedSideOffsetMeters = 0.28f;
        private const float LightAttackDurationSeconds = 0.14f;
        private const float HeavyAttackDurationSeconds = 0.22f;
        private const float MuzzleFlashDurationSeconds = 0.1f;

        [SerializeField] private PlayerWeaponController weaponController;
        [SerializeField] private Transform meleeHandSocket;
        [SerializeField] private Vector3 meleeSocketLocalPosition = new(0.03f, 0f, 0.02f);
        [SerializeField] private Vector3 meleeSocketLocalEuler = new(90f, 0f, 0f);
        [SerializeField] private Vector3 meleeSocketLocalScale = new(0.75f, 0.75f, 0.75f);

        private Transform heldRoot;
        private Transform motionPivot;
        private GameObject activeVisual;
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

        public Transform MeleeHandSocket => meleeHandSocket;

        public bool IsUsingHandAttachedMeleeVisual =>
            visibleSlot == WeaponSlot.Melee &&
            activeVisual != null &&
            meleeHandSocket != null &&
            activeVisual.transform.parent == meleeHandSocket;

        public void Bind(PlayerWeaponController nextWeaponController)
        {
            if (weaponController == nextWeaponController)
            {
                RefreshVisual(force: false);
                return;
            }

            Unsubscribe();
            weaponController = nextWeaponController;
            if (weaponController != null)
            {
                weaponController.ActiveWeaponSlotChanged += HandleActiveWeaponSlotChanged;
                weaponController.WeaponAttackVisualRequested += HandleWeaponAttackVisualRequested;
                currentFacing = SafeAim(weaponController.LastAimDirection);
            }

            RefreshVisual(force: true);
        }

        public void BindMeleeHandSocket(Transform socket)
        {
            meleeHandSocket = socket;
            ApplyMeleeSocketDefaults();
            if (hasVisibleSlot || activeVisual != null)
            {
                RefreshVisual(force: true);
            }
        }

        private void OnValidate()
        {
            if (Mathf.Abs(meleeSocketLocalScale.x) < 0.0001f)
            {
                meleeSocketLocalScale.x = 0.75f;
            }

            if (Mathf.Abs(meleeSocketLocalScale.y) < 0.0001f)
            {
                meleeSocketLocalScale.y = 0.75f;
            }

            if (Mathf.Abs(meleeSocketLocalScale.z) < 0.0001f)
            {
                meleeSocketLocalScale.z = 0.75f;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
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
            currentFacing = SafeAim(weaponController.LastAimDirection);
            TickAttack(Time.deltaTime);
            TickMuzzleFlash(Time.deltaTime);
            ApplyPose();
        }

        private void HandleActiveWeaponSlotChanged(WeaponSlot slot)
        {
            RefreshVisual(force: true);
        }

        private void HandleWeaponAttackVisualRequested(WeaponSlot slot, AttackKind kind, Vector2 direction)
        {
            attackSlot = slot;
            attackKind = kind;
            attackFacing = SafeAim(direction);
            attackAgeSeconds = 0f;
            attackDurationSeconds = kind == AttackKind.Heavy ? HeavyAttackDurationSeconds : LightAttackDurationSeconds;

            if (slot == WeaponSlot.Ranged)
            {
                SpawnMuzzleFlash(kind);
            }
        }

        private void RefreshVisual(bool force)
        {
            EnsureRoot();
            var nextSlot = weaponController != null ? weaponController.ActiveWeaponSlot : WeaponSlot.Ranged;
            var parent = ParentFor(nextSlot);
            if (parent == null)
            {
                return;
            }

            if (!force &&
                hasVisibleSlot &&
                visibleSlot == nextSlot &&
                activeVisual != null &&
                activeVisual.transform.parent == parent)
            {
                return;
            }

            ClearVisual();
            visibleSlot = nextSlot;
            hasVisibleSlot = true;
            activeVisual = PresentationPrefabResolver.InstantiateVisual(RoleFor(nextSlot), parent, Vector3.zero, Vector3.one);
            if (activeVisual != null)
            {
                activeVisual.transform.localPosition = Vector3.zero;
                activeVisual.transform.localRotation = Quaternion.identity;
                activeVisual.transform.localScale = Vector3.one;
            }

            ApplyPose();
        }

        private void EnsureRoot()
        {
            if (heldRoot == null)
            {
                var root = new GameObject("HeldWeaponRoot");
                root.transform.SetParent(transform, false);
                heldRoot = root.transform;
            }

            if (motionPivot == null)
            {
                var pivot = new GameObject("HeldWeaponMotionPivot");
                pivot.transform.SetParent(heldRoot, false);
                motionPivot = pivot.transform;
            }
        }

        private void ClearVisual()
        {
            if (muzzleFlash != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(muzzleFlash);
                }
                else
                {
                    DestroyImmediate(muzzleFlash);
                }

                muzzleFlash = null;
            }

            var visualToDestroy = activeVisual;
            activeVisual = null;
            DestroyVisual(visualToDestroy);

            if (motionPivot == null)
            {
                return;
            }

            for (var index = motionPivot.childCount - 1; index >= 0; index--)
            {
                var child = motionPivot.GetChild(index);
                if (child.gameObject == visualToDestroy)
                {
                    continue;
                }

                DestroyVisual(child.gameObject);
            }

            muzzleFlash = null;
        }

        private void ApplyPose()
        {
            if (heldRoot == null || motionPivot == null)
            {
                return;
            }

            var slot = hasVisibleSlot ? visibleSlot : WeaponSlot.Ranged;
            if (slot == WeaponSlot.Melee && IsUsingHandAttachedMeleeVisual)
            {
                heldRoot.localPosition = Vector3.zero;
                heldRoot.localRotation = Quaternion.identity;
                heldRoot.localScale = Vector3.one;
                motionPivot.localPosition = Vector3.zero;
                motionPivot.localRotation = Quaternion.identity;
                motionPivot.localScale = Vector3.one;
                return;
            }

            var direction2 = ActiveAttackProgress > 0f ? attackFacing : currentFacing;
            direction2 = SafeAim(direction2);
            var direction = new Vector3(direction2.x, 0f, direction2.y).normalized;
            var side = new Vector3(-direction.z, 0f, direction.x);
            var height = slot == WeaponSlot.Melee ? MeleeHeightMeters : RangedHeightMeters;
            var forward = slot == WeaponSlot.Melee ? MeleeForwardOffsetMeters : RangedForwardOffsetMeters;
            var sideOffset = slot == WeaponSlot.Melee ? MeleeSideOffsetMeters : RangedSideOffsetMeters;
            var basePosition = direction * forward + side * sideOffset + Vector3.up * height;
            var baseRotation = Quaternion.LookRotation(direction, Vector3.up);

            var progress = ActiveAttackProgress;
            if (progress > 0f && attackSlot == slot)
            {
                var punch = Mathf.Sin(progress * Mathf.PI);
                if (slot == WeaponSlot.Melee)
                {
                    var arcDegrees = attackKind == AttackKind.Heavy ? 92f : 62f;
                    var startDegrees = attackKind == AttackKind.Heavy ? -54f : -34f;
                    var yaw = startDegrees + arcDegrees * EaseOut(progress);
                    var thrust = direction * (attackKind == AttackKind.Heavy ? 0.22f : 0.13f) * punch;
                    basePosition += thrust;
                    baseRotation *= Quaternion.Euler(0f, yaw, 0f);
                }
                else
                {
                    var recoil = attackKind == AttackKind.Heavy ? 0.18f : 0.09f;
                    basePosition -= direction * recoil * punch;
                    baseRotation *= Quaternion.Euler(attackKind == AttackKind.Heavy ? -9f * punch : -5f * punch, 0f, 0f);
                }
            }

            heldRoot.localPosition = basePosition;
            heldRoot.localRotation = baseRotation;
            motionPivot.localPosition = Vector3.zero;
            motionPivot.localRotation = Quaternion.identity;
            motionPivot.localScale = Vector3.one;
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

            if (Application.isPlaying)
            {
                Destroy(muzzleFlash);
            }
            else
            {
                DestroyImmediate(muzzleFlash);
            }

            muzzleFlash = null;
        }

        private void SpawnMuzzleFlash(AttackKind kind)
        {
            if (heldRoot == null)
            {
                return;
            }

            if (muzzleFlash != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(muzzleFlash);
                }
                else
                {
                    DestroyImmediate(muzzleFlash);
                }
            }

            muzzleFlash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            muzzleFlash.name = kind == AttackKind.Heavy ? "HeldWeaponMuzzleFlash.Heavy" : "HeldWeaponMuzzleFlash.Light";
            muzzleFlash.transform.SetParent(heldRoot, false);
            muzzleFlash.transform.localPosition = new Vector3(0f, 0f, 0.44f);
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

        private void Unsubscribe()
        {
            if (weaponController == null)
            {
                return;
            }

            weaponController.ActiveWeaponSlotChanged -= HandleActiveWeaponSlotChanged;
            weaponController.WeaponAttackVisualRequested -= HandleWeaponAttackVisualRequested;
        }

        private float ActiveAttackProgress => attackDurationSeconds > 0f && attackAgeSeconds < attackDurationSeconds
            ? Mathf.Clamp01(attackAgeSeconds / attackDurationSeconds)
            : 0f;

        private static float EaseOut(float value)
        {
            value = Mathf.Clamp01(value);
            return 1f - (1f - value) * (1f - value);
        }

        private static PresentationPrefabRole RoleFor(WeaponSlot slot)
        {
            return slot == WeaponSlot.Melee ? PresentationPrefabRole.WeaponMelee : PresentationPrefabRole.WeaponRanged;
        }

        private Transform ParentFor(WeaponSlot slot)
        {
            if (slot == WeaponSlot.Melee)
            {
                var socket = ResolveMeleeHandSocket();
                if (socket != null)
                {
                    return socket;
                }
            }

            return motionPivot;
        }

        private Transform ResolveMeleeHandSocket()
        {
            if (meleeHandSocket != null)
            {
                return meleeHandSocket;
            }

            meleeHandSocket = FindDescendant(transform, MeleeHandSocketName);
            if (meleeHandSocket != null)
            {
                return meleeHandSocket;
            }

            var rightHand = FindRightHandBone();
            if (rightHand == null)
            {
                return null;
            }

            var socketObject = new GameObject(MeleeHandSocketName);
            socketObject.transform.SetParent(rightHand, false);
            meleeHandSocket = socketObject.transform;
            ApplyMeleeSocketDefaults();
            return meleeHandSocket;
        }

        private Transform FindRightHandBone()
        {
            var visualRoot = FindDescendant(transform, MeshyVisualRootName);
            var modelRoot = visualRoot != null ? FindDescendant(visualRoot, MeshyModelName) : null;
            var searchRoot = modelRoot != null ? modelRoot : visualRoot != null ? visualRoot : transform;
            return FindDescendant(searchRoot, RightHandBoneName);
        }

        private void ApplyMeleeSocketDefaults()
        {
            if (meleeHandSocket == null)
            {
                return;
            }

            meleeHandSocket.localPosition = meleeSocketLocalPosition;
            meleeHandSocket.localRotation = Quaternion.Euler(meleeSocketLocalEuler);
            meleeHandSocket.localScale = meleeSocketLocalScale;
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name == childName)
                {
                    return child;
                }
            }

            return null;
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

        private static Vector2 SafeAim(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        }
    }
}
