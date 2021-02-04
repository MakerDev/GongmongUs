using cakeslice;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.MiniGames
{
    public class Interactable : MonoBehaviour
    {
        public static Interactable EnteredInteractable { get; private set; } = null;

        [SerializeField]
        private GameObject _miniGameObject;
        [SerializeField]
        private GameObject _original;
        [SerializeField]
        private GameObject _highlight;

        private Outline _outlineComponent;
        private MiniGame _miniGame;
        private int _localPlayerLayer;
        public bool Highlighted
        {
            get
            {
                if (_highlight != null)
                {
                    return _highlight.activeSelf;
                }

                if (_outlineComponent != null)
                {
                    return _outlineComponent.enabled;
                }

                return false;
            }
        }

        private void Start()
        {
            _localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");
            _miniGame = _miniGameObject.GetComponent<MiniGame>();
            _outlineComponent = _original.GetComponent<Outline>();

            if (_outlineComponent != null)
            {
                _outlineComponent.enabled = false;
            }
        }

        private void SetHighlight(bool turnOn)
        {
            if (_highlight != null)
            {
                _outlineComponent.color = 3;
                _highlight.SetActive(turnOn);
                _original.SetActive(!turnOn);
            }
            else
            {
                _outlineComponent.enabled = turnOn;
            }

            if (turnOn)
            {
                EnteredInteractable = this;
            }
            else
            {
                EnteredInteractable = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Player.LocalPlayer.State != PlayerState.Student)
            {
                return;
            }

            if (other.gameObject.layer == _localPlayerLayer && _miniGame.IsCompleted == false)
            {
                SetHighlight(true);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (Player.LocalPlayer.State != PlayerState.Student)
            {
                return;
            }

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

            if (Highlighted && Input.GetKeyDown(KeyCode.Space))
            {
                StartMiniGame();
            }
        }

        public void StartMiniGame()
        {
            _miniGameObject.SetActive(true);
        }
    }
}