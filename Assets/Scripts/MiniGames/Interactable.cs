using cakeslice;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.MiniGames
{
    public class Interactable : MonoBehaviour
    {
        [SerializeField]
        private GameObject _miniGameObject;
        [SerializeField]
        private GameObject _original;
        [SerializeField]
        private GameObject _highlight;

        private Outline _outlineComponent;
        private MiniGame _miniGame;
        private int _localPlayerLayer;

        private void Awake()
        {
            _localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");
            _miniGame = _miniGameObject.GetComponent<MiniGame>();

            _miniGame.OnLocalGameCompleted += (result) =>
            {
                if (result.Passed)
                {
                    var rigidBody = GetComponent<Rigidbody>();
                    rigidBody.isKinematic = true;
                }
            };
        }

        private void SetHighlight(bool turnOn)
        {
            if (_highlight != null)
            {
                _highlight.SetActive(turnOn);
                _original.SetActive(!turnOn);
            }
            else
            {
                _outlineComponent.enabled = turnOn;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _localPlayerLayer && _miniGame.IsCompleted == false)
            {
                SetHighlight(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _localPlayerLayer)
            {
                SetHighlight(false);
            }
        }

        private void Update()
        {
            if (_miniGameObject.activeSelf)
            {
                return;
            }

            if (_highlight.activeSelf && Input.GetKeyDown(KeyCode.Space))
            {
                _miniGameObject.SetActive(true);
            }
        }
    }
}