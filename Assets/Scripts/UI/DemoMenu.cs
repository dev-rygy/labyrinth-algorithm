/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/19/2025
 * Last Modified:   09/19/2025 
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
        MapGenerator.Instance.DestroyAllRooms();
        ApplicationController.Instance.StartNewGame();
    }

    public void ExitToMenu()
    {
        MapGenerator.Instance.DestroyAllRooms();
        ApplicationController.Instance.EndGame();
    }
}
