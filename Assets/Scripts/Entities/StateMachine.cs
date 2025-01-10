/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
 * Notes:           State machine class is instatiated on any
 *                      entity that utalizes a statemachine
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.StateMachine
{
    /// <summary>
    /// Keeps track of the currentState; updates all state's tick method; handles state
    /// switching.
    /// </summary>
    public abstract class StateMachine : MonoBehaviour
    {
        // TODO: Use a pushdown automita to implement previous state handling if necessary

        // A reference to the current state the player is in
        private State _currentState;

        [field: Header("Debug")]
        [field: SerializeField] public bool DebugStateMachine { get; protected set; }

        // Update is called once per frame
        void Update()
        {
            // Update the current state every frame and pass in delta time
            _currentState?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 1.) Call the Exit method of the prev state
        /// 2.) Switch the state to the new state
        /// 3.) Call the Enter method of the new state
        /// </summary>
        /// <param name="newState">The state to switch to</param>
        public void SwitchState(State newState)
        {
            _currentState?.Exit();
            if (DebugStateMachine && _currentState != null) Debug.Log("State " + _currentState + " Exited");
            _currentState = newState;
            _currentState?.Enter();
            if (DebugStateMachine && _currentState != null) Debug.Log("State " + _currentState + " Entered");
        }
    }
}
