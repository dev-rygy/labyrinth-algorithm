/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum RoomShape
{
    generalRoom,
    longRoom,
    tallRoom,
    bigRoom,
};

public enum RoomType
{
    small,
    start,
    prize,
    boss
}

public class Room : MonoBehaviour
{
    public Vector3 Position { get; private set; }
    //public bool[,] activeEntranceways { get; private set; }
    //public List<GameObject> entranceways { get; private set; }

    public RoomShape roomShape { get; private set; }
    public RoomType roomType { get; private set; }

    private void Awake()
    {
        //activeEntranceways = new bool[4, 6];
    }

    /*
    /// <summary>
    /// Activate an Entranceway in the entranceway list
    /// </summary>
    /// <param name="entranceNum"></param>
    private void ActivateEntranceway(int entranceNum)
    {
        entranceways[entranceNum].transform.GetChild(0).gameObject.SetActive(false); // Deactivate Wall
        entranceways[entranceNum].transform.GetChild(1).gameObject.SetActive(true); // Activate Entranceway
    }
    */
}
