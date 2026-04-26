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
                return null;
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
    }
}
