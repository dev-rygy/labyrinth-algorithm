/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/26/2024
 * Last Modified:   12/26/2024 (Ryan)
 * Notes:           Loot Generator
 *                      Procedure starts after the Map Generation Procedure
 *                      Parse through the Master Path and spawn loot where
 *                      applicable.
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class LootGenerator : MonoBehaviour
    {
        [SerializeField] private bool _enabled = true;

        [Header("Debug")]
        [SerializeField] private bool debug;

        private Path _masterPathReference;

        private void Awake()
        {
            // Subscribe to "Done" event in map generator and spawn loot after
            MapGeneratorController.OnGenerationDone += SpawnLoot;
        }

        private void SpawnLoot()
        {
            // Return if the Loot Generator is not enabled
            if (!_enabled)
                return;

            // Get reference to MasterPath in Map Generator
            _masterPathReference = MapGeneratorController.Instance?.MasterPath;

            // Return if the masterpath is not initialized
            if (_masterPathReference == null)
            {
                Debug.LogError("Loot Generator Error: Master Path was null.");
                return;
            }
            // Return if the masterpath does not contain any rooms
            if (_masterPathReference.Rooms.Count <= 0)
            {
                Debug.LogError("Loot Generator Error: Master Path has no rooms.");
                return;
            }

            if (debug) Debug.Log("Loot Generator: Loot Spawn Procedure has begun.");

            foreach (Room room in _masterPathReference.Rooms)
            {
                foreach (SpawnPad spawnPad in room.RoomSpawners)
                {
                    if (room.RoomType == RoomType.prize && spawnPad.type == PadType.chest)
                    {
                        spawnPad.SpawnObject();
                        if (debug) Debug.Log("Loot Generator: Chest spawned in " + room.name);
                    }
                }
            }

            if (debug) Debug.Log("Loot Generator: Loot Spawn Procedure has ended.");
        }
    }
}
