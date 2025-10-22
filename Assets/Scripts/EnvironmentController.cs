using RyansLibrary.Console;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    private void Start()
    {
        RegisterConsoleCommand();
    }

    private void RegisterConsoleCommand()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.togglefog",
            "Toggles global fog.",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("Console Warning: Missing arguement. Must state true/false.");
                    return;
                }

                if (bool.TryParse(args[0], out bool enableFog))
                {
                    ToggleFog(enableFog);
                    Debug.Log($"Console: Fog {(enableFog ? "enabled" : "disabled")}");
                }
                else
                {
                    Debug.LogWarning($"Console Warning: Invalid Arguement {args[0]}. Use 'true' or 'false'");
                }
                ConsoleUI.OnNewConsoleOutput("Fog Toggled");
                Debug.Log($"Console: Fog Toggle Command");
            }));
    }

    private void ToggleFog(bool toggle)
    {
        RenderSettings.fog = toggle;
    }
}
