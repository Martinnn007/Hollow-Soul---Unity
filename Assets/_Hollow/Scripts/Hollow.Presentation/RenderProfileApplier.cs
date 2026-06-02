using System;
using System.Reflection;
using Hollow.Data.Definitions;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hollow.Presentation
{
    public static class RenderProfileApplier
    {
        public static void Apply(HollowRenderProfileDefinition profile)
        {
            if (profile == null)
            {
                return;
            }

            Application.targetFrameRate = profile.TargetFrameRate;
            QualitySettings.vSyncCount = profile.VSyncCount;
            if (profile.RenderPipelineAsset != null)
            {
                QualitySettings.renderPipeline = profile.RenderPipelineAsset;
                ApplyRenderPipelineRuntimeValues(profile.RenderPipelineAsset, profile);
            }
        }

        private static void ApplyRenderPipelineRuntimeValues(RenderPipelineAsset asset, HollowRenderProfileDefinition profile)
        {
            var type = asset.GetType();
            SetMember(type, asset, "renderScale", "m_RenderScale", profile.RenderScale);
            SetMember(type, asset, "supportsHDR", "m_SupportsHDR", profile.SupportsHdr);
            SetMember(type, asset, "supportsCameraDepthTexture", "m_RequireDepthTexture", profile.RequiresDepthTexture);
            SetMember(type, asset, "supportsCameraOpaqueTexture", "m_RequireOpaqueTexture", profile.RequiresOpaqueTexture);
            SetMember(type, asset, "mainLightShadowmapResolution", "m_MainLightShadowmapResolution", profile.MainLightShadowResolution);
            SetMember(type, asset, "shadowDistance", "m_ShadowDistance", profile.ShadowDistance);
            SetMember(type, asset, "shadowCascadeCount", "m_ShadowCascadeCount", profile.ShadowCascadeCount);
            SetMember(type, asset, "supportsAdditionalLightShadows", "m_AdditionalLightShadowsSupported", profile.AdditionalLightShadows);
            SetMember(type, asset, "maxAdditionalLights", "m_AdditionalLightsPerObjectLimit", profile.MaxAdditionalLights);
            ApplyScreenSpaceAmbientOcclusion(asset, profile.ScreenSpaceAmbientOcclusion);
        }

        private static void SetMember<T>(Type type, object target, string propertyName, string fieldName, T value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                try
                {
                    property.SetValue(target, value);
                    return;
                }
                catch
                {
                    // URP serializes some knobs without public runtime setters in certain package versions.
                }
            }

            var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                return;
            }

            try
            {
                field.SetValue(target, value);
            }
            catch
            {
                // URP serializes some knobs without public runtime setters in certain package versions.
            }
        }

        private static void ApplyScreenSpaceAmbientOcclusion(RenderPipelineAsset asset, bool enabled)
        {
            var rendererDataList = GetRendererDataList(asset);
            if (rendererDataList == null)
            {
                return;
            }

            for (var rendererIndex = 0; rendererIndex < rendererDataList.Length; rendererIndex++)
            {
                var rendererData = rendererDataList.GetValue(rendererIndex);
                var features = GetRendererFeatures(rendererData);
                if (features == null)
                {
                    continue;
                }

                for (var featureIndex = 0; featureIndex < features.Length; featureIndex++)
                {
                    var feature = features.GetValue(featureIndex);
                    if (feature == null || !IsScreenSpaceAmbientOcclusionFeature(feature))
                    {
                        continue;
                    }

                    var setActive = feature.GetType().GetMethod("SetActive", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    setActive?.Invoke(feature, new object[] { enabled });
                }
            }
        }

        private static Array GetRendererDataList(RenderPipelineAsset asset)
        {
            var type = asset.GetType();
            var field = type.GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.GetValue(asset) is Array fieldValue)
            {
                return fieldValue;
            }

            var property = type.GetProperty("rendererDataList", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return CanReadReflectedValue(property) && property.GetValue(asset) is Array propertyValue ? propertyValue : null;
        }

        private static Array GetRendererFeatures(object rendererData)
        {
            if (rendererData == null)
            {
                return null;
            }

            var type = rendererData.GetType();
            var field = type.GetField("m_RendererFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var value = field != null ? field.GetValue(rendererData) : null;
            if (value is Array fieldArray)
            {
                return fieldArray;
            }

            if (value is System.Collections.ICollection fieldCollection)
            {
                var items = new object[fieldCollection.Count];
                fieldCollection.CopyTo(items, 0);
                return items;
            }

            var property = type.GetProperty("rendererFeatures", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            value = CanReadReflectedValue(property) ? property.GetValue(rendererData) : null;
            if (value is Array array)
            {
                return array;
            }

            if (value is System.Collections.ICollection collection)
            {
                var items = new object[collection.Count];
                collection.CopyTo(items, 0);
                return items;
            }

            return null;
        }

        private static bool CanReadReflectedValue(PropertyInfo property)
        {
            return property != null &&
                   property.CanRead &&
                   property.GetIndexParameters().Length == 0 &&
                   !string.Equals(property.PropertyType.Name, "ReadOnlySpan`1", StringComparison.Ordinal);
        }

        private static bool IsScreenSpaceAmbientOcclusionFeature(object feature)
        {
            var typeName = feature.GetType().Name;
            return typeName.IndexOf("ScreenSpaceAmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   typeName.IndexOf("SSAO", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   feature is UnityEngine.Object unityObject &&
                   unityObject.name.IndexOf("ScreenSpaceAmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
