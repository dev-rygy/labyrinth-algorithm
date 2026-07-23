using UnityEngine;

public class HUDUI : MonoBehaviour
{
    private static HUDUI Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
}
