#if UNITY_EDITOR
using System.IO;
using UnityEditor;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [InitializeOnLoad]
    public static class ProjectUtility
    {
        #region Constructor

        static ProjectUtility()
        {
            RecursiveTryMoveFolderToRoot("Gaskellgames");
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void RecursiveTryMoveFolderToRoot(string folderToMove, string path = "Assets")
        {
            var folderPaths = AssetDatabase.GetSubFolders(path);
            foreach (var folderPath in folderPaths)
            {
                string fldName = new DirectoryInfo(folderPath).Name;
                if (fldName == folderToMove && folderPath != "Assets/" + folderToMove)
                {
                    FileUtil.MoveFileOrDirectory(folderPath, "Assets/" + folderToMove);
                }
                else
                {
                    RecursiveTryMoveFolderToRoot(folderToMove, folderPath);
                }
            }
        }

        #endregion

    } // class end
}
#endif