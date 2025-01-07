/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/07/2025
 * Last Modified:   01/07/2025
 * Notes:           Interface that is used by the player's state machine,
 *                      contains a universal execute method to fire the ability
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbility
{
    public void Execute(CharacterController playerController);
}
