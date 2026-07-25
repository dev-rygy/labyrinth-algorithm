/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   07/09/2025 (Ryan)
 * Notes:           Custom Menu used for demo
*/
using RyansLibrary.Labyrinth;
using UnityEngine;

public class DemoMenuUI : MonoBehaviour
{
    public void StartNewGame()
    {
        MapGeneratorController.Instance.ToggleStepwiseDebugging(false);
        ApplicationController.Instance.StartNewGame();
    }

    public void StartDebugging()
    {
        MapGeneratorController.Instance.ToggleStepwiseDebugging(true);
        ApplicationController.Instance.StartNewGame();
    }

    public void ExitDemo()
    {
        Application.Quit();
    }
}
