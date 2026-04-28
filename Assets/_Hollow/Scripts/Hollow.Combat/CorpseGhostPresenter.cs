using Hollow.Data.Definitions;
using Hollow.Presentation;
using UnityEngine;

namespace Hollow.Combat
{
    public sealed class CorpseGhostPresenter : MonoBehaviour
    {
        private float despawnAt;
        private float durationSeconds = 1.5f;
        private Renderer ghostRenderer;
        private Color baseColor = new(0.85f, 0.9f, 1f, 0.42f);

        public static CorpseGhostPresenter SpawnFrom(EnemyRuntimeController enemy, CombatFeelProfileDefinition profile)
        {
            if (enemy == null || enemy.transform.parent == null)
            {
                return null;
            }

            var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ghost.name = $"CorpseGhost.{enemy.ArchetypeId}";
            ghost.transform.SetParent(enemy.transform.parent, worldPositionStays: false);
            ghost.transform.localPosition = enemy.transform.localPosition;
            ghost.transform.localRotation = enemy.transform.localRotation;
            ghost.transform.localScale = Vector3.Scale(enemy.transform.localScale, new Vector3(1.05f, 0.18f, 1.05f));
            var collider = ghost.GetComponent<Collider>();
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

            MaterialResolver.ApplyTo(ghost, MaterialRole.CombatCorpseGhost);
            var presenter = ghost.AddComponent<CorpseGhostPresenter>();
            presenter.Configure(profile);
            VfxPresenter.Play(VfxCueId.EnemyCorpseGhost, ghost.transform.position, ghost.transform.parent);
            return presenter;
        }

        public void Configure(CombatFeelProfileDefinition profile)
        {
            durationSeconds = CombatFeelProfileDefinition.Resolve(profile).CorpseGhostSeconds;
            despawnAt = Time.time + durationSeconds;
            ghostRenderer = GetComponentInChildren<Renderer>();
            if (ghostRenderer != null && ghostRenderer.sharedMaterial != null)
            {
                baseColor = ghostRenderer.sharedMaterial.color;
            }
        }

        private void Update()
        {
            if (durationSeconds <= 0f || Time.time >= despawnAt)
            {
                DestroyGhost();
                return;
            }

            if (ghostRenderer != null && ghostRenderer.material != null)
            {
                var remaining = Mathf.Clamp01((despawnAt - Time.time) / durationSeconds);
                var color = baseColor;
                color.a = Mathf.Lerp(0f, baseColor.a, remaining);
                ghostRenderer.material.color = color;
            }
        }

        private void DestroyGhost()
        {
            if (Application.isPlaying)
            {
                Object.Destroy(gameObject);
            }
            else
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
