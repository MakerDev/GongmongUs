using cakeslice;
using Mirror;
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
        public MiniGame MiniGame { get; private set; }

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
            MiniGame = _miniGameObject.GetComponent<MiniGame>();
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
        }

        private void OnTriggerEnter(Collider other)
        {
            if (ShouldActivate() == false)
            {
                return;
            }

            if (other.gameObject.layer == _localPlayerLayer && MiniGame.IsCompleted == false)
            {
                SetHighlight(true);
                EnteredInteractable = this;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (ShouldActivate() == false)
            {
                return;
            }

            if (other.gameObject.layer == _localPlayerLayer)
            {
                SetHighlight(false);
                EnteredInteractable = null;
            }
        }

        private bool ShouldActivate()
        {
            if (Player.LocalPlayer == null)
            {
                return false;
            }

            if (MiniGame.IsCompleted)
            {
                return false;
            }

            var isForLocalPlayer = MiniGame.AssignedPlayer != null && MiniGame.AssignedPlayer.isLocalPlayer == true;

            if (isForLocalPlayer && Player.LocalPlayer.State == PlayerState.Student)
            {
                SetHighlight(true);
                return true;
            }

            return false;
        }

        private void Update()
        {
            if (ShouldActivate() == false)
            {
                return;
            }

            if (_miniGameObject.activeSelf)
            {
                return;
            }

            //if (Highlighted && input.getkeydown(keycode.space))
            //{
            //    startminigame();
            //}
        }

        public void StartMiniGame()
        {
            _miniGameObject.SetActive(true);
        }
    }
}