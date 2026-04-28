using UnityEngine;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Combat
{
    public sealed class CombatReadabilityPresenter : MonoBehaviour
    {
        private EnemyRuntimeController enemy;
        private CombatantHealth health;
        private CombatFeelProfileDefinition combatFeelProfile;
        private TextMesh hpLabel;
        private TextMesh stateLabel;
        private Renderer targetRenderer;
        private Renderer ringRenderer;
        private Renderer aimRenderer;
        private Material baseMaterial;
        private float hitFlashRemaining;

        public void Bind(EnemyRuntimeController nextEnemy)
        {
            Bind(nextEnemy, null);
        }

        public void Bind(EnemyRuntimeController nextEnemy, CombatFeelProfileDefinition profile)
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }

            enemy = nextEnemy;
            health = enemy != null ? enemy.Health : null;
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                baseMaterial = targetRenderer.sharedMaterial;
            }

            if (health != null)
            {
                health.Damaged += OnDamaged;
            }

            BuildLabelIfNeeded();
            BuildTelegraphsIfNeeded();
            RefreshLabel();
            RefreshTelegraphs();
        }

        private void Update()
        {
            RefreshLabel();
            RefreshTelegraphs();
            TickHitFlash(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
            }
        }

        private void OnDamaged(CombatantHealth _)
        {
            hitFlashRemaining = CombatFeelProfileDefinition.Resolve(combatFeelProfile).EnemyHitFlashSeconds;
            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterial = MaterialResolver.Resolve(MaterialRole.CombatHitFlash);
            }

            VfxPresenter.Play(VfxCueId.EnemyHit, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyHit, transform.position);
        }

        private void TickHitFlash(float deltaTime)
        {
            if (hitFlashRemaining <= 0f || targetRenderer == null)
            {
                return;
            }

            hitFlashRemaining -= deltaTime;
            if (hitFlashRemaining <= 0f)
            {
                targetRenderer.sharedMaterial = baseMaterial != null ? baseMaterial : MaterialResolver.Resolve(MaterialRole.EnemyNormal);
            }
        }

        private void BuildLabelIfNeeded()
        {
            if (hpLabel != null)
            {
                return;
            }

            var labelObject = new GameObject("EnemyHpLabel", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.095f;
            hpLabel = labelObject.GetComponent<TextMesh>();
            hpLabel.anchor = TextAnchor.MiddleCenter;
            hpLabel.alignment = TextAlignment.Center;
            hpLabel.fontSize = 28;
            hpLabel.color = Color.white;
        }

        private void RefreshLabel()
        {
            if (hpLabel == null || health == null)
            {
                return;
            }

            hpLabel.text = $"{enemy.ArchetypeId} {health.CurrentHealth}/{health.MaxHealth}";
        }

        private void BuildTelegraphsIfNeeded()
        {
            if (ringRenderer != null && aimRenderer != null && stateLabel != null)
            {
                return;
            }

            var ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringObject.name = "EnemyReadabilityTelegraphRing";
            ringObject.transform.SetParent(transform, false);
            ringObject.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            ringObject.transform.localScale = new Vector3(1.5f, 0.012f, 1.5f);
            DisableCollider(ringObject);
            ringRenderer = ringObject.GetComponent<Renderer>();
            MaterialResolver.ApplyTo(ringRenderer, MaterialRole.CombatTelegraphWarning);

            var aimObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            aimObject.name = "EnemyReadabilityAimLine";
            aimObject.transform.SetParent(transform, false);
            aimObject.transform.localPosition = new Vector3(0f, 0.06f, 0.85f);
            aimObject.transform.localScale = new Vector3(0.1f, 0.035f, 1.7f);
            DisableCollider(aimObject);
            aimRenderer = aimObject.GetComponent<Renderer>();
            MaterialResolver.ApplyTo(aimRenderer, MaterialRole.CombatTelegraphDanger);

            var labelObject = new GameObject("EnemyReadabilityStateLabel", typeof(TextMesh));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.34f, 0f);
            labelObject.transform.localScale = Vector3.one * 0.075f;
            stateLabel = labelObject.GetComponent<TextMesh>();
            stateLabel.anchor = TextAnchor.MiddleCenter;
            stateLabel.alignment = TextAlignment.Center;
            stateLabel.fontSize = 24;
            stateLabel.color = Color.white;
        }

        private void RefreshTelegraphs()
        {
            if (enemy == null || ringRenderer == null || aimRenderer == null || stateLabel == null)
            {
                return;
            }

            var state = enemy.ReadabilityStateAt(Time.time);
            var profile = CombatFeelProfileDefinition.Resolve(combatFeelProfile);
            var showRing = state is EnemyReadabilityState.EntryGrace or EnemyReadabilityState.BossBurstWindup;
            var showAim = state is EnemyReadabilityState.ChargeWindup or EnemyReadabilityState.Charging or EnemyReadabilityState.RangedWindup;
            ringRenderer.gameObject.SetActive(showRing);
            aimRenderer.gameObject.SetActive(showAim);
            stateLabel.gameObject.SetActive(profile.ShowWindupLabels && state != EnemyReadabilityState.Idle);

            if (showRing)
            {
                var role = state == EnemyReadabilityState.EntryGrace ? MaterialRole.CombatTelegraphSafe : MaterialRole.CombatTelegraphDanger;
                MaterialResolver.ApplyTo(ringRenderer, role);
                var pulse = 1f + Mathf.Sin(Time.time * 10f) * profile.WindupPulseStrength;
                var radius = (state == EnemyReadabilityState.BossBurstWindup ? 3.4f : Mathf.Max(1.2f, enemy.RadiusMeters * 4f)) * pulse;
                ringRenderer.transform.localScale = new Vector3(radius, 0.012f, radius);
            }

            if (showAim)
            {
                var role = state == EnemyReadabilityState.Charging ? MaterialRole.CombatTelegraphDanger : MaterialRole.CombatTelegraphWarning;
                MaterialResolver.ApplyTo(aimRenderer, role);
                var direction = enemy.TelegraphDirection;
                if (direction.sqrMagnitude > 0.001f)
                {
                    aimRenderer.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                var length = state == EnemyReadabilityState.RangedWindup
                    ? Mathf.Max(2.5f, enemy.Definition != null ? enemy.Definition.AttackRangeMeters : 4f)
                    : Mathf.Max(1.8f, enemy.Definition != null ? enemy.Definition.ChargeSpeedMetersPerSecond * EnemyRuntimeController.ChargeActiveSeconds : 2f);
                aimRenderer.transform.localPosition = direction.normalized * (length * 0.5f);
                aimRenderer.transform.localPosition += new Vector3(0f, 0.06f, 0f);
                aimRenderer.transform.localScale = new Vector3(state == EnemyReadabilityState.RangedWindup ? 0.065f : 0.16f, 0.035f, length);
            }

            stateLabel.text = LabelFor(state, enemy.ReadabilitySecondsRemaining(Time.time));
            stateLabel.color = state switch
            {
                EnemyReadabilityState.EntryGrace => MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphSafe),
                EnemyReadabilityState.Charging or EnemyReadabilityState.BossBurstWindup => MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphDanger),
                _ => MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphWarning)
            };
        }

        private static string LabelFor(EnemyReadabilityState state, float secondsRemaining)
        {
            return state switch
            {
                EnemyReadabilityState.EntryGrace => "Wait",
                EnemyReadabilityState.ChargeWindup => "Charge",
                EnemyReadabilityState.Charging => "Charge!",
                EnemyReadabilityState.RangedWindup => "Shot",
                EnemyReadabilityState.BossBurstWindup => "Burst",
                _ => string.Empty
            };
        }

        private static void DisableCollider(GameObject target)
        {
            var collider = target != null ? target.GetComponent<Collider>() : null;
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }
}
