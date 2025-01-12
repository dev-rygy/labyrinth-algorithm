using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class AudioExtensions
    {
        /// <summary>
        /// Get the accurate current playback time of the sampled audioSource to the nearest integer (i.e the active audio source's clip)
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static int GetCurrentSampledTimeInt(AudioSource audioSource, int beatsPerMinute)
        {
            return Mathf.FloorToInt(GetCurrentSampledTime(audioSource, beatsPerMinute));
        }

        /// <summary>
        /// Get the accurate current playback time of the sampled audioSource given the clip's beatsPerMinute (i.e the active audio source's clip)
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="beatsPerMinute"></param>
        /// <returns></returns>
        public static float GetCurrentSampledTime(AudioSource audioSource, int beatsPerMinute)
        {
            float audioSourceTimeSamples = audioSource.timeSamples;
            float audioClipFrequency = audioSource.clip.frequency;
            float beatInterval = SecondsPerBeat(beatsPerMinute);

            return audioSourceTimeSamples / (audioClipFrequency * beatInterval);
        }
        
        /// <summary>
        /// Get the accurate total time of the sampled audio clip to the nearest integer (i.e the active audio source's clip)
        /// </summary>
        /// <param name="clip"></param>
        /// <returns></returns>
        public static int GetTotalSampledTimeInt(AudioClip clip, int beatsPerMinute)
        {
            return Mathf.FloorToInt(GetTotalSampledTime(clip, beatsPerMinute));
        }

        /// <summary>
        /// Get the accurate total time of the sampled audio clip given the clip's beatsPerMinute (i.e the active audio source's clip)
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="beatsPerMinute"></param>
        /// <returns></returns>
        public static float GetTotalSampledTime(AudioClip clip, int beatsPerMinute)
        {
            float audioSourceTimeSamples = clip.samples;
            float audioClipFrequency = clip.frequency;
            float beatInterval = SecondsPerBeat(beatsPerMinute);

            return audioSourceTimeSamples / (audioClipFrequency * beatInterval);
        }

        /// <summary>
        /// Get the time in seconds between each beat.
        /// The step can be used to get beat timings for non-full beats [e.g step = 0.5 for a half beat]
        /// </summary>
        /// <param name="beatsPerMinute"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static float SecondsPerBeat(float beatsPerMinute, float step = 1)
        {
            const float minute = 60f;
            return minute / (beatsPerMinute * step);
        }

    } // class end
}
