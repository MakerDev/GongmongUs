using Assets.Scripts.MatchMaking;
using Assets.Scripts.Networking;
using BattleCampusMatchServer.Models;
using Mirror;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(PlayerSetup))]
    public class Player : NetworkBehaviour
    {
        public static Player LocalPlayer { get; private set; }

        #region SETUP
        [SerializeField]
        private Behaviour[] _disableOnDeath;
        [SerializeField]
        private GameObject[] _disableGameObjectsOnDeath;

        private bool[] _wasEnabled;

        [SerializeField]
        private GameObject _deatchEffect;
        [SerializeField]
        private GameObject _spawnEffect;

        [SerializeField]
        private PlayerInfoUI _playerInfo;

        private bool _isFirstSetup = true;
        #endregion

        public string PlayerId { get { return $"{GameManager.PLAYER_ID_PREFIX}{netId}"; } }

        [SyncVar(hook = nameof(OnNameSet))]
        private string _playerName = "player";
        public string PlayerName { get { return _playerName; } private set { _playerName = value; } }

        private PlayerState _playerState = PlayerState.Student;
        public PlayerState State { get { return _playerState; } private set { _playerState = value; } }

        [SerializeField]
        public GameObject _chatHubPrefab;

        private PlayerShoot _playerShootComponent;
        private PlayerController _playerController; //For animations

        public bool IsReady { get; private set; } = false;

        private void NotifyStateChanged(PlayerState newState)
        {
            GameManager.Instance.NofityPlayerStateChanged(this);
            _playerShootComponent.NotifyStateChanged(newState);
            _playerController.SetStateMaterial(newState);
            PlayerSetup.PlayerUI.SetState();
        }

        private void Start()
        {
            _playerInfo.SetPlayer(this);
            _playerShootComponent = GetComponent<PlayerShoot>();
            _playerController = GetComponent<PlayerController>();
        }

        #region CONNECTIONS
        public override void OnStartServer()
        {
            base.OnStartServer();
            //To avoid overriding synced value, only set this on server
            //TODO : set state here instead
            //_currentHealth = _maxHealth;
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            CmdGetConnectionID();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            //If this is local player, set player name manually because this script being called 'Start'
            //means that this is joining the existing room and other players' name will be automatically synced.
            if (isLocalPlayer)
            {
                LocalPlayer = this;

                string userName = UserManager.Instance.User.Name;
                string netId = GetComponent<NetworkIdentity>().netId.ToString();
                var newName = $"{userName}{netId}";
                SetName(newName);

                //GameManager.Instance.CmdPrintMessage($"{newName} joined!", null, ChatType.Info);
                //TODO : Report
                CmdSetMatchChecker(MatchManager.Instance.Match.MatchID.ToGuid());
            }


            if (UserManager.Instance.User.IsHost && isLocalPlayer)
            {
                CmdSpawnChatHub(MatchManager.Instance.Match.MatchID.ToGuid());
            }
        }

        public override void OnStopClient()
        {
            GameManager.Instance.PrintMessage($"{PlayerName} leaved", null, ChatType.Info);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            base.OnStopClient();
        }

        [Command]
        private void CmdGetConnectionID()
        {
            TargetGetConnectionID(connectionToClient.connectionId);
        }

        [TargetRpc]
        private void TargetGetConnectionID(int connectionID)
        {
            BCNetworkManager.Instance.NotifyUserConnect(connectionID, UserManager.Instance.User);
        }

        #endregion

        public void StartGame()
        {
            CmdStartGame(GameManager.Instance.GetRandomPlayerId());
        }

        [Command]
        private void CmdStartGame(string professorID)
        {
            RpcStartGame(professorID);
        }

        [ClientRpc]
        private async void RpcStartGame(string professorId)
        {
            if (UserManager.Instance.User.IsHost)
            {
                await BCNetworkManager.Instance.NotifyStartGame(MatchManager.Instance.Match.MatchID);
            }

            GameManager.Instance.ConfigureGameOnStart(professorId);
        }

        public void GetReady()
        {
            CmdGetReady();
        }

        [Command]
        private void CmdGetReady()
        {
            IsReady = true;
            RpcGetReady();
        }

        [ClientRpc]
        private void RpcGetReady()
        {
            IsReady = true;
            Debug.Log("Im ready");
        }

        public void CaughtByProfessor()
        {
            CmdCaughByProfessor();
        }

        [Command(ignoreAuthority = true)]
        private void CmdCaughByProfessor()
        {
            State = PlayerState.Assistant;
            RpcCaughtByProfessor();
        }

        [ClientRpc]
        private void RpcCaughtByProfessor()
        {
            //TODO : Display transformation effct and animation.
            State = PlayerState.Assistant;
            NotifyStateChanged(State);

            if (isLocalPlayer)
            {
                GameManager.Instance.DisablePlayerControl();
                PlayTransitionEffect();
                GameManager.Instance.EnablePlayerControl();
            }
            else
            {
                PlayTransitionEffect();
            }
        }

        private void PlayTransitionEffect()
        {
            GameObject effectObject = Instantiate(_deatchEffect, transform.position, Quaternion.identity);
            Destroy(effectObject, 1.5f);
            _playerController.TransformToAssistant();
        }


        #region SET STATE
        public void SetState(PlayerState state)
        {
            State = state;

            if (isLocalPlayer)
            {
                CmdSetState(state);
            }
        }

        [Command]
        private void CmdSetState(PlayerState state)
        {
            State = state;
            RpcSetState(state);
        }

        [ClientRpc]
        private void RpcSetState(PlayerState state)
        {
            State = state;
            NotifyStateChanged(state);
        }
        #endregion
        private void SuccessExit()
        {
            //Disable components
            for (int i = 0; i < _disableOnDeath.Length; i++)
            {
                _disableOnDeath[i].enabled = false;
            }

            for (int i = 0; i < _disableGameObjectsOnDeath.Length; i++)
            {
                _disableGameObjectsOnDeath[i].SetActive(false);
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            GameObject effectObject = Instantiate(_deatchEffect, transform.position, Quaternion.identity);
            Destroy(effectObject, 1.5f);

            Debug.Log(transform.name + " has exited!");

            if (isLocalPlayer)
            {
                GameManager.Instance.SetSceneCameraActive(true);
                GetComponent<PlayerSetup>().PlayerUIInstance.SetActive(false);
            }
        }
        #region SETTINGS
        [Command]
        public void CmdSpawnChatHub(Guid matchGuid)
        {
            Debug.Log($"Spawning hub for {matchGuid}");
            var chatHub = Instantiate(_chatHubPrefab);
            NetworkServer.Spawn(chatHub);
            chatHub.GetComponent<NetworkMatchChecker>().matchId = matchGuid;
        }

        [Command]
        public void CmdSetMatchChecker(Guid matchGuid)
        {
            var matchChecker = GetComponent<NetworkMatchChecker>();
            matchChecker.matchId = matchGuid;
        }

        public void OnNameSet(string _, string newName)
        {
            if (isLocalPlayer)
            {
                //HACK
                PlayerSetup.PlayerUI?.SetLocalPlayerName(newName);
            }

            _playerInfo.SetPlayer(this);
            GameManager.Instance.RefreshPlayerList();
        }

        public void SetName(string newName)
        {
            CmdChangePlayerName(newName);
        }

        [Command]
        public void CmdChangePlayerName(string newName)
        {
            _playerName = newName;
        }

        public void PlayerSetUp()
        {
            if (isLocalPlayer)
            {
                GameManager.Instance.SetSceneCameraActive(false);
                GetComponent<PlayerSetup>().PlayerUIInstance.SetActive(true);
            }

            //This is client side health regen
            //CurrentHealth = _maxHealth;
            CmdBroadCastNewPlayerSetUp();
        }

        [Command(ignoreAuthority = true)]
        private void CmdBroadCastNewPlayerSetUp()
        {
            RpcSetupPlayeronAllClients();
        }

        [ClientRpc]
        private void RpcSetupPlayeronAllClients()
        {
            if (_isFirstSetup)
            {
                _wasEnabled = new bool[_disableOnDeath.Length];

                for (int i = 0; i < _disableOnDeath.Length; i++)
                {
                    _wasEnabled[i] = _disableOnDeath[i].enabled;
                }


                _isFirstSetup = false; ;
            }

            SetDefaults();
        }

        public void SetDefaults()
        {
            for (int i = 0; i < _disableOnDeath.Length; i++)
            {
                _disableOnDeath[i].enabled = _wasEnabled[i];
            }

            for (int i = 0; i < _disableGameObjectsOnDeath.Length; i++)
            {
                _disableGameObjectsOnDeath[i].SetActive(true);
            }

            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = true;
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = true;
            }
        }
        #endregion
    }
}