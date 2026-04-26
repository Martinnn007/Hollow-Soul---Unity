using UnityEngine;
using Hollow.Data.Definitions;
using Hollow.Presentation;

namespace Hollow.Combat
{
    public sealed class CombatReadabilityPresenter : MonoBehaviour
    {
        private const float HitFlashDurationSeconds = 0.12f;

        private EnemyRuntimeController enemy;
        private CombatantHealth health;
        private TextMesh hpLabel;
        private Renderer targetRenderer;
        private Material baseMaterial;
        private float hitFlashRemaining;

        public void Bind(EnemyRuntimeController nextEnemy)
        {
            enemy = nextEnemy;
            health = enemy != null ? enemy.Health : null;
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
            RefreshLabel();
        }

        private void Update()
        {
            RefreshLabel();
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
            hitFlashRemaining = HitFlashDurationSeconds;
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
            labelObject.transform.localScale = Vector3.one * 0.12f;
            hpLabel = labelObject.GetComponent<TextMesh>();
            hpLabel.anchor = TextAnchor.MiddleCenter;
            hpLabel.alignment = TextAlignment.Center;
            hpLabel.fontSize = 32;
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
    }
}
