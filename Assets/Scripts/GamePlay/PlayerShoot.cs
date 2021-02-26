using Assets.Scripts;
using Assets.Scripts.GamePlay.PlayerActions;
using Mirror;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerShoot : NetworkBehaviour
    {
        private const string PLAYER_TAG = "Player";

        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private LayerMask _layerMask;
        [SerializeField]
        private LayerMask _remotePlayerLayer;

        private PlayerController _playerController;

        [SerializeField]
        private Transform _raycastTransform;
        private IPlayerAction _mainFireAction;

        private Player _player;

        private void Start()
        {
            if (_camera == null)
            {
                Debug.LogError("PlayerShoot : No camera reference");
                this.enabled = false;
            }
            _player = GetComponent<Player>();
            _playerController = GetComponent<PlayerController>();
        }

        public void NotifyStateChanged(PlayerState state)
        {
            if (state == PlayerState.Professor)
            {
                _mainFireAction = new CatchAction(_raycastTransform, _remotePlayerLayer, _playerController);                
            }
            else if (state == PlayerState.Student)
            {
                _mainFireAction = new StartMiniGameAction();
            }
            else
            {
                _mainFireAction = new StunByAssignmentAction(_raycastTransform, _remotePlayerLayer, _playerController);

                if (_player == null)
                {
                    _player = GetComponent<Player>();
                }

                GameManager.Instance.PrintMessage($"{_player.PlayerName} is now assistant.", "SYSTEM", ChatType.Info);
            }

            if (isLocalPlayer)
            {
                var cooltimeAction = _mainFireAction as CooltimeAction;

                if (cooltimeAction != null)
                {
                    PlayerSetup.PlayerUI.SetCooltimeAction(cooltimeAction);
                }
            }
        }

        private void Update()
        {
            if (GameManager.Instance.DisableControl || _mainFireAction == null || isServer)
            {
                return;
            }

            if (!isLocalPlayer || GameManager.Instance.IsMenuOpen || MiniGame.IsPlaying)
            {
                return;
            }

            var canExecute = _mainFireAction.CanExecute();
            PlayerSetup.PlayerUI.SetCrossHair(canExecute);

            if (Input.GetButtonDown("Fire1"))
            {
                _mainFireAction.TryExecute();
            }
        }
    }
}