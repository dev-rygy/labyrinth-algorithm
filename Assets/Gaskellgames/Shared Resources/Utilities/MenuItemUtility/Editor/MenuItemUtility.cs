#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class MenuItemUtility
    {
        #region Menu Items
        
        private const string ToolsMenu = "Tools/Gaskellgames";
        private const string WindowMenu = "Window/Gaskellgames";
        private const string GameObjectMenu = "GameObject/Gaskellgames";
        
        // Gaskellgames Hub
        private const string Hub = "/Gaskellgames Hub";
        
        public const string Hub_ToolsMenu_Path = ToolsMenu + Hub;
        public const int Hub_ToolsMenu_Priority = 100;
        
        public const string Hub_WindowMenu_Path = WindowMenu + Hub;
        public const int Hub_WindowMenu_Priority = 100;
        
        // Audio Controller
        public const string AudioController_WindowMenu_Path = WindowMenu;
        public const int AudioController_WindowMenu_Priority = 120;
        
        public const string AudioController_GameObjectMenu_Path = GameObjectMenu + "/Audio Controller";
        public const int AudioController_GameObjectMenu_Priority = 10;
        
        // Camera Controller
        public const string CameraController_GameObjectMenu_Path = GameObjectMenu + "/Camera Controller";
        public const int CameraController_GameObjectMenu_Priority = 10;
        
        // Character Controller
        public const string CharacterController_GameObjectMenu_Path = GameObjectMenu + "/Character Controller";
        public const int CharacterController_GameObjectMenu_Priority = 10;
        
        // Folder System
        public const string FolderSystem_GameObjectMenu_Path = "GameObject";
        public const int FolderSystem_GameObjectMenu_Priority = 0;
        
        // Input Event System
        public const string InputEventSystem_GameObjectMenu_Path = GameObjectMenu + "/Input Event System";
        public const int InputEventSystem_GameObjectMenu_Priority = 10;
        
        // Logic System
        public const string LogicSystem_GameObjectMenu_Path = GameObjectMenu + "/Logic System";
        public const int LogicSystem_GameObjectMenu_Priority = 10;
        
        // Platform Controller
        public const string PlatformController_GameObjectMenu_Path = GameObjectMenu + "/Platform Controller";
        public const int PlatformController_GameObjectMenu_Priority = 10;
        
        // Scene Controller
        public const string SceneController_WindowMenu_Path = WindowMenu;
        public const int SceneController_WindowMenu_Priority = 120;
        
        public const string SceneController_GameObjectMenu_Path = GameObjectMenu + "/Scene Controller";
        public const int SceneController_GameObjectMenu_Priority = 10;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Helper Functions

        public static GameObject SetupMenuItemInContext(MenuCommand menuCommand, string gameObjectName)
        {
            // create a custom gameObject, register in the undo system, parent and set position relative to context
            GameObject go = new GameObject(gameObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            GameObject context = (GameObject)menuCommand.context;
            if(context != null) { go.transform.SetParent(context.transform); }
            go.transform.localPosition = Vector3.zero;

            VerboseLogs.Log($"'{gameObjectName}' created.");
            
            return go;
        }

        #endregion
        
    } // class end
}
#endif