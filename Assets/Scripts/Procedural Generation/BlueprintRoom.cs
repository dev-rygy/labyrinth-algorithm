/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           A blueprint room is a pseudo room that only holds the 
 *                  data of where a room will potentially be instatiated
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the properties of a suedo room that does not actually exist in the world.
/// Is meant to be replaced by actual rooms later on.
/// </summary>
public class BlueprintRoom
{
    public string roomName;
    public Vector3 position;
    public bool[] activeEntranceways;

    // Constructor
    public BlueprintRoom(Vector3 postion)
    {
        position = postion;
        activeEntranceways = new bool[6];
    }
}
