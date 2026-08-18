using RyansLibrary.Debugging;
using UnityEngine;

public class EnvironmentController : MonoBehaviour
{
    [SerializeField] private bool _enableFogOnGameStart;
    [SerializeField, Range(0, 255)] private int _ambientIntensityOnGameStart;

    private void Start()
    {
        RegisterConsoleCommands();

        ScenesManager.OnSceneLoaded += SetDefaultEnvironmentVariables;
    }

    private void OnDisable()
    {
        ScenesManager.OnSceneLoaded -= SetDefaultEnvironmentVariables;
    }

    private void SetDefaultEnvironmentVariables()
    {
        SetAmbientIntensity(_ambientIntensityOnGameStart);
        ToggleFog(_enableFogOnGameStart);
    }

    private void RegisterConsoleCommands()
    {
        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.fog",
            "Toggles global fog.",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("Missing argument. Must state true/false.");
                    return;
                }

                if (bool.TryParse(args[0], out bool enableFog))
                {
                    ToggleFog(enableFog);
                    Debug.Log($"Fog {(enableFog ? "enabled" : "disabled")}.");
                }
                else
                {
                    Debug.LogWarning($"Invalid argument '{args[0]}'. Use 'true' or 'false'.");

                }
            }));

        Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
            "environment.setambientintensity",
            "Sets the ambient light intensity",
            args =>
            {
                if (args.Length != 1)
                {
                    Debug.LogWarning("Missing argument. Must state a number between 0 and 255.");
                    return;
                }

                if (int.TryParse(args[0], out int intensity))
                {
                    if (intensity < 0 || intensity > 255)
                    {
                        Debug.LogWarning($"Invalid argument '{args[0]}'. Must be a number between 0 and 255.");
                        return;
                    }

                    SetAmbientIntensity(intensity);
                }
                else
                {
                    Debug.LogWarning($"Invalid argument '{args[0]}'. Must be a number between 0 and 255.");
                }

                Debug.Log($"Ambient Light Intensity set to {intensity}.");
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
