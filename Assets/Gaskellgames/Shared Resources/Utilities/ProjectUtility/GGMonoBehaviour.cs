using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public class GGMonoBehaviour : MonoBehaviour
    {
        #region verboseLogs

        [Tooltip("If verbose logs is true: info message logs will be displayed in the console alongside warning and error message logs for this script instance.")]
        [SerializeField] protected bool verboseLogs = false;
        
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

        #region Conditional Gizmos

        [SerializeField] protected bool gizmosOnSelected;

        /// <summary>
        /// method for drawing conditional gizmos. Invoked from base class's OnDrawGizmos and OnDrawGizmosSelected
        /// </summary>
        /// <param name="selected">True if OnDrawGizmosSelected, false if OnDrawGizmos</param>
        protected virtual void OnDrawGizmosConditional(bool selected)
        {
            
        }
        
        protected virtual void OnDrawGizmos()
        {
            if (!gizmosOnSelected)
            {
                OnDrawGizmosConditional(false);
            }
        }

        protected virtual void OnDrawGizmosSelected()
        {
            OnDrawGizmosConditional(true);
        }

        #endregion
        
    } // class end
}
