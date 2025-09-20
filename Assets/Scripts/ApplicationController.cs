/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   09/19/2025 (Ryan)
 * Notes:           Controls the execution order of application
*/
using RyansLibrary.Labyrinth;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the execution order and game states.
/// </summary>
public class ApplicationController : MonoBehaviour
{
    const string MAIN_SCENE_NAME = "Main";

    public static ApplicationController Instance { get; private set; }

    [SerializeField] private GameObject playerPrefab;
    
    [SerializeField] private Vector3 playerSpawnPoint = new Vector3(65, 0, 0);


    private void Awake()
    {
        // Handle Singleton
        if (Instance != null)
        {
            Debug.LogWarning("Map Generator Warning: Another instance of MapGenerator already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        gameObject.transform.parent = null;     // Parent must be cleared to be DNDOL
        DontDestroyOnLoad(gameObject);  // Have this gameObject persist
    }

    public void StartNewGame()
    {
        StartCoroutine(LoadNewGame());
    }

    public IEnumerator LoadNewGame()
    {
        yield return StartCoroutine(ScenesManager.Instance.LoadSceneAsync(MAIN_SCENE_NAME));

        MapGenerator.Instance.StartGeneration();

        Instantiate(playerPrefab, playerSpawnPoint, Quaternion.identity);
    }

    public void EndGame()
    {
        ScenesManager.Instance.LoadScene("DemoBootstrap");
    }
}
