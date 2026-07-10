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
            Debug.LogWarning("[CameraController] Another instance of CameraController already exists. Deleting Object...");
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
                Debug.LogError("[CameraController] Invalid camera mode: " + mode);  
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

        currentCameraMode = CameraMode.Free;
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
        isFreeCameraEnabled = false;
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "camera.togglemode",
            "Toggles A Camera Mode",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("[Console] No arguement given, please enter a camera mode.");
                    ConsoleUI.OnNewConsoleOutput($"[Console] No arguement given, please enter a camera mode.");
                    return;
                }

                switch (args[0])
                {
                    case "player":
                        if (currentCameraMode == CameraMode.Player)
                        {
                            Debug.LogWarning("[CameraController] Player Camera is already enabled.");
                            ConsoleUI.OnNewConsoleOutput($"[CameraController] Player Camera is already enabled.");
                            return;
                        }

                        if (playerCamera == null)
                        {
                            Debug.LogWarning("[CameraController] Player Camera is not yet initialized. Please wait until the player has spawned.");
                            ConsoleUI.OnNewConsoleOutput($"[CameraController] Player Camera is not yet initialized. Please wait until the player has spawned.");
                            return;
                        }

                        SetCameraMode(CameraMode.Player);
                        Debug.Log($"[CameraController] Player Camera toggled.");
                        ConsoleUI.OnNewConsoleOutput($"[CameraController] Player Camera toggled.");
                        break;
                    case "free":
                        if (currentCameraMode == CameraMode.Free)
                        {
                            Debug.LogWarning("[CameraController] Free Camera is already enabled.");
                            ConsoleUI.OnNewConsoleOutput($"[CameraController] Free Camera is already enabled.");
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

                        Debug.Log($"[CameraController] Free Camera toggled.");
                        ConsoleUI.OnNewConsoleOutput($"[CameraController] Free Camera toggled");
                        break;
                    case "main":
                        if (currentCameraMode == CameraMode.Main)
                        {
                            Debug.LogWarning("[CameraController] Main Camera is already enabled.");
                            ConsoleUI.OnNewConsoleOutput($"[CameraController] Main Camera is already enabled.");
                            return;
                        }

                        SetCameraMode(CameraMode.Main);
                        Debug.Log($"[CameraController] Main Camera toggled.");
                        ConsoleUI.OnNewConsoleOutput($"[CameraController] Main Camera toggled.");
                        break;
                    default:
                        Debug.LogWarning($"[Console] Invalid Arguement {args[0]}. Please enter a valid camera mode.");
                        ConsoleUI.OnNewConsoleOutput($"[Console] Invalid Arguement {args[0]}. Please enter a valid camera mode.");
                        return;
                }
            }));
    }
}
