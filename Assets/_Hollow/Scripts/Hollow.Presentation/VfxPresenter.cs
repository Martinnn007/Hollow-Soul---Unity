using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class VfxPresenter
    {
        public static GameObject Play(VfxCueId cue, Vector3 position, Transform parent = null)
        {
            var catalog = PresentationContentProvider.ActiveCatalog;
            if (catalog == null || !catalog.TryGetVfxCue(cue, out var definition) || definition == null)
            {
                return PlayBuiltInFallback(cue, position, parent);
            }

            GameObject instance = null;
            if (definition.Prefab != null)
            {
                instance = Object.Instantiate(definition.Prefab, parent);
            }
            else if (definition.CreateDebugPrimitive)
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instance.transform.SetParent(parent, worldPositionStays: false);
                instance.transform.localScale = Vector3.one * definition.DebugScale;
                var renderer = instance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = MaterialResolver.CreateRuntimeMaterial(definition.DebugColor);
                }

                var collider = instance.GetComponent<Collider>();
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

            if (instance == null)
            {
                return null;
            }

            instance.name = $"VFX.{cue}";
            instance.transform.position = position;
            if (Application.isPlaying)
            {
                Object.Destroy(instance, 0.35f);
            }

            return instance;
        }

        private static GameObject PlayBuiltInFallback(VfxCueId cue, Vector3 position, Transform parent)
        {
            if (!TryGetBuiltInFallback(cue, out var color, out var scale))
            {
                return null;
            }

            var instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = $"VFX.{cue}.Fallback";
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * scale;

            var renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = MaterialResolver.CreateRuntimeMaterial(color);
            }

            var collider = instance.GetComponent<Collider>();
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

            if (Application.isPlaying)
            {
                Object.Destroy(instance, 0.35f);
            }

            return instance;
        }

        private static bool TryGetBuiltInFallback(VfxCueId cue, out Color color, out float scale)
        {
            scale = 0.14f;
            color = cue switch
            {
                VfxCueId.PlayerInvulnerable => new Color(0.35f, 0.9f, 1f, 0.72f),
                VfxCueId.KnockbackImpact => new Color(1f, 0.92f, 0.45f, 0.85f),
                VfxCueId.EnemyWindup => new Color(1f, 0.55f, 0.12f, 0.72f),
                VfxCueId.EnemyCorpseGhost => new Color(0.62f, 0.78f, 0.86f, 0.42f),
                VfxCueId.DamageBlocked => new Color(0.4f, 0.65f, 1f, 0.82f),
                _ => default
            };

            if (cue == VfxCueId.EnemyCorpseGhost)
            {
                scale = 0.22f;
            }

            return color != default;
        }
    }
}
