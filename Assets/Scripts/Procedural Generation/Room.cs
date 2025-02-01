/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/26/2024 (Ryan)
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    // Determines how much grid space a room will ocupy.
    public enum RoomShape
    {
        smallRoom,
        longRoom,
        tallRoom,
        bigRoom,
    }

    // Determines its gameplay and purpose.
    public enum RoomType
    {
        general,
        start,
        end,
        prize,
        toBoss,
        boss
    }

    public class Room : MonoBehaviour
    {
        [Header("Room Components")]
        [SerializeField] private List<Transform> _roomWalls;
        [SerializeField] public List<SpawnPad> RoomSpawners;

        [Header("Room Properties")]
        [SerializeField] public RoomShape roomShape;
        [SerializeField] public RoomType roomType;     // Just to show in inspector

        [Header("debug")]
        [SerializeField] private bool debug = false;

        [field: SerializeField] public Vector3 RoomDimensions { get; private set; } = Vector3.one;
        public RoomType RoomType { get; private set; }

        private bool[,] openEntracways;

        private void Awake()
        {
            // up to 4 possible unit spaces a room can take up; 6 possible faces on each unit
            // Index 1 = Bot-Left Unit (Origin)
            // Index 2 = Bot-Right Unit
            // Index 3 = Top-Right Unit
            // Index 4 = Top-Left Unit
            openEntracways = new bool[4, 6];

            RoomType = RoomType.general;
        }

        // Initialize the Room's entrances and loot
        public void Initialize(RoomType type)
        {
            RoomType = type;
            roomType = type;
            AcivateEntranceways();
        }

        /// <summary>
        /// Simply copy the bluePrint room's entranceway flags into the room's open entraceways.
        /// </summary>
        /// <param name="bluePrintArray">The blueprint room's entranceway array (6 possible entrances)</param>
        /// <param name="unitIndex">A specific unit space of the room in question</param>
        public void CopyBlueprintEntranceFlags(bool[] bluePrintArray, int unitIndex, Vector3 rotation)
        {
            bluePrintArray = HandleRotation(bluePrintArray, rotation);

            for (int i = 0; i < bluePrintArray.Length; i++) // iterate through all six faces of the Blueprint's flag array
            {
                openEntracways[unitIndex, i] = bluePrintArray[i]; // Copy into room array respectively
            }
        }

        /// <summary>
        /// Simply shift the values in the blueprint array around to handle a 90 degree rotation.
        /// </summary>
        /// <param name="bluePrintArray">The blueprint array</param>
        /// <param name="rotation">The angle of applied rotation</param>
        /// <returns></returns>
        private bool[] HandleRotation(bool[] bluePrintArray, Vector3 rotation)
        {
            if (rotation == Vector3.zero)        // If no rotation return
            {
                if (debug) Debug.Log($"Room {gameObject.name} was not rotated.");
                return bluePrintArray;
            }

            bool[] rotatedArray = new bool[bluePrintArray.Length];
            if (rotation.y == 90)      // If 90 degree rotation shift down
            {
                rotatedArray[0] = bluePrintArray[2];        // Positive X to Negative Z
                rotatedArray[1] = bluePrintArray[3];        // Negative X to Positive z
                rotatedArray[2] = bluePrintArray[1];        // Positive Z direction the same
                rotatedArray[3] = bluePrintArray[0];        // Negative Z direction the same
                rotatedArray[4] = bluePrintArray[4];        // Positive Y to Positive X
                rotatedArray[5] = bluePrintArray[5];        // Negative Y to Negative X
                if (debug) Debug.Log($"Room {gameObject.name} has been rotated by 90 degrees.");
            }
            else
                Debug.LogError($"Room Error: Room {gameObject.name} has been rotated incorrectly.");
            
            return rotatedArray;
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
            _roomWalls[entranceNum].GetChild(0).gameObject.SetActive(true);   // Activate Entranceway
            _roomWalls[entranceNum].GetChild(1).gameObject.SetActive(false);  // Deactivate Wall
        }
    }
}
