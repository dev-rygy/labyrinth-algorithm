#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code updated by Gaskellgames
/// </summary>

namespace Gaskellgames.FolderSystem
{
    public class MenuItemUtilityHierarchyFolders : MenuItemUtility
    {
        #region Tools Menu
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region GameObject Menu
        
        [MenuItem(FolderSystem_GameObjectMenu_Path + "/Create Folder", false, FolderSystem_GameObjectMenu_Priority)]
        private static void Gaskellgames_GameObjectMenu_HierarchyFolder(MenuCommand menuCommand)
        {
            // create a custom gameObject, register in the undo system, parent and set position relative to context
            GameObject go = SetupMenuItemInContext(menuCommand, "FOLDER");
            
            // add scripts & components
            go.AddComponent<HierarchyFolders>();
            
            // select newly created gameObject
            Selection.activeObject = go;
        }

        [MenuItem(FolderSystem_GameObjectMenu_Path + "/Create Folder Parent", true, FolderSystem_GameObjectMenu_Priority)]
        private static bool Gaskellgames_GameObjectMenu_HierarchyFolderParent()
        {
            return Selection.activeTransform;
        }
        
        [MenuItem(FolderSystem_GameObjectMenu_Path + "/Create Folder Parent", false, FolderSystem_GameObjectMenu_Priority)]
        private static void Gaskellgames_GameObjectMenu_HierarchyFolderParent(MenuCommand menuCommand)
        {
            // cache selected objects
            Object[] objects = Selection.objects;
            Transform[] transforms = Selection.transforms;
            Transform active = Selection.activeTransform;
            
            if (objects[0] == menuCommand.context)
            {
                // create a custom gameObject & register the creation in the undo system
                GameObject go = new GameObject("FOLDER");
                Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
                
                // parent and set position relative to context
                go.transform.SetParent(active.parent);
                go.transform.localPosition = Vector3.zero;

                // parent selected to newly created gameObject
                for (int i = 0; i < transforms.Length; i++)
                {
                    transforms[i].SetParent(go.transform);
                }
                
                // add scripts & components
                go.AddComponent<HierarchyFolders>();
                
                // select newly created gameObject
                Selection.activeObject = go;
            }
        }
        
        #endregion
        
    } // class end
}

#endif