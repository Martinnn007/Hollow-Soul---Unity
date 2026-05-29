using System;
using UnityEngine;

namespace Hollow.Core.App
{
    [CreateAssetMenu(menuName = "Hollow/Performance/Shader Warmup Profile", fileName = "HollowShaderWarmupProfile")]
    public sealed class HollowShaderWarmupProfile : ScriptableObject
    {
        [SerializeField] private string profileName = "Boot Shader Warmup";
        [SerializeField] private bool enabledForBoot = true;
        [SerializeField] private string targetRenderProfileLabel = "Boot/Global";
        [SerializeField] private ShaderVariantCollection[] collections = Array.Empty<ShaderVariantCollection>();
        [SerializeField] private float maxExpectedWarmupMilliseconds = 100f;
        [TextArea]
        [SerializeField] private string notes = "Curated shader variants warmed during boot. Do not replace this with blanket shader warmup.";

        public string ProfileName => string.IsNullOrWhiteSpace(profileName) ? name : profileName;

        public bool EnabledForBoot => enabledForBoot;

        public string TargetRenderProfileLabel => targetRenderProfileLabel ?? string.Empty;

        public ShaderVariantCollection[] Collections => collections ?? Array.Empty<ShaderVariantCollection>();

        public float MaxExpectedWarmupMilliseconds => Mathf.Max(0f, maxExpectedWarmupMilliseconds);

        public string Notes => notes ?? string.Empty;

        public int CollectionCount
        {
            get
            {
                var count = 0;
                foreach (var collection in Collections)
                {
                    if (collection != null)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasCollections => CollectionCount > 0;

        public void Configure(
            string nextProfileName,
            bool nextEnabledForBoot,
            string nextTargetRenderProfileLabel,
            ShaderVariantCollection[] nextCollections,
            float nextMaxExpectedWarmupMilliseconds,
            string nextNotes)
        {
            profileName = string.IsNullOrWhiteSpace(nextProfileName) ? profileName : nextProfileName;
            enabledForBoot = nextEnabledForBoot;
            targetRenderProfileLabel = nextTargetRenderProfileLabel ?? string.Empty;
            collections = nextCollections ?? Array.Empty<ShaderVariantCollection>();
            maxExpectedWarmupMilliseconds = Mathf.Max(0f, nextMaxExpectedWarmupMilliseconds);
            notes = nextNotes ?? string.Empty;
        }
    }
}
