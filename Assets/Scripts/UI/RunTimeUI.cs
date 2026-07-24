using RyansLibrary.Labyrinth;
using TMPro;
using UnityEngine;

public class RunTimeUI : MonoBehaviour
{
    [SerializeField] private GameObject _helpContentObject;
    [SerializeField] private GameObject _seedContentObject;
    [SerializeField] private GameObject _interactionContentObject;
    [SerializeField] private TMP_Text _helpText;
    [SerializeField] private TMP_Text _seedText;
    [SerializeField] private GameObject _debuggingOptionsObject;
    [SerializeField] [TextArea(6, 10)] private string _runtimeTextDescription; 
    [SerializeField] [TextArea(6, 10)] private string _debuggingTextDescription;

    private MapGeneratorController _mapGeneratorInstance;
    private ApplicationController _applicationControllerInstance;

    private void Start()
    {
        _mapGeneratorInstance = MapGeneratorController.Instance;
        _applicationControllerInstance = ApplicationController.Instance;

        if (_mapGeneratorInstance.DebugSequential)
        {
            ShowDebuggingOptions();
        }
        else
        {
            HideDebuggingOptions();
        }
    }

    private void OnEnable()
    {
        MapGeneratorController.OnGenerationStarted += SetHelpContentText;
        MapGeneratorController.OnGenerationDone += SetHelpContentText;
        MapGeneratorController.OnGenerationDone += HideDebuggingOptions;
        MapGeneratorController.OnSeedUpdate += UpdateSeedText;

        // Subscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened += HideContent;
        ConsoleUI.OnConsoleClosed += ShowContent;
    }

    private void OnDisable()
    {
        MapGeneratorController.OnGenerationStarted -= SetHelpContentText;
        MapGeneratorController.OnGenerationDone -= SetHelpContentText;
        MapGeneratorController.OnGenerationDone -= HideDebuggingOptions;
        MapGeneratorController.OnSeedUpdate -= UpdateSeedText;

        // Unsubscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened -= HideContent;
        ConsoleUI.OnConsoleClosed -= ShowContent;
    }

    // Button Functions
    public void Regenerate()
    {
        _mapGeneratorInstance.ResetLabyrinth();
        _applicationControllerInstance.StartNewGame();      // DELETE AFTER DEMO
    }

    public void Step(int steps)
    {
        _mapGeneratorInstance.Advance(steps);
    }

    public void StepAll()
    {
        _mapGeneratorInstance.AdvanceAll();
    }

    public void ExitToMenu()
    {
        _applicationControllerInstance.EndGame();
    }

    // Show/Hide Content
    private void ShowContent()
    {
        _helpContentObject.SetActive(true);
        _seedContentObject.SetActive(true);
        _interactionContentObject.SetActive(true);

        if (_mapGeneratorInstance.IsGenerating && _mapGeneratorInstance.DebugSequential)
        {
            ShowDebuggingOptions();
        }
        else
        {
            HideDebuggingOptions();
        }
    }

    private void HideContent()
    {
        _helpContentObject.SetActive(false);
        _seedContentObject.SetActive(false);
        _interactionContentObject.SetActive(false);
    }

    private void ShowDebuggingOptions()
    {
        _debuggingOptionsObject.SetActive(true);
    }

    private void HideDebuggingOptions()
    {
        _debuggingOptionsObject.SetActive(false);
    }

    private void SetHelpContentText()
    {
        if (_mapGeneratorInstance.IsGenerating)
        {
            if (_mapGeneratorInstance.DebugSequential)
                _helpText.text = _debuggingTextDescription;
            else
                _helpText.text = string.Empty;
        }
        else
        {
            _helpText.text = _runtimeTextDescription;
        }
    }

    // Seed Display
    private void UpdateSeedText()
    {
        if (_mapGeneratorInstance != null)
        {
            _seedText.text = $"Seed: {_mapGeneratorInstance.Seed}";
        }
    }

}
