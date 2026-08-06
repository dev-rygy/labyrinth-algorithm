/*
 * Created By:      Ryan Carpenter
 * Date Created:    07/27/2026
 * Last Modified:   07/27/2026 (Ryan)
 * Notes:           Enemy Generator
 *                      Procedure starts after the Map Generation Procedure.
*/
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGenerator : MonoBehaviour
{
    [SerializeField] private bool _enabled = true;


    [Header("Debug")]
    [SerializeField] private bool _debug;

    private void OnEnable()
    {
        // Subscribe to "Done" event in map generator and spawn loot after
        MapGeneratorController.OnGenerationDone += SpawnEnemies;
    }

    private void OnDisable()
    {
        // Unsubscribe to "Done" event in map generator and spawn loot after
        MapGeneratorController.OnGenerationDone -= SpawnEnemies;
    }

    private void SpawnEnemies()
    {
        // Return if the Loot Generator is not enabled
        if (!_enabled)
            return;

        List<Zone> zones = MapGeneratorController.Instance.Zones;

        if (zones == null)
            return;

        foreach (var zone in zones)
        {
            foreach(var room in zone.MainPath.Rooms)
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

        if (_debug) Debug.Log("Loot Spawn Procedure has ended.");
    }
}
