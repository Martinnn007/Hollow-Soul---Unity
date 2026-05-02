using UnityEngine;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Combat
{
    public sealed class CombatReadabilityPresenter : MonoBehaviour
    {
        private const float DamageHealthBarRevealSeconds = 2.5f;
        private const float HealthBarWidth = 0.72f;
        private const float HealthBarHeight = 0.045f;

        private EnemyRuntimeController enemy;
        private CombatantHealth health;
        private CombatFeelProfileDefinition combatFeelProfile;
        private TextMesh nameLabel;
        private TextMesh stateLabel;
        private GameObject healthBarRoot;
        private Renderer healthBarFillRenderer;
        private Renderer targetRenderer;
        private Renderer ringRenderer;
        private Renderer aimRenderer;
        private Material baseMaterial;
        private float hitFlashRemaining;
        private float healthBarRevealRemaining;

        public void Bind(EnemyRuntimeController nextEnemy)
        {
            Bind(nextEnemy, null);
        }

        public void Bind(EnemyRuntimeController nextEnemy, CombatFeelProfileDefinition profile)
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }

            enemy = nextEnemy;
            health = enemy != null ? enemy.Health : null;
            combatFeelProfile = CombatFeelProfileDefinition.Resolve(profile);
            healthBarRevealRemaining = 0f;
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                baseMaterial = targetRenderer.sharedMaterial;
            }

            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }

            BuildOverheadIdentityIfNeeded();
            BuildTelegraphsIfNeeded();
            RefreshOverheadIdentity();
            RefreshHealthBar();
            RefreshTelegraphs();
        }

        private void Update()
        {
            RefreshOverheadIdentity();
            TickHealthBarReveal(Time.deltaTime);
            RefreshTelegraphs();
            TickHitFlash(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDamaged(CombatantHealth _)
        {
            if (ShouldShowOverheadIdentity())
            {
                healthBarRevealRemaining = DamageHealthBarRevealSeconds;
                RefreshHealthBar();
            }

            hitFlashRemaining = CombatFeelProfileDefinition.Resolve(combatFeelProfile).EnemyHitFlashSeconds;
            if (targetRenderer != null)
            {
                targetRenderer.sharedMaterial = MaterialResolver.Resolve(MaterialRole.CombatHitFlash);
            }

            VfxPresenter.Play(VfxCueId.EnemyHit, transform.position, transform.parent);
            AudioPresenter.Play(AudioCueId.EnemyHit, transform.position);
        }

        private void OnDied(CombatantHealth _)
        {
            healthBarRevealRemaining = 0f;
            SetHealthBarVisible(false);
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

        private void BuildOverheadIdentityIfNeeded()
        {
            if (!ShouldShowOverheadIdentity())
            {
                SetOverheadIdentityVisible(false);
                return;
            }

            if (nameLabel == null)
            {
                var labelObject = new GameObject("EnemyNameLabel", typeof(TextMesh));
                labelObject.transform.SetParent(transform, false);
                labelObject.transform.localPosition = new Vector3(0f, 1.13f, 0f);
                labelObject.transform.localScale = Vector3.one * 0.082f;
                nameLabel = labelObject.GetComponent<TextMesh>();
                nameLabel.anchor = TextAnchor.MiddleCenter;
                nameLabel.alignment = TextAlignment.Center;
                nameLabel.fontSize = 26;
                nameLabel.color = Color.white;
            }

            if (healthBarRoot != null)
            {
                return;
            }

            healthBarRoot = new GameObject("EnemyDamageHealthBar");
            healthBarRoot.transform.SetParent(transform, false);
            healthBarRoot.transform.localPosition = new Vector3(0f, 1.01f, 0f);

            var backObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backObject.name = "EnemyDamageHealthBarBack";
            backObject.transform.SetParent(healthBarRoot.transform, false);
            backObject.transform.localPosition = Vector3.zero;
            backObject.transform.localScale = new Vector3(HealthBarWidth, HealthBarHeight, 0.055f);
            DisableCollider(backObject);
            var backRenderer = backObject.GetComponent<Renderer>();
            backRenderer.sharedMaterial = MaterialResolver.CreateRuntimeMaterial(new Color(0.09f, 0.02f, 0.02f, 0.92f));

            var fillObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fillObject.name = "EnemyDamageHealthBarFill";
            fillObject.transform.SetParent(healthBarRoot.transform, false);
            fillObject.transform.localPosition = Vector3.zero;
            fillObject.transform.localScale = new Vector3(HealthBarWidth, HealthBarHeight * 1.1f, 0.065f);
            DisableCollider(fillObject);
            healthBarFillRenderer = fillObject.GetComponent<Renderer>();
            MaterialResolver.ApplyTo(healthBarFillRenderer, MaterialRole.CombatTelegraphDanger);

            SetHealthBarVisible(false);
        }

        private void RefreshOverheadIdentity()
        {
            if (!ShouldShowOverheadIdentity())
            {
                SetOverheadIdentityVisible(false);
                return;
            }

            BuildOverheadIdentityIfNeeded();
            SetOverheadIdentityVisible(true);
            if (nameLabel == null)
            {
                return;
            }

            nameLabel.text = enemy.Definition != null && !string.IsNullOrWhiteSpace(enemy.Definition.DisplayName)
                ? enemy.Definition.DisplayName
                : enemy.name;
        }

        private void TickHealthBarReveal(float deltaTime)
        {
            if (!ShouldShowOverheadIdentity())
            {
                healthBarRevealRemaining = 0f;
                SetHealthBarVisible(false);
                return;
            }

            if (healthBarRevealRemaining > 0f)
            {
                healthBarRevealRemaining = Mathf.Max(0f, healthBarRevealRemaining - Mathf.Max(0f, deltaTime));
            }

            RefreshHealthBar();
        }

        private void RefreshHealthBar()
        {
            if (healthBarRoot == null || health == null)
            {
                return;
            }

            var visible = ShouldShowOverheadIdentity() && health.IsAlive && healthBarRevealRemaining > 0f;
            SetHealthBarVisible(visible);
            if (!visible || healthBarFillRenderer == null)
            {
                return;
            }

            var ratio = health.MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
            var fillWidth = HealthBarWidth * ratio;
            healthBarFillRenderer.transform.localScale = new Vector3(fillWidth, HealthBarHeight * 1.1f, 0.065f);
            healthBarFillRenderer.transform.localPosition = new Vector3((fillWidth - HealthBarWidth) * 0.5f, 0f, 0f);
        }

        private void SetOverheadIdentityVisible(bool visible)
        {
            if (nameLabel != null)
            {
                nameLabel.gameObject.SetActive(visible);
            }

            if (!visible)
            {
                SetHealthBarVisible(false);
            }
        }

        private void SetHealthBarVisible(bool visible)
        {
            if (healthBarRoot != null)
            {
                healthBarRoot.SetActive(visible);
            }
        }

        private bool ShouldShowOverheadIdentity()
        {
            return enemy != null &&
                   health != null &&
                   enemy.BossDefinition == null &&
                   enemy.ArchetypeId != EnemyArchetypeId.Boss;
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
            var showAim = state is EnemyReadabilityState.ChargeWindup
                or EnemyReadabilityState.Charging
                or EnemyReadabilityState.RangedWindup
                or EnemyReadabilityState.MeleeWindup
                or EnemyReadabilityState.MeleeLunge;
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
                var role = state is EnemyReadabilityState.Charging or EnemyReadabilityState.MeleeLunge
                    ? MaterialRole.CombatTelegraphDanger
                    : MaterialRole.CombatTelegraphWarning;
                MaterialResolver.ApplyTo(aimRenderer, role);
                var direction = enemy.TelegraphDirection;
                if (direction.sqrMagnitude > 0.001f)
                {
                    aimRenderer.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                var length = state switch
                {
                    EnemyReadabilityState.RangedWindup => Mathf.Max(2.5f, enemy.Definition != null ? enemy.Definition.AttackRangeMeters : 4f),
                    EnemyReadabilityState.MeleeWindup or EnemyReadabilityState.MeleeLunge => Mathf.Max(1.0f, enemy.Definition != null ? enemy.Definition.LungeTriggerRangeMeters + enemy.Definition.LungeDistanceMeters : 1.2f),
                    _ => Mathf.Max(1.8f, enemy.Definition != null ? enemy.Definition.ChargeSpeedMetersPerSecond * EnemyRuntimeController.ChargeActiveSeconds : 2f)
                };
                aimRenderer.transform.localPosition = direction.normalized * (length * 0.5f);
                aimRenderer.transform.localPosition += new Vector3(0f, 0.06f, 0f);
                var width = state switch
                {
                    EnemyReadabilityState.RangedWindup => 0.065f,
                    EnemyReadabilityState.MeleeWindup or EnemyReadabilityState.MeleeLunge => 0.22f,
                    _ => 0.16f
                };
                aimRenderer.transform.localScale = new Vector3(width, 0.035f, length);
            }

            stateLabel.text = LabelFor(state, enemy.ReadabilitySecondsRemaining(Time.time));
            stateLabel.color = state switch
            {
                EnemyReadabilityState.EntryGrace => MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphSafe),
                EnemyReadabilityState.Charging or EnemyReadabilityState.MeleeLunge or EnemyReadabilityState.BossBurstWindup => MaterialResolver.FallbackColorFor(MaterialRole.CombatTelegraphDanger),
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
                EnemyReadabilityState.MeleeWindup => "Lunge",
                EnemyReadabilityState.MeleeLunge => "Lunge!",
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
