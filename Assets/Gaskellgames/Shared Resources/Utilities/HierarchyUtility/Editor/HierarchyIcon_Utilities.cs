#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [InitializeOnLoad]
    public class HierarchyIcon_Utilities
    {
        #region Variables

        private static readonly Texture2D icon_Comment;

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Editor Loop

        static HierarchyIcon_Utilities()
        {
            icon_Comment = AssetDatabase.LoadAssetAtPath("Assets/Gaskellgames/Shared Resources/Utilities/HierarchyUtility/Editor/Icons/Icon_Comment.png", typeof(Texture2D)) as Texture2D;
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcon_Comment;
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void DrawHierarchyIcon_Comment(int instanceID, Rect position)
        {
            HierarchyUtility.DrawHierarchyIcon<Comment>(instanceID, position, icon_Comment);
        }

        #endregion
        
    } // class end
}

#endif