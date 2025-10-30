/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public abstract class BlueprintData
    {
        public string DataID { get; protected set; }

        public List<string> OutputPorts { get; protected set; }

        public BlueprintData()
        {
            OutputPorts = new List<string>();
        }

        public abstract void LoadIntoMemory();
    }
}
