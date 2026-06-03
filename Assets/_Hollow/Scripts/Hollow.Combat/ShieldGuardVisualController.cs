using System;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Combat
{
    public sealed class ShieldGuardVisualController : MonoBehaviour
    {
        private const string ShieldObjectName = "ShieldGuard.Visual";
        private static readonly Vector3 FallbackShieldScale = new(0.88f, 0.58f, 0.08f);

        private GameObject shieldObject;
        private Renderer[] shieldRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock shieldPropertyBlock;
        private PlayerHeldWeaponVisualController heldWeaponVisual;
        private ShieldGuardProfileDefinition shieldProfile;
        private ShieldGuardResult feedbackResult;
        private float feedbackUntil;
        private bool guardVisualActive;

        public bool IsVisible => guardVisualActive || (shieldObject != null && shieldObject.activeSelf);

        public void Configure(ShieldGuardProfileDefinition profile)
        {
            shieldProfile = ShieldGuardProfileDefinition.Resolve(profile);
            guardVisualActive = false;
            if (shieldObject != null)
            {
                shieldObject.SetActive(false);
            }
        }

        public void SetState(bool isGuarding, bool isInParryWindow, Vector3 guardFacing)
        {
            var equippedShield = ResolveEquippedShieldVisual();
            if (equippedShield != null)
            {
                guardVisualActive = isGuarding;
                if (shieldObject != null)
                {
                    shieldObject.SetActive(false);
                }

                CaptureRenderers(equippedShield);
                if (!isGuarding)
                {
                    ClearFeedbackTint();
                    return;
                }

                var equippedRole = Time.time < feedbackUntil
                    ? RoleForResult(feedbackResult)
                    : isInParryWindow
                        ? MaterialRole.ShieldParry
                        : MaterialRole.ShieldGuard;
                ApplyFeedbackTint(equippedRole);
                return;
            }

            EnsureShieldObject();
            if (!isGuarding)
            {
                guardVisualActive = false;
                shieldObject.SetActive(false);
                return;
            }

            var profile = ShieldGuardProfileDefinition.Resolve(shieldProfile);
            var facing = Flatten(guardFacing);
            if (facing.sqrMagnitude < 0.001f)
            {
                facing = Vector3.forward;
            }

            shieldObject.SetActive(true);
            guardVisualActive = true;
            shieldObject.transform.localPosition = facing.normalized * profile.ShieldVisualDistanceMeters + new Vector3(0f, profile.ShieldVisualHeightMeters, 0f);
            shieldObject.transform.localRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            var role = Time.time < feedbackUntil
                ? RoleForResult(feedbackResult)
                : isInParryWindow
                    ? MaterialRole.ShieldParry
                    : MaterialRole.ShieldGuard;
            ApplyFeedbackTint(role);
        }

        public void ShowFeedback(ShieldGuardResult result)
        {
            feedbackResult = result;
            feedbackUntil = Time.time + ShieldGuardProfileDefinition.Resolve(shieldProfile).ShieldFeedbackSeconds;
            var equippedShield = ResolveEquippedShieldVisual();
            if (equippedShield != null && guardVisualActive)
            {
                CaptureRenderers(equippedShield);
                ApplyFeedbackTint(RoleForResult(result));
                return;
            }

            if (shieldObject != null && shieldObject.activeSelf)
            {
                ApplyFeedbackTint(RoleForResult(result));
            }
        }

        private void EnsureShieldObject()
        {
            if (shieldObject != null)
            {
                return;
            }

            shieldObject = new GameObject(ShieldObjectName);
            shieldObject.name = ShieldObjectName;
            shieldObject.transform.SetParent(transform, worldPositionStays: false);
            shieldObject.transform.localPosition = Vector3.zero;
            shieldObject.transform.localRotation = Quaternion.identity;
            shieldObject.transform.localScale = Vector3.one;

            var visual = PresentationPrefabResolver.InstantiateVisual(
                PresentationPrefabRole.Armor,
                shieldObject.transform,
                Vector3.zero,
                Vector3.one);
            if (visual != null &&
                visual.TryGetComponent<PresentationVisualMarker>(out var marker) &&
                marker.IsFallback)
            {
                visual.transform.localScale = FallbackShieldScale;
            }

            if (visual == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "FallbackShieldGuard.Visual";
                visual.transform.SetParent(shieldObject.transform, worldPositionStays: false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = FallbackShieldScale;
                MaterialResolver.ApplyTo(visual, MaterialRole.ShieldGuard);
            }

            RemoveGameplayCollider(visual);
            shieldRenderers = shieldObject.GetComponentsInChildren<Renderer>(includeInactive: true);
            shieldPropertyBlock = new MaterialPropertyBlock();
            ApplyFeedbackTint(MaterialRole.ShieldGuard);
        }

        private GameObject ResolveEquippedShieldVisual()
        {
            heldWeaponVisual ??= GetComponent<PlayerHeldWeaponVisualController>();
            var visual = heldWeaponVisual != null ? heldWeaponVisual.EquippedShieldVisual : null;
            return visual != null && visual.activeInHierarchy ? visual : null;
        }

        private void CaptureRenderers(GameObject root)
        {
            shieldRenderers = root != null
                ? root.GetComponentsInChildren<Renderer>(includeInactive: true)
                : Array.Empty<Renderer>();
            shieldPropertyBlock ??= new MaterialPropertyBlock();
        }

        private void ApplyFeedbackTint(MaterialRole role)
        {
            if (shieldRenderers == null || shieldRenderers.Length == 0)
            {
                return;
            }

            shieldPropertyBlock ??= new MaterialPropertyBlock();
            var tint = TintForRole(role);
            foreach (var renderer in shieldRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                shieldPropertyBlock.Clear();
                renderer.GetPropertyBlock(shieldPropertyBlock);
                shieldPropertyBlock.SetColor("_BaseColor", tint);
                shieldPropertyBlock.SetColor("_Color", tint);
                renderer.SetPropertyBlock(shieldPropertyBlock);
            }
        }

        private void ClearFeedbackTint()
        {
            if (shieldRenderers == null || shieldRenderers.Length == 0)
            {
                return;
            }

            shieldPropertyBlock ??= new MaterialPropertyBlock();
            shieldPropertyBlock.Clear();
            foreach (var renderer in shieldRenderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.SetPropertyBlock(shieldPropertyBlock);
            }
        }

        private static Color TintForRole(MaterialRole role)
        {
            var color = MaterialResolver.FallbackColorFor(role);
            color.a = 1f;
            var strength = role switch
            {
                MaterialRole.ShieldParry => 0.36f,
                MaterialRole.ShieldBlock => 0.28f,
                MaterialRole.ShieldUnavailable => 0.42f,
                _ => 0.18f
            };
            var tint = Color.Lerp(Color.white, color, strength);
            tint.a = 1f;
            return tint;
        }

        private static void RemoveGameplayCollider(GameObject visual)
        {
            if (visual == null)
            {
                return;
            }

            foreach (var collider in visual.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                RemoveCollider(collider);
            }
        }

        private static void RemoveCollider(Collider collider)
        {
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }
        }

        private static MaterialRole RoleForResult(ShieldGuardResult result)
        {
            return result switch
            {
                ShieldGuardResult.PerfectParry => MaterialRole.ShieldParry,
                ShieldGuardResult.GuardBlocked => MaterialRole.ShieldBlock,
                ShieldGuardResult.FailedNoStamina => MaterialRole.ShieldUnavailable,
                ShieldGuardResult.FailedOutOfCone => MaterialRole.ShieldUnavailable,
                ShieldGuardResult.RejectedThreat => MaterialRole.ShieldBlock,
                _ => MaterialRole.ShieldGuard
            };
        }

        private static Vector3 Flatten(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
