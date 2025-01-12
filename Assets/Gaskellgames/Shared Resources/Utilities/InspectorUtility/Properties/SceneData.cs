using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gaskellgames.SceneManagement
{
    [System.Serializable]
    public class SceneData : ISerializationCallbackReceiver, IComparable<SceneData>
    {
        #region ISerializationCallbackReceiver
        
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            // keep asset references up to date for use at runtime
            if (sceneAsset != null)
            {
                this.sceneName = sceneAsset.name;
                this.sceneFilePath = AssetDatabase.GetAssetOrScenePath(sceneAsset);
                this.guid = AssetDatabase.AssetPathToGUID(sceneFilePath);
                this.buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneFilePath);
            }
            else
            {
                this.sceneName = null;
                this.sceneFilePath = null;
                this.guid = null;
                this.buildIndex = -1;
            }
#endif
        }

        public void OnAfterDeserialize()
        {
            // blank
        }

        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region IComparable

        public int CompareTo(SceneData otherSceneData)
        {
            return String.Compare(otherSceneData.SceneFilePath, this.sceneFilePath, StringComparison.Ordinal);
        }
        
        public override bool Equals (object obj)
        {
            SceneData other = obj as SceneData;
            if (other == null) return false;
            return this.sceneFilePath == other.sceneFilePath;
        }
 
        public override int GetHashCode ()
        {
            return SceneFilePath.GetHashCode();
        }
        
        // define the is equal to operator
        public static bool operator == (SceneData sceneData1, SceneData sceneData2)
        {
            if (sceneData1?.SceneFilePath != null && sceneData2?.SceneFilePath != null)
            {
                return sceneData1.CompareTo(sceneData2) == 0;
            }
            else if (sceneData1?.SceneFilePath == null && sceneData2?.SceneFilePath != null)
            {
                return false;
            }
            else if (sceneData1?.SceneFilePath != null && sceneData2?.SceneFilePath == null)
            {
                return false;
            }
            else 
            {
                return true;
            }
        }
        
        // define the is not equal to operator
        public static bool operator != (SceneData sceneData1, SceneData sceneData2)
        {
            if (sceneData1?.SceneFilePath != null && sceneData2?.SceneFilePath != null)
            {
                return sceneData1.CompareTo(sceneData2) < 0 || 0 < sceneData1.CompareTo(sceneData2);
            }
            else if (sceneData1?.SceneFilePath == null && sceneData2?.SceneFilePath != null)
            {
                return true;
            }
            else if (sceneData1?.SceneFilePath != null && sceneData2?.SceneFilePath == null)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Variables

        [SerializeField]
        private int buildIndex;

        [SerializeField]
        private string sceneName;

        [SerializeField]
        private string sceneFilePath;

        [SerializeField]
        private string guid;
        
#if UNITY_EDITOR
        [SerializeField]
        private SceneAsset sceneAsset;
#endif
        
        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Getter / Setter

        /// <summary>
        /// Get the GUID for the SceneData's SceneAsset
        /// </summary>
        public string Guid
        {
            get { return guid; }
            private set { guid = value; }
        }

        /// <summary>
        /// Get the BuildIndex for the SceneData's SceneAsset
        /// </summary>
        public int BuildIndex
        {
            get { return buildIndex; }
            private set { buildIndex = value; }
        }

        /// <summary>
        /// Get the FileName for the SceneData's SceneAsset
        /// </summary>
        public string SceneName
        {
            get { return sceneName; }
            private set { sceneName = value; }
        }

        /// <summary>
        /// Get the FilePath for the SceneData's SceneAsset
        /// </summary>
        public string SceneFilePath
        {
            get { return sceneFilePath; }
            private set { sceneFilePath = value; }
        }

        /// <summary>
        /// Get the Scene for the SceneData's SceneAsset
        /// </summary>
        public Scene Scene
        {
            get { return UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sceneFilePath); }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get the SceneAsset for this SceneData [can only be accessed in Editor]
        /// </summary>
        public SceneAsset SceneAsset
        {
            get { return sceneAsset; }
            set { sceneAsset = value; }
        }
#endif

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Contructors

        /// <summary>
        /// SceneData uses a SceneAsset in the editor to cache the values of buildIndex, sceneName, sceneFilePath and guid.
        /// New SceneData can be created at runtime from: a scene, a guid, a buildIndex or a sceneFilePath.
        /// </summary>
        /// <param name="sceneAsset"></param>
        /// <param name="scene"></param>
        /// <param name="buildIndex"></param>
        /// <param name="sceneFilePath"></param>
        public SceneData()
        {
#if UNITY_EDITOR
            this.sceneAsset = null;
#endif
            this.buildIndex = -1;
            this.sceneName = null;
            this.sceneFilePath = null;
            this.guid = null;
        }
        
        public SceneData(SceneData sceneData)
        {
#if UNITY_EDITOR
            this.sceneAsset = sceneData.SceneAsset;
#endif
            this.buildIndex = sceneData.BuildIndex;
            this.sceneName = sceneData.SceneName;
            this.sceneFilePath = sceneData.SceneFilePath;
            this.guid = sceneData.Guid;
        }
        
        public SceneData(Scene scene)
        {
            if (scene.IsValid())
            {
                string filePath = scene.path;

#if UNITY_EDITOR
                this.sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(filePath);
                this.guid = AssetDatabase.AssetPathToGUID(filePath);
#endif
                this.buildIndex = SceneUtility.GetBuildIndexByScenePath(filePath);
                this.sceneName = Path.GetFileNameWithoutExtension(filePath);
                this.sceneFilePath = filePath;
            }
            else
            {
#if UNITY_EDITOR
                this.sceneAsset = null;
#endif
                this.guid = null;
                this.buildIndex = -1;
                this.sceneName = null;
                this.sceneFilePath = null;
            }
        }
        
        public SceneData(int buildIndex)
        {
            if (0 <= buildIndex && buildIndex < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings)
            {
                string filePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);

#if UNITY_EDITOR
                this.sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(filePath);
                this.guid = AssetDatabase.AssetPathToGUID(filePath);
#endif
                this.buildIndex = buildIndex;
                this.sceneName = Path.GetFileNameWithoutExtension(filePath);
                this.sceneFilePath = filePath;
            }
            else
            {
#if UNITY_EDITOR
                this.sceneAsset = null;
#endif
                this.guid = null;
                this.buildIndex = -1;
                this.sceneName = null;
                this.sceneFilePath = null;
            }
        }
        
        public SceneData(string sceneFilePath)
        {
            Scene scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(sceneFilePath);
            if (scene.IsValid())
            {
#if UNITY_EDITOR
                this.sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(sceneFilePath);
                this.guid = AssetDatabase.AssetPathToGUID(sceneFilePath);
#endif
                this.buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneFilePath);
                this.sceneName = Path.GetFileNameWithoutExtension(sceneFilePath);
                this.sceneFilePath = sceneFilePath;
            }
            else
            {
#if UNITY_EDITOR
                this.sceneAsset = null;
#endif
                this.guid = null;
                this.buildIndex = -1;
                this.sceneName = null;
                this.sceneFilePath = null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// SceneData uses a SceneAsset in the editor to cache the values of buildIndex, sceneName, sceneFilePath and guid.
        /// New SceneData can be created at runtime from: a scene, a guid, a buildIndex or a sceneFilePath.
        /// </summary>
        /// <param name="sceneAsset"></param>
        /// <param name="scene"></param>
        /// <param name="buildIndex"></param>
        /// <param name="sceneFilePath"></param>
        public SceneData(SceneAsset sceneAsset)
        {
            if (sceneAsset != null)
            {
                this.sceneAsset = sceneAsset;
                this.buildIndex = SceneUtility.GetBuildIndexByScenePath(sceneFilePath);
                this.sceneName = sceneAsset.name;
                this.sceneFilePath = AssetDatabase.GetAssetOrScenePath(sceneAsset);
                this.guid = AssetDatabase.AssetPathToGUID(sceneFilePath);
            }
            else
            {
                this.sceneAsset = null;
                this.buildIndex = -1;
                this.sceneName = null;
                this.sceneFilePath = null;
                this.guid = null;
            }
        }
#endif

        #endregion

    } // class end
}
