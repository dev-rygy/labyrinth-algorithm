#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class GaskellgamesHub : GGEditorWindow
    {
        #region Variables

        private string assetPath = "Assets/Gaskellgames/Shared Resources/Utilities/EditorWindowUtility/Editor/Icons/";
        
        public string[] PopUp_ShowAtStartUp = new string[] {"Always", "Never"}; // TBC: "On New Version"
        public int index_ShowAtStartUp;

        public string[] PopUp_PackageBanners = new string[] {"Always", "Never"};
        public int index_PackageBanners;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Menu Item
        
        [MenuItem(MenuItemUtility.Hub_ToolsMenu_Path, false, MenuItemUtility.Hub_ToolsMenu_Priority)]
        public static void GaskellgamesWindow()
        {
            // Create new utility window from menu item
            GaskellgamesHub window = GetWindow<GaskellgamesHub>(true);
            window.titleContent = new GUIContent("Gaskellgames Hub");
            
            // Limit size of the window
            window.minSize = new Vector2(750, 500);
            window.maxSize = window.minSize;
        }

        [MenuItem(MenuItemUtility.Hub_WindowMenu_Path, false, MenuItemUtility.Hub_WindowMenu_Priority)]
        public static void WindowGaskellgames() { GaskellgamesWindow(); }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region On Events

        private void OnEnable()
        {
            InitialiseSettings();
        }

        private void OnFocus()
        {
            InitialiseSettings();
        }

        protected override void OnGUI()
        {
            // setup Gaskellgames Editor Window
            base.OnGUI();
            base.versionNumber = "Hub Version 1.0.0";
            base.includeOptions = true;
            base.versionNumberOnRight = true;
            base.copyrightYear = "2024";
            base.includeCopyright = true;
            //base.PlaceHolderPageText();
            
            // draw window content
            DrawBanner();
            DrawWelcomeMessage();
            EditorWindowUtility.LineSeparator();
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
            HandleShowAtStartUp();
            HandleShowPackageBanners();
            EditorWindowUtility.LineSeparator();
            EditorGUILayout.LabelField("Downloaded Packages:", EditorStyles.boldLabel);
            HandleDownloadedPackages();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private void DrawBanner()
        {
            float bannerWidth = Screen.width - 8;
            Texture banner = (Texture)AssetDatabase.LoadAssetAtPath(assetPath + "InspectorBanner_SettingsHub.png", typeof(Texture));
            GUILayout.Box(banner, GUILayout.ExpandWidth(false), GUILayout.Width(bannerWidth), GUILayout.Height(bannerWidth / 7.5f));
        }

        private void DrawWelcomeMessage()
        {
            GUI.enabled = false;
            float defaultHeight = EditorStyles.textField.fixedHeight;
            EditorStyles.textField.fixedHeight = 100;
            EditorGUILayout.TextArea("Thank you for installing a Gaskellgames asset, and welcome to the settings hub!\n\n" +
                                     "Any settings options you choose here will be applied to all relevant Gaskellgames packages.\n\n" +
                                     "Links to the Unity Asset Store page, Gaskellgames Discord and Gaskellgames Website are available via the 'options' dropdown\n" +
                                     "menu above. (Note: Please read through each packages documentation pdf before contacting Gaskellgames with any queries.)");
            EditorStyles.textField.fixedHeight = defaultHeight;
            GUI.enabled = true;
        }

        private void InitialiseSettings()
        {
            switch (EditorWindowUtility.settings.showPackageBanners)
            {
                case true: // always
                    index_PackageBanners = 0;
                    break;
                case false: // never
                    index_PackageBanners = 1;
                    break;
            }
            
            switch (EditorWindowUtility.settings.showHubOnStartup)
            {
                case true: // always
                    index_ShowAtStartUp = 0;
                    break;
                case false: // never
                    index_ShowAtStartUp = 1;
                    break;
            }
        }
        
        private void HandleShowPackageBanners()
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent label = new GUIContent("Show Package Banners");
            index_PackageBanners = EditorGUILayout.Popup(label, index_PackageBanners, PopUp_PackageBanners, GUILayout.Width(Screen.width * 0.4f));
            switch (index_PackageBanners)
            {
                case 0: // always
                    EditorWindowUtility.settings.showPackageBanners = true;
                    break;
                case 1: // never
                    EditorWindowUtility.settings.showPackageBanners = false;
                    break;
            }
            EditorUtility.SetDirty(EditorWindowUtility.settings);
            EditorGUILayout.EndHorizontal();
        }

        private void HandleShowAtStartUp()
        {
            EditorGUILayout.BeginHorizontal();
            GUIContent label = new GUIContent("Show Hub On Startup");
            index_ShowAtStartUp = EditorGUILayout.Popup(label, index_ShowAtStartUp, PopUp_ShowAtStartUp, GUILayout.Width(Screen.width * 0.4f));
            switch (index_ShowAtStartUp)
            {
                case 0: // always
                    EditorWindowUtility.settings.showHubOnStartup = true;
                    break;
                case 1: // never
                    EditorWindowUtility.settings.showHubOnStartup = false;
                    break;
                /*
                case 2: // on new version
                    break;
                */
            }
            EditorUtility.SetDirty(EditorWindowUtility.settings);
            EditorGUILayout.EndHorizontal();
        }

        private void HandleDownloadedPackages()
        {
            int logoSize = 75;
            EditorGUILayout.BeginHorizontal();
            
            string platformController = "Assets/Gaskellgames/Platform Controller/Editor/Icons/Logo_PlatformController.png";
            Texture logo_PlatformController = (Texture)AssetDatabase.LoadAssetAtPath(platformController, typeof(Texture));
            if(logo_PlatformController != null)
            {
                GUIContent label_PlatformController = new GUIContent(logo_PlatformController, "Platform Controller");
                GUILayout.Box(label_PlatformController, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string CameraController = "Assets/Gaskellgames/Camera Controller/Editor/Icons/Logo_CameraController.png";
            Texture logo_CameraController = (Texture)AssetDatabase.LoadAssetAtPath(CameraController, typeof(Texture));
            if(logo_CameraController != null)
            {
                GUIContent label_CameraController = new GUIContent(logo_CameraController, "Camera Controller");
                GUILayout.Box(label_CameraController, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string CharacterController = "Assets/Gaskellgames/Character Controller/Editor/Icons/Logo_CharacterController.png";
            Texture logo_CharacterController = (Texture)AssetDatabase.LoadAssetAtPath(CharacterController, typeof(Texture));
            if(logo_CharacterController != null)
            {
                GUIContent label_CharacterController = new GUIContent(logo_CharacterController, "Character Controller");
                GUILayout.Box(label_CharacterController, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string AudioController = "Assets/Gaskellgames/Audio Controller/Editor/Icons/Logo_AudioController.png";
            Texture logo_AudioController = (Texture)AssetDatabase.LoadAssetAtPath(AudioController, typeof(Texture));
            if(logo_AudioController != null)
            {
                GUIContent label_AudioController = new GUIContent(logo_AudioController, "Audio Controller");
                GUILayout.Box(label_AudioController, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string SceneController = "Assets/Gaskellgames/Scene Controller/Editor/Icons/Logo_SceneController.png";
            Texture logo_SceneController = (Texture)AssetDatabase.LoadAssetAtPath(SceneController, typeof(Texture));
            if(logo_SceneController != null)
            {
                GUIContent label_SceneController = new GUIContent(logo_SceneController, "Scene Controller");
                GUILayout.Box(label_SceneController, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string InputEventSystem = "Assets/Gaskellgames/Input Event System/Editor/Icons/Logo_InputEventSystem.png";
            Texture logo_InputEventSystem = (Texture)AssetDatabase.LoadAssetAtPath(InputEventSystem, typeof(Texture));
            if(logo_InputEventSystem != null)
            {
                GUIContent label_InputEventSystem = new GUIContent(logo_InputEventSystem, "Input Event System");
                GUILayout.Box(label_InputEventSystem, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string HierarchyFolderSystem = "Assets/Gaskellgames/Folder System/Editor/Icons/Logo_HierarchyFolderSystem.png";
            Texture logo_HierarchyFolderSystem = (Texture)AssetDatabase.LoadAssetAtPath(HierarchyFolderSystem, typeof(Texture));
            if(logo_HierarchyFolderSystem != null)
            {
                GUIContent label_HierarchyFolderSystem = new GUIContent(logo_HierarchyFolderSystem, "Hierarchy Folder System");
                GUILayout.Box(label_HierarchyFolderSystem, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            string LogicSystem = "Assets/Gaskellgames/Logic System/Editor/Icons/Logo_LogicSystem.png";
            Texture logo_LogicSystem = (Texture)AssetDatabase.LoadAssetAtPath(LogicSystem, typeof(Texture));
            if(logo_LogicSystem != null)
            {
                GUIContent label_LogicSystem = new GUIContent(logo_LogicSystem, "Logic System");
                GUILayout.Box(label_LogicSystem, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            }
            
            EditorGUILayout.EndHorizontal();
        }

        #endregion
        
    } // class end
}

#endif