/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/26/2024
 * Last Modified:   07/17/2026 (Ryan)
 * Notes:           Loot Generator
 *                      Procedure starts after the Map Generation Procedure.
*/
using RyansLibrary.Console;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Third and final pass, run strictly after the labyrinth finishes generating (subscribes to
    /// MapGeneratorController.OnGenerationDone rather than being called directly. 
    /// For now, walks every alt-path's prize rooms and triggers their chest spawn pads - loot placement is
    /// deliberately decoupled from room/blueprint generation so it can be re-run or disabled independently.
    /// </summary>
    public class LootGenerator : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Header("Debug")]
        [SerializeField] private bool _debug;

        private void OnEnable()
        {
            // Subscribe to "Done" event in map generator and spawn loot after
            MapGeneratorController.OnGenerationDone += SpawnLoot;
            RegisterConsoleCommands();
        }

        private void OnDisable()
        {
            // Unsubscribe to "Done" event in map generator and spawn loot after
            MapGeneratorController.OnGenerationDone -= SpawnLoot;
            UnregisterConsoleCommands();
        }

        private void SpawnLoot()
        {
            // Return if the Loot Generator is not enabled
            if (!_enabled)
                return;

            List<Zone> zones = MapGeneratorController.Instance.Zones;

            if (zones == null)
                return;

            foreach (var zone in zones)
            {
                foreach (var altPath in zone.Paths)
                {
                    foreach (var room in altPath.Rooms)
                    {
                        if (room.RoomType == RoomType.prize)
                        {
                            foreach (SpawnPad spawnPad in room.RoomSpawners)
                            {
                                if (spawnPad.Type == PadType.chest)
                                {
                                    // Spawn chest
                                    spawnPad.SpawnObject();
                                }
                            }
                        }
                    }
                }
            }

            if (_debug) Debug.Log("Loot Spawn Procedure has ended.");
        }

        private void RegisterConsoleCommands()
        {
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "lootgenerator.spawn",
                "Toggles loot spawning. Enter 'true' for on and 'false' for off.",
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
                    Debug.Log($"Loot Spawning set to {_enabled}.");
                }
                ));
        }

        private void UnregisterConsoleCommands()
        {
            ConsoleUI.CommandRegistry.UnregisterCommand("lootgenerator.spawn");
        }
    }
}
