using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerRollVisualController : MonoBehaviour
    {
        private const float TrailIntervalSeconds = 0.08f;
        private const float CueHeightMeters = 0.16f;

        [SerializeField] private PlayerWeaponController weaponController;

        private PlayerRollPhase observedPhase = PlayerRollPhase.None;
        private float nextTrailTimeSeconds;

        public PlayerRollPhase LastObservedPhase => observedPhase;

        public void Bind(PlayerWeaponController nextWeaponController)
        {
            weaponController = nextWeaponController;
            observedPhase = weaponController != null ? weaponController.CurrentRollPhase : PlayerRollPhase.None;
            nextTrailTimeSeconds = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (weaponController == null)
            {
                observedPhase = PlayerRollPhase.None;
                return;
            }

            var phase = weaponController.CurrentRollPhase;
            if (phase != observedPhase)
            {
                EnterPhase(phase, timeSeconds);
                observedPhase = phase;
            }

            if (phase == PlayerRollPhase.InvulnerableTravel && timeSeconds >= nextTrailTimeSeconds)
            {
                PlayCue(VfxCueId.PlayerRollTrail, 0f);
                nextTrailTimeSeconds = timeSeconds + TrailIntervalSeconds;
            }
        }

        private void EnterPhase(PlayerRollPhase phase, float timeSeconds)
        {
            switch (phase)
            {
                case PlayerRollPhase.Startup:
                    PlayCue(VfxCueId.PlayerRollStart, 0.18f);
                    AudioPresenter.Play(AudioCueId.PlayerRollStart, transform.position);
                    nextTrailTimeSeconds = timeSeconds;
                    break;
                case PlayerRollPhase.InvulnerableTravel:
                    PlayCue(VfxCueId.PlayerRollTrail, 0f);
                    nextTrailTimeSeconds = timeSeconds + TrailIntervalSeconds;
                    break;
                case PlayerRollPhase.Recovery:
                    PlayCue(VfxCueId.PlayerRollRecovery, -0.08f);
                    break;
            }
        }

        private void PlayCue(VfxCueId cue, float directionOffsetMeters)
        {
            var direction = weaponController != null ? weaponController.RollDirection : Vector2.up;
            var offset = new Vector3(direction.x, 0f, direction.y) * directionOffsetMeters;
            VfxPresenter.Play(cue, transform.position + offset + Vector3.up * CueHeightMeters, transform.parent);
        }
    }
}
