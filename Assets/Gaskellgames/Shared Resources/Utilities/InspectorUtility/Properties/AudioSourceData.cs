using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace Gaskellgames
{
    [System.Serializable]
    public class AudioSourceData
    {
#if UNITY_EDITOR
        [SerializeField]
        [Tooltip("Reference to an AudioSource: Used in editor only to save/load the data to/from.")]
        public AudioSource audioSource;
#endif
        
        [SerializeField, Space]
        [Tooltip("Sets whether the sound should play through an Audio Mixer first or directly through the listener.")]
        public AudioMixerGroup output;

        [SerializeField]
        [Tooltip("Mutes the sound.")]
        public bool mute;
        
        [SerializeField]
        [Tooltip("Enables or disables custom spatialization for the AudioSource.")]
        public bool spatialize;
        
        [SerializeField]
        [Tooltip("Determines if the custom spatializer is applied before or after the effect filters attached to the AudioSource. This flag only has an effect if the spatialize flag is enabled on the AudioSource.")]
        public bool spatializePostEffects;

        [SerializeField]
        [Tooltip("Bypass/ignore any applied effects on the AudioSource.")]
        public bool bypassEffects;

        [SerializeField]
        [Tooltip("Bypass/ignore any applied effects from listener.")]
        public bool bypassListenerEffects;

        [SerializeField]
        [Tooltip("Bypass/ignore any reverb zones.")]
        public bool bypassReverbZones;

        [SerializeField]
        [Tooltip("Play the sound when the component loads.")]
        public bool playOnAwake;

        [SerializeField]
        [Tooltip("Set the source to loop. If loop points are defined in the clip, these will be respected.")]
        public bool loop;

        [SerializeField, Range(0, 256, "High", "Low")]
        [Tooltip("Sets the priority of the source.Note that a sound with a larger priority value will more likely be stolen by sounds with smaller priority values.")]
        public int priority;

        [SerializeField, Range(0, 1, "", "")]
        [Tooltip("Sets the overall volume of this sound.")]
        public float volume;

        [SerializeField, Range(-3, 3, "", "")]
        [Tooltip("Sets the frequency of the sound. Use this to slow down or speed up the sound.")]
        public float pitch;

        [FormerlySerializedAs("stereoPan")]
        [SerializeField, Range(-1, 1, "Left", "Right")]
        [Tooltip("Only valid for Mono and Stereo AudioClips. Mono sounds will be panned at constant power left and right. Stereo sounds will have each left/right value faded up and down according to the specified pan value.")]
        public float panStereo;

        [SerializeField, Range(0, 1, "2D", "3D")]
        [Tooltip("Sets how much the AudioSource is treated as a 3D source. 3D sources are affected by spacial position and spread. If 3D Pan Level is 0, all spacial attenuation is ignored.")]
        public float spatialBlend;

        [SerializeField, Range(0, 1.1f, "", "")]
        [Tooltip("Sets how much of the signal this AudioSource is mixing into the global reverb associated with the zones. [0, 1] is a linear range (like volume) while [1, 1.1] lets you boost the reverb mix by 10dB.")]
        public float reverbZoneMix;

        [SerializeField, Range(0, 5, false)]
        [Tooltip("Specifies how much the pitch is changed based on the relative velocity between AudioListener and AudioSource.")]
        public float dopplerLevel;

        [SerializeField, Range(0, 360, false)]
        [Tooltip("Sets the spread of a 3D sound in speaker space.")]
        public float spread;

        [SerializeField]
        [Tooltip("Which type of rolloff curve to use.")]
        public AudioRolloffMode volumeRolloff;

        [SerializeField]
        [Tooltip("Within the minDistance, the volume will stay at the loudest possible. Outside of this minDistance it begins to attenuate.")]
        public float minDistance;

        [SerializeField]
        [Tooltip("MaxDistance is the distance a sound stops attenuating at.")]
        public float maxDistance;

        [SerializeField]
        [Tooltip("Toggle using custom curve for spatialBlend")]
        public bool customCurveSpatialBlend;
        
        [SerializeField]
        [Tooltip("Toggle using custom curve for reverbZone")]
        public bool customCurveReverbZone;
        
        [SerializeField]
        [Tooltip("Toggle using custom curve for spread")]
        public bool customCurveSpread;
        
        [SerializeField, CustomCurve(223, 050, 050, 255)]
        [Tooltip("Custom curve for volume rolloff.")]
        public AnimationCurve volumeCurve;

        [SerializeField, CustomCurve(079, 179, 079, 255)]
        [Tooltip("Custom curve for spacialBlend.")]
        public AnimationCurve spatialBlendCurve;

        [SerializeField, CustomCurve(000, 079, 223, 255)]
        [Tooltip("Custom curve for spread.")]
        public AnimationCurve spreadCurve;

        [SerializeField, CustomCurve(223, 179, 000, 255)]
        [Tooltip("Custom curve for reverbZoneMix.")]
        public AnimationCurve reverbZoneCurve;
        
        /// <summary>
        /// Create a new default AudioSourceData.
        /// </summary>
        public AudioSourceData()
        {
            ResetAudioSourceData();
        }
        
        /// <summary>
        /// Create a new AudioSourceData using the data from an AudioSource.
        /// </summary>
        /// <param name="audioSource"></param>
        public AudioSourceData(AudioSource audioSource)
        {
            SetAudioSourceDataFromAudioSource(audioSource);
        }

        public void ResetAudioSourceData()
        {
            this.output = null;
            this.mute = false;
            this.spatialize = false;
            this.spatializePostEffects = false;
            this.bypassEffects = false;
            this.bypassListenerEffects = false;
            this.bypassReverbZones = false;
            this.playOnAwake = false;
            this.loop = false;
            this.priority = 128;
            this.volume = 1;
            this.pitch = 1;
            this.panStereo = 0;
            this.spatialBlend = 1;
            this.reverbZoneMix = 1;
            this.dopplerLevel = 1;
            this.spread = 0;
            this.volumeRolloff = AudioRolloffMode.Logarithmic;
            this.minDistance = 1;
            this.maxDistance = 500;
            this.customCurveSpatialBlend = false;
            this.customCurveReverbZone = false;
            this.customCurveSpread = false;
            this.volumeCurve = AnimationCurveExtensions.Logarithmic(0, 1, 512, 0);
            this.spatialBlendCurve = AnimationCurve.Linear(0, 0, 512, 0);
            this.spreadCurve = AnimationCurve.Linear(0, 0, 512, 0);
            this.reverbZoneCurve = AnimationCurve.Linear(0, 1, 512, 1);
        }
        
        /// <summary>
        /// Set the values of this AudioSourceData instance using the data from an AudioSource.
        /// </summary>
        /// <param name="audioSource"></param>
        public void SetAudioSourceDataFromAudioSource(AudioSource audioSource)
        {
            if(audioSource == null) { return; }
            
            // setup settings
            this.output = audioSource.outputAudioMixerGroup;
            this.mute = audioSource.mute;
            this.spatialize = audioSource.spatialize;
            this.spatializePostEffects = audioSource.spatializePostEffects;
            this.bypassEffects = audioSource.bypassEffects;
            this.bypassListenerEffects = audioSource.bypassListenerEffects;
            this.bypassReverbZones = audioSource.bypassReverbZones;
            this.playOnAwake = audioSource.playOnAwake;
            this.loop = audioSource.loop;
            this.priority = audioSource.priority;
            this.volume = audioSource.volume;
            this.pitch = audioSource.pitch;
            this.panStereo = audioSource.panStereo;
            this.spatialBlend = audioSource.spatialBlend;
            this.reverbZoneMix = audioSource.reverbZoneMix;
            this.dopplerLevel = audioSource.dopplerLevel;
            this.spread = audioSource.spread;
            this.volumeRolloff = audioSource.rolloffMode;
            this.minDistance = audioSource.minDistance;
            this.maxDistance = audioSource.maxDistance;

            // set volume rolloff curve
            switch (audioSource.rolloffMode)
            {
                case AudioRolloffMode.Logarithmic:
                    this.volumeCurve = AnimationCurveExtensions.Logarithmic(minDistance, 1, maxDistance, 0);
                    break;
                case AudioRolloffMode.Linear:
                    this.volumeCurve = AnimationCurve.Linear(minDistance, 1, maxDistance, 0);
                    break;
                case AudioRolloffMode.Custom:
                    this.volumeCurve = audioSource.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
                    break;
            }
            
            // set custom curves as true, so that the curves can be copied
            this.customCurveSpatialBlend = true;
            this.spatialBlendCurve = audioSource.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
            this.customCurveReverbZone = true;
            this.reverbZoneCurve = audioSource.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix);
            this.customCurveSpread = true;
            this.spreadCurve = audioSource.GetCustomCurve(AudioSourceCurveType.Spread);
        }
        
        /// <summary>
        /// Set the values of an AudioSource using the data from this AudioSourceData instance.
        /// </summary>
        /// <param name="audioSource"></param>
        public void SetAudioSourceFromAudioSourceData(AudioSource audioSource)
        {
            if(audioSource == null) { return; }
            
            // remove audio clip
            audioSource.clip = null;
            
            // setup settings
            audioSource.outputAudioMixerGroup = this.output;
            audioSource.mute = this.mute;
            audioSource.spatialize = this.spatialize;
            audioSource.spatializePostEffects = this.spatializePostEffects;
            audioSource.bypassEffects = this.bypassEffects;
            audioSource.bypassListenerEffects = this.bypassListenerEffects;
            audioSource.bypassReverbZones = this.bypassReverbZones;
            audioSource.playOnAwake = this.playOnAwake;
            audioSource.loop = this.loop;
            audioSource.priority = this.priority;
            audioSource.volume = this.volume;
            audioSource.pitch = this.pitch;
            audioSource.panStereo = this.panStereo;
            audioSource.spatialBlend = this.spatialBlend;
            audioSource.reverbZoneMix = this.reverbZoneMix;
            audioSource.dopplerLevel = this.dopplerLevel;
            audioSource.spread = this.spread;
            audioSource.rolloffMode = this.volumeRolloff;
            audioSource.minDistance = this.minDistance;
            audioSource.maxDistance = this.maxDistance;
            
            // set custom curves
            if (this.volumeRolloff == AudioRolloffMode.Custom)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, this.volumeCurve);
            }
            if (this.customCurveSpatialBlend)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.SpatialBlend, this.spatialBlendCurve);
            }
            if (this.customCurveReverbZone)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, this.reverbZoneCurve);
            }
            if (this.customCurveSpread)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.Spread, this.spreadCurve);
            }
        }

#if UNITY_EDITOR
        public async void HandleInspectorValidation()
        {
            // add small delay so all variables are correctly set before calculating curves
            await TaskExtensions.WaitForSeconds(0.1f);
            
            // update curve only if not custom curve
            if (!customCurveSpatialBlend)
            {
                ValidateSpatialBlendCurve();
            }
            
            // update curve only if not custom curve
            if (!customCurveReverbZone)
            {
                ValidateReverbZoneCurve();
            }
            
            // update curve only if not custom curve
            if (!customCurveSpread)
            {
                ValidateSpreadCurve();
            }
            
            // update curve only if not custom curve
            if (volumeRolloff != AudioRolloffMode.Custom)
            {
                ValidateVolumeCurve();
            }
        }
        
        /// <summary>
        /// Setup the volume curve based on the currently selected volume rolloff.
        /// </summary>
        private void ValidateVolumeCurve()
        {
            switch (volumeRolloff)
            {
                case AudioRolloffMode.Logarithmic:
                    volumeCurve = AnimationCurveExtensions.Logarithmic(minDistance, 1, maxDistance, 0);
                    break;
                case AudioRolloffMode.Linear:
                    volumeCurve = AnimationCurve.Linear(minDistance, 1, maxDistance, 0);
                    break;
                case AudioRolloffMode.Custom:
                    volumeCurve = AnimationCurve.EaseInOut(0, 1, maxDistance, 0);
                    break;
            }
        }
        
        /// <summary>
        /// Setup the SpatialBlendCurve based on the current value of spacialBlend.
        /// </summary>
        private void ValidateSpatialBlendCurve()
        {
            customCurveSpatialBlend = false;
            spatialBlendCurve = AnimationCurve.Linear(0, spatialBlend, maxDistance, spatialBlend);
        }
        
        /// <summary>
        /// Setup the SpreadCurve based on the current value of spread.
        /// </summary>
        private void ValidateSpreadCurve()
        {
            customCurveSpread = false;
            float adjusted01 = spread / 360;
            spreadCurve = AnimationCurve.Linear(0, adjusted01, maxDistance, adjusted01);
        }
        
        /// <summary>
        /// Setup the ReverbZone based on the current value of reverbZoneMix.
        /// </summary>
        private void ValidateReverbZoneCurve()
        {
            customCurveReverbZone = false;
            reverbZoneCurve = AnimationCurve.Linear(0, reverbZoneMix, maxDistance, reverbZoneMix);
        }
        
        public void Editor_CopyFromAudioSource()
        {
            if (audioSource != null)
            {
                SetAudioSourceDataFromAudioSource(audioSource);
                VerboseLogs.Log($"AudioSourceData setup using AudioSource '{audioSource.gameObject.name}'");
            }
            else
            {
                VerboseLogs.Log("No audioSource reference", LogType.Warning);
            }
        }
        
        public void Editor_CopyToAudioSource()
        {
            if (audioSource != null)
            {
                SetAudioSourceFromAudioSourceData(audioSource);
                VerboseLogs.Log($"AudioSource '{audioSource.gameObject.name}' setup using AudioSourceData");
            }
            else
            {
                VerboseLogs.Log("No audioSourceB reference", LogType.Warning);
            }
        }
#endif
        
    } // class end
}
