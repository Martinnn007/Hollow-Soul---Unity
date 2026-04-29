using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class ShieldGuardVisualController : MonoBehaviour
    {
        private const string ShieldObjectName = "ShieldGuard.Visual";

        private GameObject shieldObject;
        private Renderer shieldRenderer;
        private ShieldGuardProfileDefinition shieldProfile;
        private ShieldGuardResult feedbackResult;
        private float feedbackUntil;

        public bool IsVisible => shieldObject != null && shieldObject.activeSelf;

        public void Configure(ShieldGuardProfileDefinition profile)
        {
            shieldProfile = ShieldGuardProfileDefinition.Resolve(profile);
            EnsureShieldObject();
            shieldObject.SetActive(false);
        }

        public void SetState(bool isGuarding, bool isInParryWindow, Vector3 guardFacing)
        {
            EnsureShieldObject();
            if (!isGuarding)
            {
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
            shieldObject.transform.localPosition = facing.normalized * profile.ShieldVisualDistanceMeters + new Vector3(0f, profile.ShieldVisualHeightMeters, 0f);
            shieldObject.transform.localRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);

            var role = Time.time < feedbackUntil
                ? RoleForResult(feedbackResult)
                : isInParryWindow
                    ? MaterialRole.ShieldParry
                    : MaterialRole.ShieldGuard;
            MaterialResolver.ApplyTo(shieldRenderer, role);
        }

        public void ShowFeedback(ShieldGuardResult result)
        {
            feedbackResult = result;
            feedbackUntil = Time.time + ShieldGuardProfileDefinition.Resolve(shieldProfile).ShieldFeedbackSeconds;
            if (shieldObject != null && shieldObject.activeSelf)
            {
                MaterialResolver.ApplyTo(shieldRenderer, RoleForResult(result));
            }
        }

        private void EnsureShieldObject()
        {
            if (shieldObject != null)
            {
                return;
            }

            shieldObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shieldObject.name = ShieldObjectName;
            shieldObject.transform.SetParent(transform, worldPositionStays: false);
            shieldObject.transform.localScale = new Vector3(0.88f, 0.58f, 0.08f);
            shieldRenderer = shieldObject.GetComponent<Renderer>();
            MaterialResolver.ApplyTo(shieldRenderer, MaterialRole.ShieldGuard);

            var collider = shieldObject.GetComponent<Collider>();
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
