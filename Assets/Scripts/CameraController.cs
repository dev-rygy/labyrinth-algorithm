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
            Debug.LogWarning("Player State Machine Warning: Another instance of PlayerStateMachine already exists. Deleting Object...");
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
        playerCamera?.gameObject.SetActive(true);

        currentCameraMode = CameraMode.Player;
        isFreeCameraEnabled = false;
    }

    private void EnableFreeCamera()
    {
        mainCameraObject.SetActive(false);
        freeCameraObject.SetActive(true);
        playerCamera?.gameObject.SetActive(false);

        currentCameraMode = CameraMode.Free;
        isFreeCameraEnabled = true;
    }

    private void EnableMainCamera()
    {
        mainCameraObject.SetActive(true);
        freeCameraObject.SetActive(false);
        playerCamera?.gameObject.SetActive(false);

        currentCameraMode = CameraMode.Main;
        isFreeCameraEnabled = false;
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "togglecameramode",
            "Toggles A Camera Mode",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("[Console] No arguement given, please enter a camera mode.");
                    return;
                }

                if (args[0] == "player")
                {
                    if (playerCamera == null)
                    {
                        Debug.LogWarning("[Console] Player Camera is not yet initialized. Please wait until the player has spawned.");
                        return;
                    }

                    SetCameraMode(CameraMode.Player);
                }
                else if (args[0] == "free")
                {
                    if (isFreeCameraEnabled)
                    {
                        SetCameraMode(previousCameraMode);
                    }
                    else
                    {
                        previousCameraMode = currentCameraMode;
                        SetCameraMode(CameraMode.Free);
                    }
                    ConsoleUI.OnNewConsoleOutput("Free Camera Toggled");
                }
                else if (args[0] == "main")
                {
                    SetCameraMode(CameraMode.Main);
                }
                else
                {
                    Debug.LogWarning($"[Console] Invalid Arguement {args[0]}. please enter a valid camera mode.");
                }

                Debug.Log($"Console: Toggle Camera Mode Command");
            }));
    }
}
