using Hollow.Entities;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public class EnemyRuntimeController : MonoBehaviour
    {
        [SerializeField] private float speedMetersPerSecond = ChaserEnemyController.DefaultSpeedMetersPerSecond;
        [SerializeField] private int contactDamage = ChaserEnemyController.DefaultContactDamage;
        [SerializeField] private float contactCooldownSeconds = ChaserEnemyController.DefaultContactCooldownSeconds;
        [SerializeField] private float radiusMeters = 0.32f;
        [SerializeField] private EnemyArchetypeId archetypeId = EnemyArchetypeId.Normal;
        [SerializeField] private EnemyMovementMode movementMode = EnemyMovementMode.Grounded;

        private RoomRuntimeRoot roomRuntimeRoot;
        private PlaceholderPlayerController playerController;
        private CombatantHealth playerHealth;
        private float nextAllowedContactTime;

        public CombatantHealth Health { get; private set; }

        public EnemyDefinition Definition { get; private set; }

        public EnemyArchetypeId ArchetypeId => archetypeId;

        public EnemyMovementMode MovementMode => movementMode;

        public float SpeedMetersPerSecond => speedMetersPerSecond;

        public int ContactDamage => contactDamage;

        public float RadiusMeters => radiusMeters;

        public bool IsAlive => Health != null && Health.IsAlive;

        public void Configure(RoomRuntimeRoot room, PlaceholderPlayerController player, EnemyDefinition definition, DifficultyTierDefinition difficultyTier)
        {
            roomRuntimeRoot = room;
            playerController = player;
            playerHealth = playerController != null ? playerController.GetComponent<CombatantHealth>() : null;
            Definition = definition != null ? definition : EnemyDefinition.CreateRuntimeNormal();
            var tuning = difficultyTier != null ? difficultyTier.Tuning : DifficultyTierDefinition.CreateRuntimeDeveloperSample().Tuning;

            archetypeId = Definition.ArchetypeId;
            movementMode = Definition.MovementMode;
            speedMetersPerSecond = tuning.ApplySpeed(Definition.SpeedMetersPerSecond);
            contactDamage = tuning.ApplyContactDamage(Definition.ContactDamage);
            contactCooldownSeconds = Definition.ContactCooldownSeconds;
            radiusMeters = Definition.RadiusMeters;

            Health = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
            Health.Configure(tuning.ApplyHealth(Definition.MaxHealth));
            Health.Died += OnDied;
            ApplyVisualMaterial(RoleForArchetype(archetypeId));

            var presenter = GetComponent<CombatReadabilityPresenter>() ?? gameObject.AddComponent<CombatReadabilityPresenter>();
            presenter.Bind(this);
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float timeSeconds)
        {
            if (!IsAlive || playerController == null)
            {
                return;
            }

            var delta = playerController.transform.localPosition - transform.localPosition;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                var desired = transform.localPosition + delta.normalized * speedMetersPerSecond * deltaTime;
                transform.localPosition = movementMode == EnemyMovementMode.Flying
                    ? RoomLocalCollision.ResolveMoveIgnoringObstacles(roomRuntimeRoot, desired, radiusMeters)
                    : RoomLocalCollision.ResolveMove(roomRuntimeRoot, transform.localPosition, desired, radiusMeters);
            }

            TryApplyContactDamage(timeSeconds);
        }

        public bool TryApplyContactDamage(float timeSeconds)
        {
            if (!IsAlive || playerHealth == null || !playerHealth.IsAlive || timeSeconds < nextAllowedContactTime)
            {
                return false;
            }

            var distance = Vector3.Distance(Flat(transform.localPosition), Flat(playerController.transform.localPosition));
            if (distance > radiusMeters + PlaceholderPlayerController.DefaultRadiusMeters + 0.12f)
            {
                return false;
            }

            nextAllowedContactTime = timeSeconds + contactCooldownSeconds;
            var damaged = DamageSystem.ApplyDamage(playerHealth, new DamageRequest(contactDamage, gameObject));
            if (damaged)
            {
                VfxPresenter.Play(VfxCueId.PlayerHit, playerController.transform.position, playerController.transform.parent);
                AudioPresenter.Play(AudioCueId.PlayerHit, playerController.transform.position);
            }

            return damaged;
        }

        private void OnDied(CombatantHealth _)
        {
            VfxPresenter.Play(VfxCueId.EnemyDeath, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyDeath, transform.position);
            gameObject.SetActive(false);
        }

        private void ApplyVisualMaterial(MaterialRole role)
        {
            var renderer = GetComponentInChildren<Renderer>();
            MaterialResolver.ApplyTo(renderer, role);
        }

        private static MaterialRole RoleForArchetype(EnemyArchetypeId archetype)
        {
            return archetype switch
            {
                EnemyArchetypeId.Flying => MaterialRole.EnemyFlying,
                EnemyArchetypeId.Fast => MaterialRole.EnemyFast,
                EnemyArchetypeId.Heavy => MaterialRole.EnemyHeavy,
                _ => MaterialRole.EnemyNormal
            };
        }

        private static Vector3 Flat(Vector3 value)
        {
            return new Vector3(value.x, 0f, value.z);
        }
    }
}
