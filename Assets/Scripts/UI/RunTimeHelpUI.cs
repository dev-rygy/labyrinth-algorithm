using RyansLibrary.Labyrinth;
using UnityEngine;

public class RunTimeHelpUI : MonoBehaviour
{
    [SerializeField] private GameObject textObject;

    private void Awake()
    {
        HideText();

        MapGeneratorController.OnGenerationDone += ShowText;
    }

    private void OnEnable()
    {
        ConsoleUI.OnConsoleOpened += HideText;
        ConsoleUI.OnConsoleClosed += ShowText;
    }

    private void OnDisable()
    {
        ConsoleUI.OnConsoleOpened -= HideText;
        ConsoleUI.OnConsoleClosed -= ShowText;
    }

    private void ShowText()
    {
        ToggleText(true);
    }

    private void HideText()
    {
        ToggleText(false);
    }

    private void ToggleText(bool toggle)
    {
        if (MapGeneratorController.Instance.IsGenerating)
            return;

        textObject.SetActive(toggle);
    }
}
