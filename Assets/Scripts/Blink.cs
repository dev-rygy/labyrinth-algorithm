/*
 * Created By:      Ryan Carpenter
 * Date Created:    12/17/2024
 * Last Modified:   12/17/2024 
 * Notes:           Teleports player to desired location
 *                  used for ladders and trapdoors
*/

using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary
{
    public class Blink : MonoBehaviour
    {
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private Vector3 blinkDistance;
        [SerializeField] private bool useAltInteract;

        private PlayerStateMachine _playerReference;
        private Vector3 _targetPos;
        private bool _canBlink = false;

        [SerializeField] private bool _debug = false;

        private void Start()
        {
            if (useAltInteract)
                OldInputhandler.OnInteract2 += Teleport;
            else
                OldInputhandler.OnInteract1 += Teleport;

            OldInputhandler.OnInteract1 += ResetTP;
            OldInputhandler.OnInteract2 += ResetTP;

            _playerReference = PlayerStateMachine.Instance;
            if (_debug) Debug.Log("Player reference set to " + _playerReference);

            _targetPos = transform.position + blinkDistance;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_debug) Debug.Log("Object on layer" + other.gameObject.layer + " has entered.");

            if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
                return;

            _canBlink = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (_debug) Debug.Log("Object on layer" + other.gameObject.layer + " has exited.");

            if ((playerLayer.value & (1 << other.gameObject.layer)) == 0)
                return;

            _canBlink = false;
        }

        private void Teleport()
        {
            if (!_canBlink)
                return;

            _canBlink = false;

            _playerReference.GetComponent<CharacterController>().enabled = false;
            _playerReference.transform.position = _targetPos;
            _playerReference.GetComponent<CharacterController>().enabled = true;

            if (_debug) Debug.Log("Player has blinked to " + _targetPos);
        }

        private void ResetTP()
        {
            _canBlink = false;
        }
    }
}
