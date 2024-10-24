/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/23/2024 
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary
{
    // Determines how much grid space a room will ocupy.
    public enum RoomShape
    {
        smallRoom,
        longRoom,
        tallRoom,
        bigRoom,
    };

    // Determines its gameplay and purpose.
    public enum RoomType
    {
        general,
        start,
        prize,
        boss
    }

    public class Room : MonoBehaviour
    {
        [SerializeField] private List<Transform> roomWalls;
        [SerializeField] public RoomShape roomShape;

        public Vector3 Position { get; private set; }
        public RoomType roomType { get; private set; }

        private bool[,] openEntracways;

        private void Awake()
        {
            // up to 4 possible unit spaces a room can take up; 6 possible faces on each unit
            openEntracways = new bool[4, 6];
        }

        public void Initialize(RoomType type)
        {
            roomType = type;
            //AcivateEntranceways();
        }

        /// <summary>
        /// Simply copy the bluePrint room's entranceway flags into the room's open entraceways.
        /// </summary>
        /// <param name="bluePrintArray">The blueprint room's entranceway array (6 possible entrances)</param>
        /// <param name="unitIndex">A specific unit space of the room in question</param>
        public void CopyBlueprintRoomEntranceFlags(bool[] bluePrintArray, int unitIndex)
        {
            for (int i = 0; i < bluePrintArray.Length; i++) // iterate through all six faces of the Blueprint's flag array
            {
                openEntracways[unitIndex, i] = bluePrintArray[i]; // Copy into room array respectively
            }
        }

        /// <summary>
        /// When called will activate all entranceways that have been flagged as open in the openEntranceways array
        /// </summary>
        private void AcivateEntranceways()
        {
            int enListIdx = 0;              // iterator for activeEntranceway List
            for (int i = 0; i < 4; i++)     // iterate through 4 possible unit spaces
            {
                for (int j = 0; j < 6; j++)     // iterate through the faces of each unit
                {
                    enListIdx = (i * 6) + j;
                    if (openEntracways[i, j] == true)   // Activate entrance if true in activeEntranceway List
                        ActivateEntranceway(enListIdx);
                }
            }
        }

        /// <summary>
        /// Activate an Entranceway in the entranceway list
        /// </summary>
        /// <param name="entranceNum"></param>
        private void ActivateEntranceway(int entranceNum)
        {
            roomWalls[entranceNum].GetChild(0).gameObject.SetActive(false); // Deactivate Wall
            roomWalls[entranceNum].GetChild(1).gameObject.SetActive(true); // Activate Entranceway
        }
    }
}
