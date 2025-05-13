using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator
{
    // ***** Path Containers *****
    // The Master Path holds a reference to all bluprint rooms in an zone
    public Path MasterPath { get; private set; }

    // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
    // Keys are in room coords
    public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }

    public RoomGenerator(Path masterPath, Dictionary<Vector3Int, BlueprintRoom> masterDictionary)
    {
        MasterPath = masterPath;
        MasterDictionary = masterDictionary;
    }


}
