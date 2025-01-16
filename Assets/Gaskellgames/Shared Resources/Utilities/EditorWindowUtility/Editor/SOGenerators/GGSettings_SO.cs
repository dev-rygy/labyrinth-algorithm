#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    //[CreateAssetMenu(fileName = "GGSettings", menuName = "Gaskellgames/Settings")]
    public class GGSettings_SO : ScriptableObject
    {
        #region Variables

        public bool showHubOnStartup = true;
        public bool showPackageBanners = true;
        public bool showGaskellgamesLogs = true;

        #endregion
        
    } // class end
}
#endif