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
    private static DemoMenuUI Instance;       // Should not be accessed by another class

    private void Awake()
    {
        // Handle singleton
        if (Instance != null)
        {
            Debug.LogWarning("[DemoMenuUI] Another instance of DemoMenuUI already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartNewGame()
    {
        ApplicationController.Instance.StartNewGame();
    }

    public void StartDebugging()
    {
        MapGeneratorController.Instance.ToggleStepwiseDebugging(true);
        ApplicationController.Instance.StartNewGame();
    }

    public void Regenerate()
    {
        MapGeneratorController.Instance.ResetLabyrinth();
        ApplicationController.Instance.StartNewGame();      // DELETE AFTER DEMO
    }

    public void Step()
    {
        MapGeneratorController.Instance.Advance(1);
    }

    public void StepAll()
    {
        MapGeneratorController.Instance.AdvanceAll();
    }

    public void ExitToMenu()
    {
        ApplicationController.Instance.EndGame();
    }
}
