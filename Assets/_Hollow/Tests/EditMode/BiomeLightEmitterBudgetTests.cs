using Hollow.Data.Definitions;
using Hollow.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hollow.Tests.EditMode
{
    public sealed class BiomeLightEmitterBudgetTests
    {
        [Test]
        public void ApplyBudgetsKeepsHighestPriorityEmittersWithinCaps()
        {
            var profile = CreateProfile(maxProps: 2, maxEffects: 1, maxShadowed: 1);
            var lowProp = CreateEmitter("low_prop", BiomeLightEmitterKind.Prop, 10, true);
            var highProp = CreateEmitter("high_prop", BiomeLightEmitterKind.Prop, 100, true);
            var midProp = CreateEmitter("mid_prop", BiomeLightEmitterKind.Prop, 50, false);
            var lowEffect = CreateEmitter("low_effect", BiomeLightEmitterKind.DynamicEffect, 15, true);
            var highEffect = CreateEmitter("high_effect", BiomeLightEmitterKind.DynamicEffect, 80, true);
            var hero = CreateEmitter("hero", BiomeLightEmitterKind.Hero, 0, true);

            try
            {
                BiomeLightEmitter.ApplyBudgets(profile);

                Assert.IsFalse(LightOf(lowProp).enabled);
                Assert.IsTrue(LightOf(highProp).enabled);
                Assert.IsTrue(LightOf(midProp).enabled);
                Assert.IsFalse(LightOf(lowEffect).enabled);
                Assert.IsTrue(LightOf(highEffect).enabled);
                Assert.IsTrue(LightOf(hero).enabled);
                Assert.AreEqual(2, BiomeLightEmitter.ActivePropLightCount);
                Assert.AreEqual(1, BiomeLightEmitter.ActiveDynamicEffectLightCount);
                Assert.LessOrEqual(CountShadowed(lowProp, highProp, midProp, lowEffect, highEffect, hero), 1);
            }
            finally
            {
                Destroy(profile, lowProp, highProp, midProp, lowEffect, highEffect, hero);
            }
        }

        [Test]
        public void ConfigureResetsPooledEmitterState()
        {
            var profile = CreateProfile(maxProps: 1, maxEffects: 1, maxShadowed: 1);
            var emitter = CreateEmitter("pooled", BiomeLightEmitterKind.Prop, 5, true);

            try
            {
                emitter.Configure(BiomeLightEmitterKind.DynamicEffect, Color.cyan, 2.5f, 7f, 77, false);
                BiomeLightEmitter.ApplyBudgets(profile);

                var light = LightOf(emitter);

                Assert.AreEqual(BiomeLightEmitterKind.DynamicEffect, emitter.Kind);
                Assert.AreEqual(77, emitter.Priority);
                Assert.IsTrue(light.enabled);
                Assert.AreEqual(Color.cyan, light.color);
                Assert.AreEqual(2.5f, light.intensity, 0.001f);
                Assert.AreEqual(7f, light.range, 0.001f);
                Assert.AreEqual(LightShadows.None, light.shadows);
                Assert.AreEqual(0, BiomeLightEmitter.ActivePropLightCount);
                Assert.AreEqual(1, BiomeLightEmitter.ActiveDynamicEffectLightCount);
            }
            finally
            {
                Destroy(profile, emitter);
            }
        }

        private static BiomeLightingProfileDefinition CreateProfile(int maxProps, int maxEffects, int maxShadowed)
        {
            var profile = ScriptableObject.CreateInstance<BiomeLightingProfileDefinition>();
            profile.Configure(
                "test_budget",
                RoomBiomeIds.HollowThreshold,
                Color.black,
                Color.gray,
                Color.gray,
                Color.black,
                1f,
                false,
                Color.black,
                0f,
                Color.white,
                new Vector3(55f, -35f, 0f),
                1f,
                Color.white,
                Color.white,
                Color.white,
                Color.white,
                8,
                maxShadowed,
                maxProps,
                maxEffects);
            return profile;
        }

        private static BiomeLightEmitter CreateEmitter(string name, BiomeLightEmitterKind kind, int priority, bool castsShadows)
        {
            var gameObject = new GameObject(name);
            var emitter = gameObject.AddComponent<BiomeLightEmitter>();
            emitter.Configure(kind, Color.white, 1f, 4f, priority, castsShadows);
            return emitter;
        }

        private static Light LightOf(BiomeLightEmitter emitter)
        {
            return emitter.GetComponent<Light>();
        }

        private static int CountShadowed(params BiomeLightEmitter[] emitters)
        {
            var count = 0;
            for (var index = 0; index < emitters.Length; index++)
            {
                var light = LightOf(emitters[index]);
                if (light != null && light.enabled && light.shadows != LightShadows.None)
                {
                    count++;
                }
            }

            return count;
        }

        private static void Destroy(params Object[] objects)
        {
            for (var index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null)
                {
                    Object.DestroyImmediate(objects[index] is Component component ? component.gameObject : objects[index]);
                }
            }

            BiomeLightEmitter.ApplyBudgets(null);
        }
    }
}
