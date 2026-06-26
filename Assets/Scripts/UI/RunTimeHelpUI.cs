using RyansLibrary.Labyrinth;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class RunTimeHelpUI : MonoBehaviour
{
    [SerializeField] private GameObject _contentObject;
    [SerializeField] private TMP_Text _helpText;
    [SerializeField] [TextArea(6, 10)] private string _runtimeTextDescription; 
    [SerializeField] [TextArea(6, 10)] private string _debuggingTextDescription; 

    private void OnEnable()
    {
        MapGeneratorController.OnGenerationStarted += SetTextContent;
        MapGeneratorController.OnGenerationDone += SetTextContent;

        // Subscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened += HideContent;
        ConsoleUI.OnConsoleClosed += ShowContent;
    }

    private void OnDisable()
    {
        MapGeneratorController.OnGenerationStarted -= SetTextContent;
        MapGeneratorController.OnGenerationDone -= SetTextContent;

        // Unsubscribe to Console Open and Close events
        ConsoleUI.OnConsoleOpened -= HideContent;
        ConsoleUI.OnConsoleClosed -= ShowContent;
    }

    private void ShowContent()
    {
        _contentObject.SetActive(true);
    }

    private void HideContent()
    {
        _contentObject.SetActive(false);
    }

    private void SetTextContent()
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
}
