using System.Collections.Generic;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public enum BiomeLightEmitterKind
    {
        Prop = 0,
        DynamicEffect = 1,
        Hero = 2
    }

    [RequireComponent(typeof(Light))]
    public sealed class BiomeLightEmitter : MonoBehaviour
    {
        private static readonly List<BiomeLightEmitter> ActiveEmitters = new();
        private static int activePropLightCount;
        private static int activeDynamicEffectLightCount;

        [SerializeField] private BiomeLightEmitterKind kind = BiomeLightEmitterKind.Prop;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float intensity = 1f;
        [SerializeField] private float range = 4f;
        [SerializeField] private int priority = 10;
        [SerializeField] private bool castsShadows;
        [SerializeField] private float lifetimeSeconds;

        private Light cachedLight;
        private float enabledTimeSeconds;

        public static int ActivePropLightCount => activePropLightCount;

        public static int ActiveDynamicEffectLightCount => activeDynamicEffectLightCount;

        public BiomeLightEmitterKind Kind => kind;

        public int Priority => priority;

        private void OnEnable()
        {
            enabledTimeSeconds = Time.time;
            cachedLight = GetComponent<Light>();
            ConfigureLight();
            if (!ActiveEmitters.Contains(this))
            {
                ActiveEmitters.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveEmitters.Remove(this);
            RecountActiveEmitters();
        }

        private void Update()
        {
            if (lifetimeSeconds > 0f && Time.time - enabledTimeSeconds >= lifetimeSeconds)
            {
                gameObject.SetActive(false);
            }
        }

        public void Configure(BiomeLightEmitterKind nextKind, Color nextColor, float nextIntensity, float nextRange, int nextPriority, bool nextCastsShadows, float nextLifetimeSeconds = 0f)
        {
            kind = nextKind;
            color = nextColor;
            intensity = Mathf.Max(0f, nextIntensity);
            range = Mathf.Max(0.1f, nextRange);
            priority = nextPriority;
            castsShadows = nextCastsShadows;
            lifetimeSeconds = Mathf.Max(0f, nextLifetimeSeconds);
            cachedLight = GetComponent<Light>();
            ConfigureLight();
        }

        public static void ApplyBudgets(BiomeLightingProfileDefinition profile)
        {
            var discoveredEmitters = Object.FindObjectsByType<BiomeLightEmitter>(FindObjectsInactive.Exclude);
            for (var index = 0; index < discoveredEmitters.Length; index++)
            {
                var emitter = discoveredEmitters[index];
                if (emitter != null && emitter.isActiveAndEnabled && !ActiveEmitters.Contains(emitter))
                {
                    emitter.cachedLight = emitter.GetComponent<Light>();
                    ActiveEmitters.Add(emitter);
                }
            }

            ActiveEmitters.RemoveAll(emitter => emitter == null || !emitter.isActiveAndEnabled);
            ActiveEmitters.Sort((left, right) => right.Priority.CompareTo(left.Priority));
            var maxProps = profile != null ? profile.MaxPropLights : 0;
            var maxEffects = profile != null ? profile.MaxDynamicEffectLights : 0;
            var maxShadowed = profile != null ? profile.MaxShadowedLocalLights : 0;
            var usedProps = 0;
            var usedEffects = 0;
            var usedShadowed = 0;

            for (var index = 0; index < ActiveEmitters.Count; index++)
            {
                var emitter = ActiveEmitters[index];
                if (emitter == null)
                {
                    continue;
                }

                emitter.cachedLight = emitter.cachedLight != null ? emitter.cachedLight : emitter.GetComponent<Light>();
                var allowed = emitter.kind switch
                {
                    BiomeLightEmitterKind.Hero => true,
                    BiomeLightEmitterKind.DynamicEffect => usedEffects++ < maxEffects,
                    _ => usedProps++ < maxProps
                };

                if (emitter.cachedLight != null)
                {
                    emitter.cachedLight.enabled = allowed;
                    if (allowed && emitter.castsShadows && usedShadowed < maxShadowed)
                    {
                        emitter.cachedLight.shadows = LightShadows.Soft;
                        usedShadowed++;
                    }
                    else if (emitter.cachedLight != null)
                    {
                        emitter.cachedLight.shadows = LightShadows.None;
                    }
                }
            }

            RecountActiveEmitters();
        }

        private void ConfigureLight()
        {
            if (cachedLight == null)
            {
                return;
            }

            cachedLight.type = LightType.Point;
            cachedLight.color = color;
            cachedLight.intensity = Mathf.Max(0f, intensity);
            cachedLight.range = Mathf.Max(0.1f, range);
            cachedLight.shadows = castsShadows ? LightShadows.Soft : LightShadows.None;
        }

        private static void RecountActiveEmitters()
        {
            activePropLightCount = 0;
            activeDynamicEffectLightCount = 0;
            for (var index = 0; index < ActiveEmitters.Count; index++)
            {
                var emitter = ActiveEmitters[index];
                if (emitter == null || !emitter.isActiveAndEnabled || emitter.cachedLight == null || !emitter.cachedLight.enabled)
                {
                    continue;
                }

                if (emitter.kind == BiomeLightEmitterKind.DynamicEffect)
                {
                    activeDynamicEffectLightCount++;
                }
                else if (emitter.kind == BiomeLightEmitterKind.Prop)
                {
                    activePropLightCount++;
                }
            }
        }
    }
}
