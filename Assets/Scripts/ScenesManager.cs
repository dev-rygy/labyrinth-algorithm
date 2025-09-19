/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Wrapper class for Unity's Scene Management class
*/
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wrapper class for Unity's Scene Management class
/// </summary>
public class ScenesManager : MonoBehaviour
{
    public static ScenesManager Instance { get; private set; }

    private void Awake()
    {
        // Handle singleton
        if (Instance != null)
        {
            Debug.LogWarning("Scenes Manager Warning: Another instance of ScenesManager already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        gameObject.transform.parent = null;     // Parent must be cleared to be DNDOL
        DontDestroyOnLoad(gameObject);
    }

    public void ReloadScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }
}
