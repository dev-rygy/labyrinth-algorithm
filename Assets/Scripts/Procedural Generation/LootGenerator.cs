/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/26/2024
 * Last Modified:   07/17/2026 (Ryan)
 * Notes:           Loot Generator
 *                      Procedure starts after the Map Generation Procedure
 *                      Parse through the Master Path and spawn loot where
 *                      applicable.
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class LootGenerator : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Header("Debug")]
        [SerializeField] private bool _debug;

        private void OnEnable()
        {
            // Subscribe to "Done" event in map generator and spawn loot after
            MapGeneratorController.OnGenerationDone += SpawnLoot;
        }

        private void OnDisable()
        {
            // Unsubscribe to "Done" event in map generator and spawn loot after
            MapGeneratorController.OnGenerationDone -= SpawnLoot;
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
    }
}
