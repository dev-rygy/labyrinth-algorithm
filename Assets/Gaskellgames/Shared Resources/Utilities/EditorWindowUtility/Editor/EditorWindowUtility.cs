#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [InitializeOnLoad]
    public static class EditorWindowUtility
    {
        #region Variables

        private static string filePath = "Assets/Gaskellgames/Shared Resources/Utilities/EditorWindowUtility/Data/";
        private static string asset = "GaskellgamesHubSettings.asset";
        
        public static GGSettings_SO settings;

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Constructor

        static EditorWindowUtility()
        {
            Initialisation();
            EditorApplication.update += RunOnceOnStartup;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void Initialisation()
        {
            string completePath;
            
            if (Directory.Exists(filePath))
            {
                completePath = filePath + asset;
                settings = AssetDatabase.LoadAssetAtPath<GGSettings_SO>(completePath);
            }
            else
            {
                string[] results = AssetDatabase.FindAssets("GaskellgamesHubSettings");
                foreach (string guid in results)
                {
                    completePath = AssetDatabase.GUIDToAssetPath(guid);
                    settings = AssetDatabase.LoadAssetAtPath<GGSettings_SO>(completePath);
                }
            }
        }
        
        private static void RunOnceOnStartup()
        {
            if (!settings)
            {
                Initialisation();
            }
            
            if(settings.showHubOnStartup && !SessionState.GetBool("EditorWindowUtilityFirstInit", false))
            {
                GaskellgamesHub.GaskellgamesWindow();
                SessionState.SetBool("EditorWindowUtilityFirstInit", true);
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Public Functions

        public static void LineSeparator(bool includeSpace = true)
        {
            if(includeSpace) { EditorGUILayout.Space(); }
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color32(079,079,079, 255));
            if(includeSpace) { EditorGUILayout.Space(); }
        }

        public static void DrawInspectorBanner(string textureFilepath)
        {
            if (settings != null)
            {
                if (settings.showPackageBanners)
                {
                    Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(textureFilepath, typeof(Texture));
                    GUILayout.Box(banner, GUILayout.ExpandWidth(true), GUILayout.Height(Screen.width / 7.5f));
                }
            }
        }

        #endregion
        
        
    } // class end
}

#endif