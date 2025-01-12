#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public class GGEditor : Editor
    {
        #region verboseLogs

        [Tooltip("If verbose logs is true: info message logs will be displayed in the console alongside warning and error message logs for this script instance.")]
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

    } // class end
}

#endif