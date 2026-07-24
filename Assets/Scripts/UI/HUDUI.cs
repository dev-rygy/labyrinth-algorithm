using UnityEngine;

public class HUDUI : MonoBehaviour
{
    private static HUDUI Instance;

    [SerializeField] private ConsoleUI console;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;

        console.InitConsole();
    }
}
