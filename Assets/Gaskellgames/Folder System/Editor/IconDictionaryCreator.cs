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
    public class IconDictionaryCreator : AssetPostprocessor
    {
        #region Variables

        private const string assetsPath = "Gaskellgames/Folder System/Editor/Textures";
        private const string filePath = "Assets/" + assetsPath;
        internal static Dictionary<string, Texture> iconDictionary;

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region OnEvents

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (IconAssetExists(importedAssets) && IconAssetExists(deletedAssets) && IconAssetExists(movedAssets) && IconAssetExists(movedFromAssetPaths))
            {
                CreateIconDictionary();
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static bool IconAssetExists(string[] assets)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (FixStringSeparators(Path.GetDirectoryName(assets[i])) == filePath)
                {
                    return true;
                }
            }
            return false;
        }

        private static string FixStringSeparators(string path)
        {
            return path.Replace("\\", "/");
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Internal Functions

        internal static void CreateIconDictionary()
        {
            // get all custom icons
            Dictionary<string, Texture> dictionary = new Dictionary<string, Texture>();
            DirectoryInfo directory = new DirectoryInfo(Application.dataPath + "/" + assetsPath);
            FileInfo[] info = directory.GetFiles("*.png");
            for (int i = 0; i < info.Length; i++)
            {
                string path = filePath + "/" + info[i].Name;
                Texture texture = AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D)) as Texture;
                dictionary.Add(Path.GetFileNameWithoutExtension(info[i].Name),texture);
            }

            // get asset info for custom icons
            FileInfo[] fileInfo = directory.GetFiles("*.asset");
            for (int i = 0; i < fileInfo.Length; i++)
            {
                string path = filePath + "/" + fileInfo[i].Name;
                FolderIcon_SO folderIconSo = (FolderIcon_SO)AssetDatabase.LoadAssetAtPath(path, typeof(FolderIcon_SO));

                if (folderIconSo != null) 
                {
                    Texture texture = folderIconSo.icon;
                    for (int j = 0; j < folderIconSo.folderNames.Count; j++)
                    {
                        if (folderIconSo.folderNames[j] != null) 
                        {
                            dictionary.TryAdd(folderIconSo.folderNames[j], texture);
                        }
                    }
                }
            }
            
            iconDictionary = dictionary;
        }

        #endregion
        
    } // class end
}
        
#endif