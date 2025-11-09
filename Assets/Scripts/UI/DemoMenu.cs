/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           Custom Menu used for demo
*/
using RyansLibrary.Labyrinth;
using UnityEngine;

public class DemoMenu : MonoBehaviour
{
    private static DemoMenu Instance;       // Should not be accessed by another class

    private void Awake()
    {
        // Handle singleton
        if (Instance != null)
        {
            Debug.LogWarning("Demo Menu Warning: Another instance of DemoMenu already exists. Deleting Object...");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void StartNewGame()
    {
        ApplicationController.Instance.StartNewGame();
    }

    public void Regenerate()
    {
        MapGeneratorController.Instance.DestroyAllRooms();
        ApplicationController.Instance.StartNewGame();
    }

    public void ExitToMenu()
    {
        MapGeneratorController.Instance.DestroyAllRooms();
        ApplicationController.Instance.EndGame();
    }
}
