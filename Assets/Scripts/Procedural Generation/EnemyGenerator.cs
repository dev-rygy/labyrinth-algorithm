/*
 * Created By:      Ryan Carpenter
 * Date Created:    07/27/2026
 * Last Modified:   08/10/2026 (Ryan)
 * Notes:           Enemy Generator
 *                  Procedure begins after map generation is complete.
*/
using RyansLibrary.Console;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class EnemyGenerator : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;


        [Header("Debug")]
        [SerializeField] private bool _debug;

        private void OnEnable()
        {
            // Subscribe to "Done" event in map generator and spawn enemies after
            MapGeneratorController.OnGenerationDone += SpawnEnemies;
            RegisterConsoleCommands();
        }

        private void OnDisable()
        {
            // Unsubscribe to "Done" event in map generator and spawn enemies after
            MapGeneratorController.OnGenerationDone -= SpawnEnemies;
            UnregisterConsoleCommands();
        }

        private void SpawnEnemies()
        {
            // Return if the Enemy Generator is not enabled
            if (!_enabled)
                return;

            List<Zone> zones = MapGeneratorController.Instance.Zones;

            if (zones == null)
                return;

            foreach (var zone in zones)
            {
                // Spawn enemies in the main path
                foreach (var room in zone.MainPath.Rooms)
                {
                    foreach (SpawnPad spawnPad in room.RoomSpawners)
                    {
                        if (spawnPad.Type == PadType.enemy)
                        {
                            // Spawn enemy
                            spawnPad.SpawnObject();
                        }
                    }
                }

                // Spawn enemies in the alternative paths
                foreach (var altPath in zone.Paths)
                {
                    foreach (var room in altPath.Rooms)
                    {
                        foreach (SpawnPad spawnPad in room.RoomSpawners)
                        {
                            if (spawnPad.Type == PadType.enemy)
                            {
                                // Spawn enemy
                                spawnPad.SpawnObject();
                            }
                        }
                    }
                }
            }

            if (_debug) Debug.Log("Enemy Spawn Procedure has ended.");
        }

        private void RegisterConsoleCommands()
        {
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "enemygenerator.spawn",
                "Toggles enemy spawning. Enter 'true' for on and 'false' for off.",
                args =>
                {
                    if (args.Length < 1)
                    {
                        Debug.LogWarning("No argument given, please enter 'true' or 'false'.");
                        return;
                    }

                    if (args[0] == "true")
                    {
                        _enabled = true;
                    }
                    else if (args[0] == "false")
                    {
                        _enabled = false;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid argument {args[0]}. Please input either 'true' or 'false'.");
                    }
                    Debug.Log($"Enemy Spawning set to {_enabled}.");
                }
                ));
        }

        private void UnregisterConsoleCommands()
        {
            ConsoleUI.CommandRegistry.UnregisterCommand("enemygenerator.spawn");
        }
    }
}
