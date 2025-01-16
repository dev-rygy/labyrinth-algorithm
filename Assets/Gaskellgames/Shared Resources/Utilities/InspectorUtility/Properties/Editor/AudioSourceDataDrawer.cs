#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(AudioSourceData))]
    public class AudioSourceDataDrawer : PropertyDrawer
    {
        #region variables

        private SerializedProperty audioSource;
        
        private SerializedProperty output;
        private SerializedProperty mute;
        private SerializedProperty spatialize;
        private SerializedProperty spatializePostEffects;
        private SerializedProperty bypassEffects;
        private SerializedProperty bypassListenerEffects;
        private SerializedProperty bypassReverbZones;
        private SerializedProperty playOnAwake;
        private SerializedProperty loop;
        private SerializedProperty priority;
        private SerializedProperty volume;
        private SerializedProperty pitch;
        private SerializedProperty panStereo;
        private SerializedProperty spatialBlend;
        private SerializedProperty reverbZoneMix;
        
        private SerializedProperty dopplerLevel;
        private SerializedProperty spread;
        private SerializedProperty volumeRolloff;
        private SerializedProperty minDistance;
        private SerializedProperty maxDistance;

        private SerializedProperty customCurveSpatialBlend;
        private SerializedProperty customCurveSpread;
        private SerializedProperty customCurveReverbZone;
        
        private SerializedProperty volumeCurve;
        private SerializedProperty spatialBlendCurve;
        private SerializedProperty spreadCurve;
        private SerializedProperty reverbZoneCurve;
        
        private float singleLine = EditorGUIUtility.singleLineHeight;
        private bool show3D = true;
        private bool foldout = false;
        
        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region Property Height
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) - singleLine;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region OnGUI
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // open property & get reference to instance
            EditorGUI.BeginProperty(position, label, property);
            AudioSourceData audioSourceData = property.GetValue<AudioSourceData>();
            
            // get reference to SerializeFields
            audioSource = property.FindPropertyRelative("audioSource");
            
            output = property.FindPropertyRelative("output");
            mute = property.FindPropertyRelative("mute");
            spatialize = property.FindPropertyRelative("spatialize");
            spatializePostEffects = property.FindPropertyRelative("spatializePostEffects");
            bypassEffects = property.FindPropertyRelative("bypassEffects");
            bypassListenerEffects = property.FindPropertyRelative("bypassListenerEffects");
            bypassReverbZones = property.FindPropertyRelative("bypassReverbZones");
            playOnAwake = property.FindPropertyRelative("playOnAwake");
            loop = property.FindPropertyRelative("loop");
            priority = property.FindPropertyRelative("priority");
            volume = property.FindPropertyRelative("volume");
            pitch = property.FindPropertyRelative("pitch");
            panStereo = property.FindPropertyRelative("panStereo");
            spatialBlend = property.FindPropertyRelative("spatialBlend");
            reverbZoneMix = property.FindPropertyRelative("reverbZoneMix");
            
            dopplerLevel = property.FindPropertyRelative("dopplerLevel");
            spread = property.FindPropertyRelative("spread");
            volumeRolloff = property.FindPropertyRelative("volumeRolloff");
            minDistance = property.FindPropertyRelative("minDistance");
            maxDistance = property.FindPropertyRelative("maxDistance");
            
            customCurveSpatialBlend = property.FindPropertyRelative("customCurveSpatialBlend");
            customCurveSpread = property.FindPropertyRelative("customCurveSpread");
            customCurveReverbZone = property.FindPropertyRelative("customCurveReverbZone");
            
            volumeCurve = property.FindPropertyRelative("volumeCurve");
            spatialBlendCurve = property.FindPropertyRelative("spatialBlendCurve");
            spreadCurve = property.FindPropertyRelative("spreadCurve");
            reverbZoneCurve = property.FindPropertyRelative("reverbZoneCurve");
            
            // start change check for custom OnValidate
            EditorGUI.BeginChangeCheck();
            
            // draw property
            EditorGUILayout.BeginVertical("box");
            foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, label);
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (foldout)
            {
                // auto setup
                InspectorUtility.DrawInspectorLine(new Color32(045, 045, 045, 255));
                EditorGUILayout.PropertyField(audioSource);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Reset", "Reset values of the audioSourceData to default.")))
                {
                    audioSourceData?.ResetAudioSourceData();
                }
                if (GUILayout.Button(new GUIContent("Save", "Save the values of the audioSource to this audioSourceData.\n[audioSource -> audioSourceData]")))
                {
                    audioSourceData?.SetAudioSourceDataFromAudioSource(audioSourceData?.audioSource);
                }
                if (GUILayout.Button(new GUIContent("Load", "Load the values of this audioSourceData to the audioSource.\n[audioSourceData -> audioSource]")))
                {
                    audioSourceData?.SetAudioSourceFromAudioSourceData(audioSourceData?.audioSource);
                }
                EditorGUILayout.EndHorizontal();
                InspectorUtility.DrawInspectorLine(new Color32(045, 045, 045, 255));
                // audio source replica
                EditorGUILayout.PropertyField(output);
                EditorGUILayout.PropertyField(mute);
                EditorGUILayout.PropertyField(spatialize);
                GUI.enabled = spatialize.boolValue;
                EditorGUILayout.PropertyField(spatializePostEffects);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(bypassEffects);
                GUI.enabled = output.objectReferenceValue == null;
                EditorGUILayout.PropertyField(bypassListenerEffects);
                GUI.enabled = true;
                EditorGUILayout.PropertyField(bypassReverbZones);
                EditorGUILayout.PropertyField(playOnAwake);
                EditorGUILayout.PropertyField(loop);
                EditorGUILayout.PropertyField(priority);
                EditorGUILayout.PropertyField(volume);
                EditorGUILayout.PropertyField(pitch);
                EditorGUILayout.PropertyField(panStereo);
                if (customCurveSpatialBlend.boolValue)
                {
                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(ObjectNames.NicifyVariableName(spatialBlend.name));
                    EditorGUILayout.LabelField("Controlled by curve");
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                    GUILayout.Space((int)(singleLine * 0.5f));
                }
                else
                {
                    EditorGUILayout.PropertyField(spatialBlend);
                }
                if (customCurveReverbZone.boolValue)
                {
                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(ObjectNames.NicifyVariableName(reverbZoneMix.name));
                    EditorGUILayout.LabelField("Controlled by curve");
                    EditorGUILayout.EndHorizontal();
                    GUI.enabled = true;
                    GUILayout.Space((int)(singleLine * 0.5f));
                }
                else
                {
                    EditorGUILayout.PropertyField(reverbZoneMix);
                }
                show3D = EditorGUILayout.BeginFoldoutHeaderGroup(show3D, new GUIContent("3D Sound Settings"));
                if (show3D)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(dopplerLevel);
                    if (customCurveSpread.boolValue)
                    {
                        GUI.enabled = false;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(ObjectNames.NicifyVariableName(spread.name));
                        EditorGUI.indentLevel--;
                        EditorGUILayout.LabelField("Controlled by curve");
                        EditorGUI.indentLevel++;
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                        GUILayout.Space((int)(singleLine * 0.15f));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(spread);
                    }
                    EditorGUILayout.PropertyField(volumeRolloff);
                    if (volumeRolloff.intValue == 2)
                    {
                        GUI.enabled = false;
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.PrefixLabel(ObjectNames.NicifyVariableName(minDistance.name));
                        EditorGUI.indentLevel--;
                        EditorGUILayout.LabelField("Controlled by curve");
                        EditorGUI.indentLevel++;
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(minDistance);
                    }
                    EditorGUILayout.PropertyField(maxDistance);
                    EditorGUILayout.Space();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Custom Curves"));
                    EditorGUI.indentLevel--;
                    customCurveSpatialBlend.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("SB", "SpatialBlend"), customCurveSpatialBlend.boolValue, GUILayout.Width(40));
                    customCurveSpread.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("S", "Spread"), customCurveSpread.boolValue, GUILayout.Width(32));
                    customCurveReverbZone.boolValue = EditorGUILayout.ToggleLeft(new GUIContent("RZ", "ReverbZone"), customCurveReverbZone.boolValue, GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUI.indentLevel++;
                    GUI.enabled = volumeRolloff.intValue == 2;
                    EditorGUILayout.PropertyField(volumeCurve);
                    GUI.enabled = customCurveSpatialBlend.boolValue;
                    EditorGUILayout.PropertyField(spatialBlendCurve);
                    GUI.enabled = customCurveSpread.boolValue;
                    EditorGUILayout.PropertyField(spreadCurve);
                    GUI.enabled = customCurveReverbZone.boolValue;
                    EditorGUILayout.PropertyField(reverbZoneCurve);
                    GUI.enabled = true;
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.EndFoldoutHeaderGroup();
                
                // end change check for custom OnValidate 
                if (EditorGUI.EndChangeCheck())
                {
                    audioSourceData?.HandleInspectorValidation();
                }
            }
            EditorGUILayout.EndVertical();
            
            // close property
            EditorGUI.EndProperty();
        }

        #endregion
        
    } // class end
        
}
#endif
