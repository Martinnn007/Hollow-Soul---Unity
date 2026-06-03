using System;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public enum RuntimeRenderResolutionMode
    {
        Native = 0,
        Balanced = 1,
        Low = 2
    }

    public static class RuntimeRenderResolutionSettings
    {
        public const string PlayerPrefsKey = "Hollow.RenderResolution.Mode";

        private const string NativeValue = "native";
        private const string BalancedValue = "balanced";
        private const string LowValue = "low";

        private static bool loaded;
        private static RuntimeRenderResolutionMode? explicitMode;

        public static bool HasExplicitMode
        {
            get
            {
                EnsureLoaded();
                return explicitMode.HasValue;
            }
        }

        public static RuntimeRenderResolutionMode CurrentMode => CurrentModeFor(RuntimeRenderProfileSettings.CurrentProfile);

        public static RuntimeRenderResolutionMode CurrentModeFor(HollowRenderProfileDefinition profile)
        {
            EnsureLoaded();
            return explicitMode ?? ModeForScale(profile != null ? profile.RenderScale : ScaleFor(RuntimeRenderResolutionMode.Native));
        }

        public static float CurrentRenderScale => ScaleFor(CurrentMode);

        public static void SetMode(RuntimeRenderResolutionMode mode, bool persist = true)
        {
            loaded = true;
            explicitMode = NormalizeMode(mode);
            if (persist)
            {
                PlayerPrefs.SetString(PlayerPrefsKey, ToPreferenceValue(explicitMode.Value));
                PlayerPrefs.Save();
            }

            ApplyCurrentResolution(RuntimeRenderProfileSettings.CurrentProfile);
        }

        public static bool ApplyCurrentResolution(HollowRenderProfileDefinition profile)
        {
            EnsureLoaded();
            if (!explicitMode.HasValue || profile == null || profile.RenderPipelineAsset == null)
            {
                return false;
            }

            RenderProfileApplier.ApplyRenderScale(profile.RenderPipelineAsset, ScaleFor(explicitMode.Value));
            return true;
        }

        public static float ScaleFor(RuntimeRenderResolutionMode mode)
        {
            switch (NormalizeMode(mode))
            {
                case RuntimeRenderResolutionMode.Low:
                    return 0.5f;
                case RuntimeRenderResolutionMode.Balanced:
                    return 0.75f;
                default:
                    return 1f;
            }
        }

        public static RuntimeRenderResolutionMode ModeForScale(float renderScale)
        {
            if (renderScale <= 0.625f)
            {
                return RuntimeRenderResolutionMode.Low;
            }

            return renderScale >= 0.875f ? RuntimeRenderResolutionMode.Native : RuntimeRenderResolutionMode.Balanced;
        }

        public static bool TryParseMode(string value, out RuntimeRenderResolutionMode mode)
        {
            if (string.Equals(value, NativeValue, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuntimeRenderResolutionMode.Native;
                return true;
            }

            if (string.Equals(value, BalancedValue, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuntimeRenderResolutionMode.Balanced;
                return true;
            }

            if (string.Equals(value, LowValue, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuntimeRenderResolutionMode.Low;
                return true;
            }

            mode = RuntimeRenderResolutionMode.Native;
            return false;
        }

        public static void ResetForTests()
        {
            loaded = false;
            explicitMode = null;
        }

        private static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            explicitMode = PlayerPrefs.HasKey(PlayerPrefsKey) &&
                           TryParseMode(PlayerPrefs.GetString(PlayerPrefsKey), out var savedMode)
                ? savedMode
                : null;
            loaded = true;
        }

        private static RuntimeRenderResolutionMode NormalizeMode(RuntimeRenderResolutionMode mode)
        {
            switch (mode)
            {
                case RuntimeRenderResolutionMode.Low:
                    return RuntimeRenderResolutionMode.Low;
                case RuntimeRenderResolutionMode.Balanced:
                    return RuntimeRenderResolutionMode.Balanced;
                default:
                    return RuntimeRenderResolutionMode.Native;
            }
        }

        private static string ToPreferenceValue(RuntimeRenderResolutionMode mode)
        {
            switch (NormalizeMode(mode))
            {
                case RuntimeRenderResolutionMode.Low:
                    return LowValue;
                case RuntimeRenderResolutionMode.Balanced:
                    return BalancedValue;
                default:
                    return NativeValue;
            }
        }
    }
}
