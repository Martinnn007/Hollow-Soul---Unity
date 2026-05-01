using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerHeldWeaponVisualController : MonoBehaviour
    {
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

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (weaponController == null)
            {
                return;
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
            if (!force && hasVisibleSlot && visibleSlot == nextSlot && activeVisual != null)
            {
                return;
            }

            ClearVisual();
            visibleSlot = nextSlot;
            hasVisibleSlot = true;
            activeVisual = PresentationPrefabResolver.InstantiateVisual(RoleFor(nextSlot), motionPivot, Vector3.zero, Vector3.one);
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
            if (motionPivot == null)
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

                muzzleFlash = null;
            }

            for (var index = motionPivot.childCount - 1; index >= 0; index--)
            {
                var child = motionPivot.GetChild(index);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            activeVisual = null;
            muzzleFlash = null;
        }

        private void ApplyPose()
        {
            if (heldRoot == null || motionPivot == null)
            {
                return;
            }

            var slot = hasVisibleSlot ? visibleSlot : WeaponSlot.Ranged;
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

        private static Vector2 SafeAim(Vector2 direction)
        {
            return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        }
    }
}
