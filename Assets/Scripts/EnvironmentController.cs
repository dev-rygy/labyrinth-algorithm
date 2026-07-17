using RyansLibrary.Console;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    private void Start()
    {
        RegisterConsoleCommands();
    }

    private void RegisterConsoleCommands()
    {
        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.togglefog",
            "Toggles global fog.",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("[Console] Missing argument. Must state true/false.");
                    ConsoleUI.OnNewConsoleOutput($"Missing argument. Must state true/false.", LogType.Warning);
                    return;
                }

                if (bool.TryParse(args[0], out bool enableFog))
                {
                    ToggleFog(enableFog);
                    Debug.Log($"[Environment] Fog {(enableFog ? "enabled" : "disabled")}.");
                    ConsoleUI.OnNewConsoleOutput($"Fog {(enableFog ? "enabled" : "disabled")}.", LogType.Log);
                }
                else
                {
                    Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Use 'true' or 'false'.");
                    ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. Use 'true' or 'false'.", LogType.Warning);

                }
            }));

        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.setambientintensity",
            "Sets the ambient light intensity",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("[Console] Missing argument. Must state a number between 0 and 255.");
                    ConsoleUI.OnNewConsoleOutput($"Missing argument. Must state a number between 0 and 255.", LogType.Warning);
                    return;
                }

                if (int.TryParse(args[0], out int intensity))
                {
                    if (intensity < 0 || intensity > 255)
                    {
                        Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Must be a number between 0 and 255.");
                        ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. Must be a number between 0 and 255.", LogType.Warning);
                        return;
                    }

                    SetAmbientIntensity(intensity);
                }
                else
                {
                    Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Must be a number between 0 and 255.");
                    ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. Must be a number between 0 and 255.", LogType.Warning);
                }

                Debug.Log($"[Environment] Ambient Light Intensity set to {intensity}.");
                ConsoleUI.OnNewConsoleOutput($"Ambient Light Intensity set to {intensity}.", LogType.Log);
            }));
    }

    private void SetAmbientIntensity(int intensity)
    {
        if (intensity < 0 || intensity > 255)
        {
            Debug.LogWarning($"[Environment] Intensity {intensity} is not valid. Must be a number between 0 and 255.");
            ConsoleUI.OnNewConsoleOutput($"Intensity {intensity} is not valid. Must be a number between 0 and 255.", LogType.Warning);
            return;
        }

        RenderSettings.ambientLight = new Color(intensity / 255f, intensity / 255f, intensity / 255f);
    }

    private void ToggleFog(bool toggle)
    {
        RenderSettings.fog = toggle;
    }
}
