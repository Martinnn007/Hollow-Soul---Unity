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
            SetProperty(type, asset, "renderScale", profile.RenderScale);
            SetProperty(type, asset, "supportsHDR", profile.SupportsHdr);
            SetProperty(type, asset, "supportsCameraDepthTexture", profile.RequiresDepthTexture);
            SetProperty(type, asset, "supportsCameraOpaqueTexture", profile.RequiresOpaqueTexture);
            SetProperty(type, asset, "shadowDistance", profile.ShadowDistance);
            SetProperty(type, asset, "shadowCascadeCount", profile.ShadowCascadeCount);
        }

        private static void SetProperty<T>(Type type, object target, string propertyName, T value)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanWrite)
            {
                return;
            }

            try
            {
                property.SetValue(target, value);
            }
            catch
            {
                // URP serializes some knobs without public runtime setters in certain package versions.
            }
        }
    }
}
