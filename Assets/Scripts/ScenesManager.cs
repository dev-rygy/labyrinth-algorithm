/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Wrapper class for Unity's Scene Management class
*/
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wrapper class for Unity's Scene Management class
/// </summary>
public class ScenesManager : MonoBehaviour
{
    // TODO: Just make the class static?
    public static ScenesManager Instance { get; private set; }

    private void Awake()
    {
        // Handle singleton
        if (Instance != null)
        {
            Debug.LogWarning("[ScenesManager] Another instance of ScenesManager already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        gameObject.transform.parent = null;     // Parent must be cleared to be DNDOL
        DontDestroyOnLoad(gameObject);
    }

    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }

    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    public void ReloadScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneIndex);
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void LoadScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public IEnumerator LoadSceneAsync(int index)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(index);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public IEnumerator LoadSceneAsync(string name)
    {
        var asyncLoad = SceneManager.LoadSceneAsync(name);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public IEnumerator ReloadSceneAsync()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        var asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    public void LoadNextScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(sceneIndex);
    }

    public void LoadPreviousScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex - 1;
        SceneManager.LoadScene(sceneIndex);
    }
}
