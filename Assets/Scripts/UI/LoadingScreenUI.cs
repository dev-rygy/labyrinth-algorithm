using RyansLibrary.Labyrinth;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    private enum LoadingStatus
    {
        Starting = 0,
        Initializing,
        Operations,
        Rooms,
        Done
    }

    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _loadingText;

    private int _totalOperationCount;
    private int _currentOperationCount;
    LoadingStatus _loadingStatus;

    private void Awake()
    {
        if (_loadingSlider is null)
        {
            Debug.LogError("Error: Loading slider is not assigned");
            return;
        }

        if (_loadingText is null)
        {
            Debug.LogError("Error: Loading text is not assigned");
            return;
        }
    }

    private void OnEnable()
    {
        MapGeneratorController.OnOperationsGetTotal += SetTotalOperationCount;
        MapGeneratorController.OnOperationsUpdate += UpdateLoadingSlider;

        // Subscribing Text Update Events
        MapGeneratorController.OnGenerationStarted += UpdateLoadingText;
        MapGeneratorController.OnOperationsStarted += UpdateLoadingText;
        MapGeneratorController.OnOperationExecuted += UpdateLoadingText;
        MapGeneratorController.OnOperationsEnded += UpdateLoadingText;
        MapGeneratorController.OnRoomParseStarted += UpdateLoadingText;
        MapGeneratorController.OnRoomParseDone += UpdateLoadingText;

        MapGeneratorController.OnGenerationStarted += UpdateTextStatus;
        MapGeneratorController.OnOperationsStarted += UpdateTextStatus;
        MapGeneratorController.OnOperationsEnded += UpdateTextStatus;
        MapGeneratorController.OnRoomParseStarted += UpdateTextStatus;
        MapGeneratorController.OnRoomParseDone += UpdateTextStatus;
    }

    private void OnDisable()
    {
        // Unsubscribing Text Update Events
        MapGeneratorController.OnGenerationStarted -= UpdateTextStatus;
        MapGeneratorController.OnOperationsStarted -= UpdateTextStatus;
        MapGeneratorController.OnOperationsEnded -= UpdateTextStatus;
        MapGeneratorController.OnRoomParseStarted -= UpdateTextStatus;
        MapGeneratorController.OnRoomParseDone -= UpdateTextStatus;
    }

    private void Start()
    {
        _loadingSlider.value = 0;
    }

    private void SetTotalOperationCount(int totalOperationCount)
    {
        _totalOperationCount = totalOperationCount;
    }

    private void UpdateLoadingSlider(int currentOperationCount)
    {
        _currentOperationCount = currentOperationCount;

        if (_totalOperationCount < 0)
        {
            Debug.LogError("Error: Divide by zero"); 
            return;
        }

        float value = 1 - ((float)_currentOperationCount / (float)_totalOperationCount);
        Debug.Log($"Value: {value}");
        _loadingSlider.value = value;
    }

    private void UpdateLoadingText()
    {
        switch (_loadingStatus)
        {
            case LoadingStatus.Starting:
                _loadingText.text = "Starting";
                break;
            case LoadingStatus.Initializing:
                _loadingText.text = "Initializing";
                break;
            case LoadingStatus.Operations:
                _loadingText.text = $"Executing Operations";
                break;
            case LoadingStatus.Rooms:
                _loadingText.text = $"Generating Rooms";
                break;
            case LoadingStatus.Done:
                _loadingText.text = $"Finalizing";
                break;
            default:
                _loadingText.text = $"Loading...";
                break;
        }
    }

    private void UpdateTextStatus()
    {
        _loadingStatus++;
    }
}
