/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/18/2024
 * Last Modified:   06/30/2026 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Utilities
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        // Tracks which persistent objects already exist so re-loading a bootstrap scene
        // mid-session (e.g. returning to the main menu) doesn't spawn duplicates that
        // race to register themselves as DontDestroyOnLoad.
        private static readonly HashSet<string> _persistedNames = new HashSet<string>();

        private void Awake()
        {
            if (!_persistedNames.Add(gameObject.name))
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            gameObject.transform.SetParent(null);     // Parent must be cleared to be DNDOL
        }
    }
}
