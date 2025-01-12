#if UNITY_EDITOR
using UnityEngine;

namespace Gaskellgames
{
    public class Comment : MonoBehaviour
    {
        [SerializeField, TextArea(5, 10)]
        private string comment = "Add comment here...";

        private void RemoveWarnings()
        {
            if (string.IsNullOrEmpty(comment))
            {
                // blank
            };
        }
        
    } // class end
}

#endif