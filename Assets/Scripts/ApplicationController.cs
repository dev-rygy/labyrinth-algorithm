/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           Controls the execution order of application
*/
using System.Collections;
using UnityEngine;

using RyansLibrary.Labyrinth;
using System;

/// <summary>
/// Controls the execution order and game states.
/// </summary>
public class ApplicationController : MonoBehaviour
{
    const string MAIN_SCENE_NAME = "Main";

    public static event Action OnMenu;
    public static ApplicationController Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPoint = new Vector3(65, 0, 0);
    [Header("Camera")]
    [SerializeField] private CameraController _cameraController;
    [SerializeField] private bool _setCameraToPlayerOnGameStart = true;
    [Header("Cursor")]
    [SerializeField] private bool _hideCursorOnGameStart = false;
    [SerializeField] private CursorLockMode _cursorLockMode = CursorLockMode.None;
    [Header("Console")]
    [SerializeField] private bool _clearConsoleOnGameStart = false;

    private GameObject _player;

    private void Awake()
    {
        // Handle Singleton
        if (Instance != null)
        {
            Debug.LogWarning("Another instance of ApplicationController already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        gameObject.transform.parent = null;     // Parent must be cleared to be DNDOL
        DontDestroyOnLoad(gameObject);  // Have this gameObject persist
    }

    private void Start()
    {
        _cameraController.SetCameraMode(CameraMode.Main);
    }

    public void StartNewGame()
    {
        if (_clearConsoleOnGameStart) 
            Debug.ClearDeveloperConsole();

        if (_player != null)
        {
            Destroy(_player);
            _player = null;
        }

        StartCoroutine(LoadNewGame());
    }

    public IEnumerator LoadNewGame()
    {
        if (ScenesManager.Instance.GetCurrentSceneName() != MAIN_SCENE_NAME)
        {
            // Load Scene
            yield return StartCoroutine(ScenesManager.Instance.LoadSceneAsync(MAIN_SCENE_NAME));
        }

        _cameraController.SetCameraMode(CameraMode.Main);

        // Start Map Generation
        yield return MapGeneratorController.Instance.StartGeneration();

        // Spawn player
        _player = Instantiate(playerPrefab, playerSpawnPoint, Quaternion.identity);

        if (_setCameraToPlayerOnGameStart)
            _cameraController.SetCameraMode(CameraMode.Player);

        Cursor.visible = _hideCursorOnGameStart ? false : true;
        Cursor.lockState = _cursorLockMode;
    }

    public void EndGame()
    {
        ScenesManager.Instance.LoadScene("DemoBootstrap");
        MapGeneratorController.Instance.ResetLabyrinth();
        _cameraController.SetCameraMode(CameraMode.Main);
        OnMenu?.Invoke();
    }

    private void ShowCursor()
    {

    }
}
