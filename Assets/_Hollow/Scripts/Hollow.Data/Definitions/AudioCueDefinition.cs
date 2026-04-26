using UnityEngine;

namespace Hollow.Data.Definitions
{
    [CreateAssetMenu(menuName = "Hollow/Presentation/Audio Cue", fileName = "AudioCue")]
    public sealed class AudioCueDefinition : HollowDefinition
    {
        [SerializeField] private AudioCueId cueId;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 0.65f;

        public AudioCueId CueId => cueId;

        public AudioClip Clip => clip;

        public float Volume => volume;

        public float SpatialBlend => spatialBlend;

        public void Configure(AudioCueId nextCueId, AudioClip nextClip, float nextVolume, float nextSpatialBlend)
        {
            cueId = nextCueId;
            clip = nextClip;
            volume = Mathf.Clamp01(nextVolume);
            spatialBlend = Mathf.Clamp01(nextSpatialBlend);
        }
    }
}
