using System;
using Hollow.Data.Definitions;
using UnityEngine;

namespace Hollow.Presentation
{
    public enum RuntimeRenderProfileMode
    {
        Cool = 0,
        Quality = 1
    }

    public static class RuntimeRenderProfileSettings
    {
        public const string PlayerPrefsKey = "Hollow.RenderProfile.Mode";
        public const string CommandLineArgumentPrefix = "--hollow-render-profile=";

        private const string CoolProfileResourcePath = "Hollow/Rendering/RenderProfile_DevCool";
        private const string QualityProfileResourcePath = "Hollow/Rendering/RenderProfile_WindowsQuality";
        private const string CoolValue = "cool";
        private const string QualityValue = "quality";

        private static RuntimeRenderProfileMode? currentMode;
        private static HollowRenderProfileDefinition coolProfile;
        private static HollowRenderProfileDefinition qualityProfile;

        public static RuntimeRenderProfileMode CurrentMode
        {
            get
            {
                EnsureCurrentModeLoaded();
                return currentMode.GetValueOrDefault(RuntimeRenderProfileMode.Cool);
            }
        }

        public static HollowRenderProfileDefinition CurrentProfile => ProfileFor(CurrentMode);

        public static HollowRenderProfileDefinition SetMode(RuntimeRenderProfileMode mode, bool persist = true)
        {
            currentMode = NormalizeMode(mode);
            if (persist)
            {
                PlayerPrefs.SetString(PlayerPrefsKey, ToPreferenceValue(currentMode.Value));
                PlayerPrefs.Save();
            }

            return ApplyCurrentProfile();
        }

        public static HollowRenderProfileDefinition ApplyCurrentProfile()
        {
            var profile = CurrentProfile;
            if (profile != null)
            {
                RenderProfileApplier.Apply(profile);
            }

            return profile;
        }

        public static HollowRenderProfileDefinition ProfileFor(RuntimeRenderProfileMode mode)
        {
            EnsureProfilesLoaded();
            return NormalizeMode(mode) == RuntimeRenderProfileMode.Quality ? qualityProfile : coolProfile;
        }

        public static bool TryParseMode(string value, out RuntimeRenderProfileMode mode)
        {
            if (string.Equals(value, CoolValue, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuntimeRenderProfileMode.Cool;
                return true;
            }

            if (string.Equals(value, QualityValue, StringComparison.OrdinalIgnoreCase))
            {
                mode = RuntimeRenderProfileMode.Quality;
                return true;
            }

            mode = RuntimeRenderProfileMode.Cool;
            return false;
        }

        public static void ResetForTests()
        {
            currentMode = null;
            coolProfile = null;
            qualityProfile = null;
        }

        private static void EnsureCurrentModeLoaded()
        {
            if (currentMode.HasValue)
            {
                return;
            }

            if (TryReadCommandLineOverride(out var commandLineMode))
            {
                currentMode = commandLineMode;
                return;
            }

            currentMode = PlayerPrefs.HasKey(PlayerPrefsKey) &&
                          TryParseMode(PlayerPrefs.GetString(PlayerPrefsKey), out var savedMode)
                ? savedMode
                : RuntimeRenderProfileMode.Cool;
        }

        private static void EnsureProfilesLoaded()
        {
            if (coolProfile == null)
            {
                coolProfile = Resources.Load<HollowRenderProfileDefinition>(CoolProfileResourcePath);
            }

            if (qualityProfile == null)
            {
                qualityProfile = Resources.Load<HollowRenderProfileDefinition>(QualityProfileResourcePath);
            }
        }

        private static bool TryReadCommandLineOverride(out RuntimeRenderProfileMode mode)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
            {
                var argument = arguments[index];
                if (!argument.StartsWith(CommandLineArgumentPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = argument.Substring(CommandLineArgumentPrefix.Length);
                return TryParseMode(value, out mode);
            }

            mode = RuntimeRenderProfileMode.Cool;
            return false;
        }

        private static RuntimeRenderProfileMode NormalizeMode(RuntimeRenderProfileMode mode)
        {
            return mode == RuntimeRenderProfileMode.Quality ? RuntimeRenderProfileMode.Quality : RuntimeRenderProfileMode.Cool;
        }

        private static string ToPreferenceValue(RuntimeRenderProfileMode mode)
        {
            return mode == RuntimeRenderProfileMode.Quality ? QualityValue : CoolValue;
        }
    }
}
