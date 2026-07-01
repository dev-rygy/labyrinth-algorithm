using RyansLibrary.Labyrinth;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class RunTimeUI : MonoBehaviour
{
    [SerializeField] private GameObject _helpContentObject;
    [SerializeField] private GameObject _seedContentObject;
    [SerializeField] private TMP_Text _helpText;
    [SerializeField] private TMP_Text _seedText;
    [SerializeField] [TextArea(6, 10)] private string _runtimeTextDescription; 
    [SerializeField] [TextArea(6, 10)] private string _debuggingTextDescription;

    private void OnEnable()
    {
        MapGeneratorController.OnGenerationStarted += SetHelpContentText;
        MapGeneratorController.OnGenerationDone += SetHelpContentText;
        MapGeneratorController.OnSeedUpdate += UpdateSeedText;

        // Subscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened += HideHelpContent;
        ConsoleUI.OnConsoleClosed += ShowHelpContent;
    }

    private void OnDisable()
    {
        MapGeneratorController.OnGenerationStarted -= SetHelpContentText;
        MapGeneratorController.OnGenerationDone -= SetHelpContentText;
        MapGeneratorController.OnSeedUpdate -= UpdateSeedText;
        // Unsubscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened -= HideHelpContent;
        ConsoleUI.OnConsoleClosed -= ShowHelpContent;
    }

    private void ShowHelpContent()
    {
        _helpContentObject.SetActive(true);
        _seedContentObject.SetActive(true);
    }

    private void HideHelpContent()
    {
        _helpContentObject.SetActive(false);
        _seedContentObject.SetActive(false);
    }

    private void SetHelpContentText()
    {
        if (MapGeneratorController.Instance.IsGenerating)
        {
            if (MapGeneratorController.Instance.DebugSequential)
                _helpText.text = _debuggingTextDescription;
            else
                _helpText.text = string.Empty;
        }
        else
        {
            _helpText.text = _runtimeTextDescription;
        }
    }

    private void UpdateSeedText()
    {
        if (MapGeneratorController.Instance != null)
        {
            _seedText.text = $"Seed: {MapGeneratorController.Instance.Seed}";
        }
    }
}
