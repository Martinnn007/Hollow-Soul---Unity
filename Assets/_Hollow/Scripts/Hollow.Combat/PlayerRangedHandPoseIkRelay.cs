using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerRangedHandPoseIkRelay : MonoBehaviour
    {
        [SerializeField] private PlayerRangedHandPoseController rangedOwner;
        [SerializeField] private PlayerShieldGuardPoseController shieldOwner;

        public PlayerRangedHandPoseController Owner => rangedOwner;

        public PlayerShieldGuardPoseController ShieldOwner => shieldOwner;

        public void Bind(PlayerRangedHandPoseController nextOwner)
        {
            rangedOwner = nextOwner;
        }

        public void BindShield(PlayerShieldGuardPoseController nextOwner)
        {
            shieldOwner = nextOwner;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (rangedOwner == null)
            {
                rangedOwner = GetComponentInParent<PlayerRangedHandPoseController>();
            }

            if (shieldOwner == null)
            {
                shieldOwner = GetComponentInParent<PlayerShieldGuardPoseController>();
            }

            rangedOwner?.ApplyAnimatorIK(layerIndex);
            shieldOwner?.ApplyAnimatorIK(layerIndex);
        }
    }
}
