using RyansLibrary.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blink : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Vector3 blinkDistance;
    [SerializeField] private bool useAltInteract;

    private GameObject _playerReference;
    private Vector3 _targetPos;
    private bool _canBlink = false;

    [SerializeField] private bool _debug = false;

    private void Start()
    {
        if (useAltInteract)
            InputManager.OnInteract2 += Teleport;
        else
            InputManager.OnInteract1 += Teleport;

        _playerReference = PlayerController.Instance.gameObject;
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

        _playerReference.GetComponent<CharacterController>().enabled = false;
        _playerReference.transform.position = _targetPos;
        _playerReference.GetComponent<CharacterController>().enabled = true;

        _canBlink = false;

        if (_debug) Debug.Log("Player has blinked to " + _targetPos);
    }
}
