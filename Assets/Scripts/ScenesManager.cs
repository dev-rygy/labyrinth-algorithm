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
        if (Instance && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void ReloadScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }
}
