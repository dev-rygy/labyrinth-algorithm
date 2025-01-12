#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public static class EditorExtensions
    {
        /// <summary>
        /// Get all assets of a set type from the project files
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>Return all objects of a set type from the project files</returns>
        public static List<T> GetAllAssetsByType<T>() where T : Object
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }

    } // class end
}

#endif