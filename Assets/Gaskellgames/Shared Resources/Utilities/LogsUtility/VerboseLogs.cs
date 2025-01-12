using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class VerboseLogs
    {
        #region Variables
        
        /// <summary>
        /// Set to false to disable logs in the editor for any debug message that used Gaskellgames verboseLogs
        /// </summary>
        private static readonly bool logsEnabled = true;

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        /// <summary>
        /// Format a string with color tags ready to be used in messages in the Unity Console.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="logColor"></param>
        /// <returns></returns>
        private static string GetColoredMessage(string message, Color32 logColor)
        {
            string hexColor = $"#{logColor.r:X2}{logColor.g:X2}{logColor.b:X2}";
            string coloredMessage = $"<color={hexColor}>{message}</color>";

            return coloredMessage;
        }
        
        /// <summary>
        /// Logs a message to the Unity Console.
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="type">Type of log to show in the console.</param>
        public static void Log(object message, LogType type = LogType.Log)
        {
            Log(message, null, type);
        }
        
        /// <summary>
        /// Logs a message to the Unity Console. 
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="type">Type of log to show in the console.</param>
        public static void Log(object message, Object context, LogType type = LogType.Log)
        {
            if(!logsEnabled) {return;}
            Debug.unityLogger.Log(type, message, context);
        }
        
        
        /// <summary>
        /// Logs a message to the Unity Console. 
        /// </summary>
        /// <param name="message">String or object to be converted to string representation for display.</param>
        /// <param name="context">Object to which the message applies.</param>
        /// <param name="type">Type of log to show in the console.</param>
        /// <param name="messageColor">Color of the message to display in the console</param>
        public static void Log(object message, Object context, LogType type, Color32 messageColor)
        {
            if(!logsEnabled) {return;}
            Debug.unityLogger.Log(type, GetColoredMessage(message.ToString(), messageColor) as object, context);
        }

        #endregion

    } // class end
}
