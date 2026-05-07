using System.Collections.Generic;
using Hollow.Data.Definitions;
using Hollow.Presentation;
using Hollow.Rooms;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class PlayerDamageFeedbackController : MonoBehaviour, IIncomingDamageModifier
    {
        [SerializeField] private RoomRuntimeRoot roomRuntimeRoot;
        [SerializeField] private CombatFeelProfileDefinition combatFeelProfile;

        private readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        private CombatantHealth health;
        private PlayerWeaponController weaponController;
        private float invulnerableUntil;
        private float flashUntil;
        private float nextBlockedCueTime;

        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public void Configure(RoomRuntimeRoot room, CombatFeelProfileDefinition profile)
        {
            roomRuntimeRoot = room;
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            health = GetComponent<CombatantHealth>() ?? gameObject.AddComponent<CombatantHealth>();
            health.Damaged -= OnDamaged;
            health.Damaged += OnDamaged;
            var knockback = GetComponent<CombatKnockbackReceiver>() ?? gameObject.AddComponent<CombatKnockbackReceiver>();
            knockback.Configure(roomRuntimeRoot, Hollow.Entities.PlaceholderPlayerController.DefaultRadiusMeters, false, 1f);
        }

        private void OnDestroy()
        {
            if (originalMaterials.Count > 0)
            {
                ApplyFlash(false);
            }

            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }
        }

        public int ModifyIncomingDamage(DamageRequest request, int currentAmount)
        {
            if (currentAmount <= 0)
            {
                return 0;
            }

            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController>();
            }

            if (!IsInvulnerable && (weaponController == null || !weaponController.IsRollInvulnerable))
            {
                return currentAmount;
            }

            if (Time.time >= nextBlockedCueTime)
            {
                nextBlockedCueTime = Time.time + 0.2f;
                VfxPresenter.Play(VfxCueId.DamageBlocked, transform.position, transform.parent);
                AudioPresenter.Play(AudioCueId.DamageBlocked, transform.position);
            }

            return 0;
        }

        private void OnDamaged(CombatantHealth _)
        {
            var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            invulnerableUntil = Time.time + profile.PlayerInvulnerabilitySeconds;
            flashUntil = Time.time + profile.PlayerFlashSeconds;
            ApplyFlash(true);
            VfxPresenter.Play(VfxCueId.PlayerInvulnerable, transform.position, transform.parent);
        }

        private void Update()
        {
            if (originalMaterials.Count > 0 && Time.time >= flashUntil)
            {
                ApplyFlash(false);
            }
        }

        private void ApplyFlash(bool enabled)
        {
            if (enabled)
            {
                originalMaterials.Clear();
                var flashMaterial = MaterialResolver.Resolve(MaterialRole.CombatHitFlash);
                foreach (var renderer in GetComponentsInChildren<Renderer>(includeInactive: true))
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    var materials = renderer.sharedMaterials;
                    originalMaterials[renderer] = materials;
                    if (materials == null || materials.Length == 0)
                    {
                        renderer.sharedMaterials = new[] { flashMaterial };
                        continue;
                    }

                    var flashMaterials = new Material[materials.Length];
                    for (var index = 0; index < flashMaterials.Length; index++)
                    {
                        flashMaterials[index] = flashMaterial;
                    }

                    renderer.sharedMaterials = flashMaterials;
                }

                return;
            }

            foreach (var pair in originalMaterials)
            {
                if (pair.Key != null)
                {
                    pair.Key.sharedMaterials = pair.Value;
                }
            }

            originalMaterials.Clear();
        }
    }
}
