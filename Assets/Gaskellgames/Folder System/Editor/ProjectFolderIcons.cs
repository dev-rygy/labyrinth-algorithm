#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code updated by Gaskellgames
/// </summary>

namespace Gaskellgames.FolderSystem
{
    [InitializeOnLoad]
    public class CustomFolderIcons
    {
        #region Constructors

        static CustomFolderIcons()
        {
            IconDictionaryCreator.CreateIconDictionary();
            EditorApplication.projectWindowItemOnGUI += DrawFolderIcon;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void DrawFolderIcon(string guid, Rect position)
        {
            // get asset path & dictionary info
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Dictionary<string,Texture> iconDictionary = IconDictionaryCreator.iconDictionary;

            // check for valid draw
            if (path == "") { return; }
            if (Event.current.type != EventType.Repaint) { return; }
            if (!iconDictionary.ContainsKey(Path.GetFileName(path))) { return; }
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory)) { return; }

            // get image position
            float positionX = position.x - 1;
            float positionY = position.y - 1;
            float positionWidth = position.height + 2;
            float positionHeight = position.height + 2;
            if (20 < position.height)
            {
                positionWidth = position.width + 2;
                positionHeight = position.width + 2;
            }
            else if (position.x < 20)
            {
                positionX = position.x + 2;
            }
            Rect imagePosition = new Rect(positionX, positionY, positionWidth, positionHeight);

            // get image texture
            Texture texture = IconDictionaryCreator.iconDictionary[Path.GetFileName(path)];
            if (texture == null) { return; }

            // draw image texture at image position
            GUI.DrawTexture(imagePosition, texture);
        }

        #endregion
        
    } // class end
}
        
#endif