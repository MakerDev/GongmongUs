using Assets.Scripts;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerSetup : NetworkBehaviour
    {
        private const string DONT_DRAW_LAYER_NAME = "DontDraw";

        [HideInInspector]
        public GameObject PlayerUIInstance;

        [SerializeField]
        private Behaviour[] _componentsToDiable;
        [SerializeField]
        private string _remoteLayerName = "RemotePlayer";

        [SerializeField]
        private GameObject _playerUIPrefab;
        [SerializeField]
        private GameObject _playerGraphics;

        private static PlayerUI _playerUI;
        public static PlayerUI PlayerUI
        {
            get { return _playerUI; }
            private set { _playerUI = value; }
        }

        private void Start()
        {
            string netId = GetComponent<NetworkIdentity>().netId.ToString();
            Player player = GetComponent<Player>();

            GameManager.Instance.RegisterPlayer(netId, player);

            if (!isLocalPlayer)
            {
                foreach (var component in _componentsToDiable)
                {
                    component.enabled = false;
                }

                Utils.SetLayerRecursive(gameObject, LayerMask.NameToLayer(_remoteLayerName));
            }
            else
            {
                //If Local player
                PlayerUIInstance = Instantiate(_playerUIPrefab);
                PlayerUIInstance.name = _playerUIPrefab.name;

                Utils.SetLayerRecursive(_playerGraphics, LayerMask.NameToLayer(DONT_DRAW_LAYER_NAME));

                ConfigurePlayerUI();

                GetComponent<Player>().PlayerSetUp();

                Debug.Log("PlayerSetUp: Called player setup on server? " + isServer);
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ConfigurePlayerUI()
        {                
            //Configure PlayerUI
            _playerUI = PlayerUIInstance.GetComponent<PlayerUI>();
            if (_playerUI == null)
            {
                Debug.LogError("No PlayerUI Component");
            }

            _playerUI.SetController(GetComponent<PlayerController>());

            Player player = GetComponent<Player>();

            _playerUI.SetPlayer(player);
            _playerUI.SetLocalPlayerName($"Player{netId}");
        }

        private void OnDisable()
        {
            Destroy(PlayerUIInstance);

            if (isLocalPlayer)
            {
                GameManager.Instance.SetSceneCameraActive(true);                
            }

            GameManager.Instance.UnRegisterPlayer(transform.name);
        }
    }
}
