#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class GGEditorWindow : EditorWindow
    {
        #region Variables
        
        private readonly float menuHeight = 21.0f;
        private readonly float shadowBuffer = 2.0f;

        protected string versionNumber = "Version 1.0.0";
        protected bool versionNumberOnRight = true;
        protected bool includeOptions = true;
        protected bool includeCopyright = true;
        protected string copyrightYear = "2022";

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region verboseLogs

        protected bool verboseLogs = true;

        /// <summary>
        /// If verbose logs is true: info message logs will be displayed in the console alongside warning and error message logs.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="type">Type of log to show in the console.</param>
        protected void Log(object message, LogType type = LogType.Log)
        {
            if (type == LogType.Log && !verboseLogs) return;
            VerboseLogs.Log(message, this, type);
        }

        /// <summary>
        /// If verbose logs is true: info message logs will be displayed in the console alongside warning and error message logs.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="type">Type of log to show in the console.</param>
        /// <param name="messageColor">Color of the message to display in the console</param>
        protected void Log(object message, LogType type, Color32 messageColor)
        {
            if (type == LogType.Log && !verboseLogs) return;
            VerboseLogs.Log(message, this, type, messageColor);
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Menu Item [Implement in child]
        
        /*
        [MenuItem("Gaskellgames/GGEditorWindow", false, 1)]
        public static void ShowWindow()
        {
            // Create new window from menu item
            GGEditorWindow window = GetWindow<GGEditorWindow>();
            window.titleContent = new GUIContent("GGEditorWindow");
            
            // Create new utility window from menu item
            GGEditorWindow window = GetWindow<GGEditorWindow>(true);
            window.titleContent = new GUIContent("GGEditorWindow");
            
            // Limit size of the window
            window.minSize = new Vector2(750, 500);
            window.maxSize = window.minSize;
        }
        */
        
        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Editor Loop

        protected virtual void OnGUI()
        {
            OnGUI_Toolbar();
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Toolbar Tree

        private void OnGUI_Toolbar()
        {
            // draw page background
            EditorGUI.DrawRect(new Rect(0, menuHeight, Screen.width, Screen.height - menuHeight), InspectorUtility.backgroundNormalColorDark);
            EditorGUI.DrawRect(new Rect(0, menuHeight, Screen.width, shadowBuffer), InspectorUtility.backgroundShadowColor);
            
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawToolbar();
            GUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            // cache variables
            int pixelHeight = 16;
            int dropdownOffset = 5;
            int dropDownWidth = 65;
            
            // left toolbar
            if(includeCopyright)
            {
                GUI.enabled = false;
                if (GUILayout.Button("Copyright \u00a9" + copyrightYear + " Gaskellgames. All rights reserved.", EditorStyles.toolbarButton)) { }
                GUI.enabled = true;
            }
            if(!versionNumberOnRight)
            {
                GUI.enabled = false;
                if (GUILayout.Button(versionNumber, EditorStyles.toolbarButton)) { }
                GUI.enabled = true;
            }

            GUILayout.FlexibleSpace();

            // right toolbar
            if(versionNumberOnRight)
            {
                GUI.enabled = false;
                if (GUILayout.Button(versionNumber, EditorStyles.toolbarButton)) { }
                GUI.enabled = true;
            }
            if (includeOptions)
            {
                if (GUILayout.Button("Options", EditorStyles.toolbarDropDown, GUILayout.Width(dropDownWidth)))
                {
                    // create new dropdown menu
                    GenericMenu toolsMenu = new GenericMenu();
                    toolsMenu.AddItem(new GUIContent("Unity Asset Store"), false, OnSupport_AssetStoreLink);
                    toolsMenu.AddSeparator("");
                    toolsMenu.AddItem(new GUIContent("Gaskellgames Discord"), false, OnSupport_DiscordLink);
                    toolsMenu.AddItem(new GUIContent("Gaskellgames Website"), false, OnSupport_WebsiteLink);
                
                    // offset menu from right of editor window
                    toolsMenu.DropDown(new Rect(Screen.width - dropDownWidth, dropdownOffset, 0, pixelHeight));
                    EditorGUIUtility.ExitGUI();
                }
            }
        }

        private void OnSupport_AssetStoreLink()
        {
            Help.BrowseURL("https://assetstore.unity.com/publishers/75563");
        }

        private void OnSupport_DiscordLink()
        {
            Help.BrowseURL("https://discord.gg/nzRQ87GGbD");
        }

        private void OnSupport_WebsiteLink()
        {
            Help.BrowseURL("https://gaskellgames.com");
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Protected Functions

        protected void PlaceHolderPageText(string pageName = "")
        {
            // set style
            GUIStyle myStyle = new GUIStyle();
            myStyle.alignment = TextAnchor.MiddleCenter;
            myStyle.normal.textColor = Color.grey;
            
            // draw placeholder window
            if (pageName == "") { EditorGUILayout.LabelField("Window is empty", myStyle); }
            else { EditorGUILayout.LabelField(pageName + " is empty", myStyle); }
        }

        #endregion
        
    } // class end
}

#endif