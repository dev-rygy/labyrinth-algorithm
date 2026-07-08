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

        ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.setambientintensity",
            "Sets the ambient light intensity.",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("Console Warning: Missing arguement. Must state a number between 0 and 255.");
                    return;
                }

                if (int.TryParse(args[0], out int intensity))
                {
                    if (intensity < 0 || intensity > 255)
                    {
                        Debug.LogWarning($"Console Warning: Invalid Arguement {args[0]}. Must be a number between 0 and 255.");
                        return;
                    }

                    SetAmbientIntensity(intensity);
                }
                else
                {
                    Debug.LogWarning($"Console Warning: Invalid Arguement {args[0]}. Must be a number between 0 and 255.");
                }
                ConsoleUI.OnNewConsoleOutput("Ambient Light Intensity Set");
                Debug.Log($"Console: Ambient Light Intensity Set Command");
            }));
    }

    private void SetAmbientIntensity(int intensity)
    {
        if (intensity < 0 || intensity > 255)
        {
            Debug.LogWarning($"Intensity {intensity} is not valid. Must be a number between 0 and 255.");
            return;
        }

        RenderSettings.ambientLight = new Color(intensity / 255f, intensity / 255f, intensity / 255f);
    }

    private void ToggleFog(bool toggle)
    {
        RenderSettings.fog = toggle;
    }
}
