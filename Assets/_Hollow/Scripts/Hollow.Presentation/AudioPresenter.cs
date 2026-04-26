using Hollow.Data.Definitions;
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

            var audioObject = new GameObject($"Audio.{cue}", typeof(AudioSource));
            audioObject.transform.position = position;
            var source = audioObject.GetComponent<AudioSource>();
            source.clip = definition.Clip;
            source.volume = definition.Volume;
            source.spatialBlend = definition.SpatialBlend;
            source.Play();
            if (Application.isPlaying)
            {
                Object.Destroy(audioObject, definition.Clip.length + 0.1f);
            }

            return source;
        }

        public static AudioSource PlayUi(AudioCueId cue)
        {
            return Play(cue, Vector3.zero);
        }
    }
}
