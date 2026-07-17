/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/18/2024
 * Last Modified:   06/30/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Console;
using UnityEngine;

public enum CameraMode
{
    Main,
    Player,
    Free
}

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private GameObject mainCameraObject;
    [SerializeField] private GameObject freeCameraObject;

    private GameObject currentCamera;
    private Camera playerCamera;
    private bool isFreeCameraEnabled = false;
    public CameraMode currentCameraMode = CameraMode.Main;
    private CameraMode previousCameraMode = CameraMode.Main;

    private void OnEnable()
    {
        PlayerStateMachine.OnPlayerSpawned += SetPlayerCamera;
    }

    private void OnDisable()
    {
        PlayerStateMachine.OnPlayerSpawned -= SetPlayerCamera;
    }

    private void Awake()
    {
        // Handle singleton; if instance has a reference and the reference is not this object
        if (Instance != null)
        {
            Debug.LogWarning("Another instance of CameraController already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Set initial camera mode to Main
        SetCameraMode(CameraMode.Main);
        RegisterConsoleCommand();
    }

    private void SetPlayerCamera(PlayerStateMachine playerStateMachine)
    {
        playerCamera = playerStateMachine.gameObject.GetComponentInChildren<Camera>();
        playerCamera.gameObject.SetActive(false);
    }

    public void SetCameraMode(CameraMode mode)
    {
        switch (mode)
        {
            case CameraMode.Player:
                EnablePlayerCamera();
                break;
            case CameraMode.Free:
                EnableFreeCamera();
                break;
            case CameraMode.Main:
                EnableMainCamera();
                break;
            default:
                Debug.LogError("Invalid camera mode: " + mode);  
                break;
        }
    }

    private void EnablePlayerCamera()
    {
        mainCameraObject.SetActive(false);
        freeCameraObject.SetActive(false);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
        }

        currentCameraMode = CameraMode.Player;
        currentCamera = playerCamera.gameObject;
        isFreeCameraEnabled = false;
    }

    private void EnableFreeCamera()
    {
        mainCameraObject.SetActive(false);
        freeCameraObject.SetActive(true);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        // Set the free camera's position and rotation to match the current camera
        freeCameraObject.GetComponent<FreeCameraController>().SetCameraTransform(currentCamera.transform);

        currentCameraMode = CameraMode.Free;
        currentCamera = freeCameraObject;
        isFreeCameraEnabled = true;
    }

    private void EnableMainCamera()
    {
        mainCameraObject.SetActive(true);
        freeCameraObject.SetActive(false);

        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(false);
        }

        currentCameraMode = CameraMode.Main;
        currentCamera = mainCameraObject;
        isFreeCameraEnabled = false;
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "camera.mode",
            "Toggles A Camera Mode",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("No argument given, please enter a camera mode.");
                    return;
                }

                switch (args[0])
                {
                    case "player":
                        if (currentCameraMode == CameraMode.Player)
                        {
                            Debug.LogWarning("Player Camera is already enabled.");
                            return;
                        }

                        if (playerCamera == null)
                        {
                            Debug.LogWarning("Player Camera is not yet initialized. Please wait until the player has spawned.");
                            return;
                        }

                        SetCameraMode(CameraMode.Player);
                        Debug.Log($"Player Camera toggled.");
                        break;
                    case "free":
                        if (currentCameraMode == CameraMode.Free)
                        {
                            Debug.LogWarning("Free Camera is already enabled.");
                            return;
                        }

                        if (isFreeCameraEnabled)
                        {
                            SetCameraMode(previousCameraMode);
                        }
                        else
                        {
                            previousCameraMode = currentCameraMode;
                            SetCameraMode(CameraMode.Free);
                        }

                        Debug.Log($"Free Camera toggled.");
                        break;
                    case "main":
                        if (currentCameraMode == CameraMode.Main)
                        {
                            Debug.LogWarning("Main Camera is already enabled.");
                            return;
                        }

                        SetCameraMode(CameraMode.Main);
                        Debug.Log($"Main Camera toggled.");
                        break;
                    default:
                        Debug.LogWarning($"Invalid argument '{args[0]}'. Please enter a valid camera mode.");
                        return;
                }
            }));
    }
}
