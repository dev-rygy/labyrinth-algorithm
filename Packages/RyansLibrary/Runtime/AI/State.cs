/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/04/2025
 * Notes:           Abstract interface for states that entities
 *                  can utalize with a state machine
*/
namespace RyansLibrary.StateMachines
{
    public abstract class State
    {
        /// <summary>
        /// Enter should be called right when the state is created;
        /// subscribe to any events here
        /// </summary>
        public abstract void Enter();

        /// <summary>
        /// Tick is called every frame from the statemachine
        /// </summary>
        /// <param name="deltaTime">Time per frame</param>
        public abstract void Tick(float deltaTime);

        /// <summary>
        /// Exit is called right before a state is switched;
        /// unsubscribe to events here
        /// </summary>
        public abstract void Exit();
    }
}
