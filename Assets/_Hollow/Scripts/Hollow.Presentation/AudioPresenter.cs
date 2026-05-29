using Hollow.Data.Definitions;
using Hollow.Core;
using UnityEngine;

namespace Hollow.Presentation
{
    public static class AudioPresenter
    {
        public static AudioSource Play(AudioCueId cue, Vector3 position)
        {
            var catalog = PresentationContentProvider.ActiveCatalog;
            if (catalog == null || !catalog.TryGetAudioCue(cue, out var definition) || definition == null || definition.Clip == null)
            {
                return null;
            }

            var audioObject = HollowRuntimePool.RentGenerated($"Audio.{cue}", null, () => new GameObject($"Audio.{cue}", typeof(AudioSource)));
            audioObject.transform.position = position;
            var source = audioObject.GetComponent<AudioSource>();
            source.Stop();
            source.clip = definition.Clip;
            source.volume = definition.Volume;
            source.spatialBlend = definition.SpatialBlend;
            source.Play();
            if (Application.isPlaying)
            {
                HollowRuntimePool.ReturnAfter(audioObject, definition.Clip.length + 0.1f);
            }

            return source;
        }

        public static AudioSource PlayUi(AudioCueId cue)
        {
            return Play(cue, Vector3.zero);
        }
    }
}
