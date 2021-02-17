using Assets.Scripts.MatchMaking;
using Assets.Scripts.Networking;
using BattleCampusMatchServer.Models;
using cakeslice;
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(PlayerSetup))]
    public class Player : NetworkBehaviour
    {
        public static Player LocalPlayer { get; private set; }

        #region SETUP
        [SerializeField]
        private Behaviour[] _disableOnExit;
        [SerializeField]
        private GameObject[] _disableGameObjectsOnExit;

        private bool[] _wasEnabled;

        [SerializeField]
        private GameObject _deatchEffect;
        [SerializeField]
        private GameObject _spawnEffect;

        [SerializeField]
        private PlayerInfoUI _playerInfo;

        private bool _isFirstSetup = true;
        #endregion

        [SerializeField]
        public GameObject _chatHubPrefab;

        public string PlayerId { get { return $"{GameManager.PLAYER_ID_PREFIX}{netId}"; } }

        [SyncVar(hook = nameof(OnNameSet))]
        private string _playerName = "player";
        public string PlayerName { get { return _playerName; } private set { _playerName = value; } }

        private PlayerState _playerState = PlayerState.Student;
        public PlayerState State { get { return _playerState; } private set { _playerState = value; } }

        private PlayerShoot _playerShootComponent;
        private PlayerController _playerController; //For animations

        public bool HasExited { get; private set; } = false;

        [SyncVar]
        private bool _isReady = false;
        public bool IsReady { get { return _isReady; } private set { _isReady = value; } }
        /// <summary>
        /// This is for server to identify which match this player belongs to.
        /// </summary>
        public string MatchID { get; private set; }

        public List<MiniGame> AssignedMissions { get; private set; } = new List<MiniGame>();
        public int MissionsLeft { get; private set; } = 1;

        [Client]
        private void NotifyStateChanged(PlayerState newState)
        {
            GameManager.Instance.NofityPlayerStateChanged(this);
            _playerShootComponent.NotifyStateChanged(newState);
            _playerController.SetStateMaterial(newState);
            PlayerSetup.PlayerUI.SetState();

            if (newState != PlayerState.Student)
            {
                MissionManager.Instance.RemovePlayer(PlayerId);
            }
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
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            CmdGetConnectionID();
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

                CmdSetMatchChecker(MatchManager.Instance.Match.MatchID.ToGuid());
            }

            //HACK : 호스트가 소환하는 거였는데, 이제 호스트 없어
            if (GameManager.Instance.Players.Count <= 1 && isLocalPlayer)
            {
                CmdSpawnChatHub(MatchManager.Instance.Match.MatchID.ToGuid());
            }
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            //Reevaluate mission manager
            if (MatchID == null)
            {
                return;
            }

            var missionManager = BCNetworkManager.Instance.GetMissionManager(MatchID);

            if (missionManager == null)
            {
                return;
            }

            missionManager.OnPlayerDisconnectGame(this);
        }

        private void OnDisable()
        {
            GameManager.Instance.UnRegisterPlayer(PlayerId);
        }

        public override void OnStopClient()
        {
            GameManager.Instance.PrintMessage($"{PlayerName} leaved", null, ChatType.Info);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            base.OnStopClient();
        }
        #endregion

        #region GAME SETUP
        public void StartGame()
        {
            CmdStartGame(GameManager.Instance.GetRandomPlayerId(), MatchManager.Instance.Match.MatchID);
        }

        [Command]
        private async void CmdStartGame(string professorID, string matchId)
        {
            var serverMissionManager = BCNetworkManager.Instance.SpawnMissionManager(matchId);
            serverMissionManager.ConfigureForServer(matchId, professorID);
            await BCNetworkManager.Instance.NotifyStartGame(matchId);

            RpcStartGame(professorID);
        }

        [ClientRpc]
        private void RpcStartGame(string professorId)
        {
            GameManager.Instance.ConfigureGameOnStart(professorId);
        }

        public void GetReady()
        {
            MatchID = MatchManager.Instance.Match.MatchID;
            CmdGetReady(MatchID);
        }

        [Command]
        private void CmdGetReady(string matchID)
        {
            //Store match id for retrieving proper MissionManager on server.
            MatchID = matchID;
            IsReady = true;
            RpcGetReady();
        }

        [ClientRpc]
        private void RpcGetReady()
        {
            MatchID = MatchManager.Instance.Match.MatchID;
            IsReady = true;

            GameManager.Instance.PrintMessage($"{PlayerName} is now ready!", "SYSTEM", ChatType.Info);

            //TODO : Update RoomPlayerUI
        }
        #endregion

        #region MISSIONS
        public void AssignMissions(IEnumerable<MiniGame> missions)
        {
            AssignedMissions.AddRange(missions);

            foreach (var assignedMission in AssignedMissions)
            {
                assignedMission.AssignPlayer(this);
            }

            MissionsLeft = AssignedMissions.Count;
            UpdatePlayerMissionProgress();
        }

        public void OnCompleteMission(MiniGameResult miniGameResult)
        {
            if (miniGameResult.Passed)
            {
                MissionsLeft -= 1;
                UpdatePlayerMissionProgress();
                CmdReportMissionComplete(MissionsLeft);
            }
        }

        [Command]
        public void CmdReportMissionComplete(int missionsLeft)
        {
            MissionsLeft = missionsLeft;

            if (missionsLeft == 0)
            {
                var serverMissionManager = BCNetworkManager.Instance.GetMissionManager(MatchID);
                serverMissionManager.ServerNotifyPlayerCompleteMissions(PlayerId);
            }

            RpcReportMissionComplete(MissionsLeft);
        }

        [ClientRpc]
        private void RpcReportMissionComplete(int missionsLeft)
        {
            MissionsLeft = missionsLeft;

            if (MissionsLeft == 0)
            {
                MissionManager.Instance.NotifyPlayerCompleteMissions(PlayerId);

                if (isLocalPlayer)
                {
                    //TODO: Display all missions for the local player are completed.
                    UpdatePlayerMissionProgress();
                }
            }
        }

        //Display mission progress for this player
        private void UpdatePlayerMissionProgress()
        {
            PlayerSetup.PlayerUI.SetPlayerMissionProgress(AssignedMissions.Count, AssignedMissions.Count - MissionsLeft);
        }
        #endregion


        #region CaughtBy
        public void CaughtByProfessor()
        {
            CmdCaughByProfessor();
        }

        [Command(ignoreAuthority = true)]
        private void CmdCaughByProfessor()
        {
            State = PlayerState.Assistant;
            var serverMissionManager = BCNetworkManager.Instance.GetMissionManager(MatchID);
            serverMissionManager.ServerOnPlayerCaught(PlayerId);
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

                //MissionManager.Instance.OnPlayerCaught(PlayerId);
                MissionManager.Instance.RemovePlayer(PlayerId);
            }
            else
            {
                PlayTransitionEffect();
            }
        }

        //No need to sync because while the local player is trying to solve the given assignment,
        //that player character will not move.
        public void CaughtByAssistant()
        {
            CmdCaughtByAssistant();
        }

        [Command(ignoreAuthority = true)]
        private void CmdCaughtByAssistant()
        {
            RpcCaughtByAssistant();
        }

        [ClientRpc]
        private async void RpcCaughtByAssistant()
        {
            _playerController.SetOnCaughtByAssistant(false);

            if (isLocalPlayer)
            {
                GameManager.Instance.DisableMove();
            }

            await UniTask.Delay(PlayerController.STUN_AMOUNT_SEC * 1000);

            if (isLocalPlayer)
            {
                GameManager.Instance.EnableMove();
            }

            _playerController.SetOnCaughtByAssistant(true);
        }

        #endregion

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

        #region ESCAPE
        public void Escape()
        {
            //TODO : Disable main camera and enable scene camera.
            HasExited = true;
            CmdEscape();
        }

        [Command]
        private void CmdEscape()
        {
            HasExited = true;

            var serverMissionManager = BCNetworkManager.Instance.GetMissionManager(MatchID);
            serverMissionManager.ServerOnPlayerEscape(PlayerId);
            RpcEscape();
        }

        [ClientRpc]
        private void RpcEscape()
        {
            GameManager.Instance.PrintMessage($"{PlayerName} has exited!", "SYSTEM", ChatType.Info);
            HasExited = true;

            MissionManager.Instance.RemovePlayer(PlayerId);

            if (isLocalPlayer)
            {
                SuccessExit();
            }
            else
            {
                //Disable
                this.gameObject.SetActive(false);
            }
        }

        private void SuccessExit()
        {
            HasExited = true;

            //Disable components
            for (int i = 0; i < _disableOnExit.Length; i++)
            {
                _disableOnExit[i].enabled = false;
            }

            for (int i = 0; i < _disableGameObjectsOnExit.Length; i++)
            {
                _disableGameObjectsOnExit[i].SetActive(false);
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

            if (isLocalPlayer)
            {
                GameManager.Instance.SetSceneCameraActive(true);
                GetComponent<PlayerSetup>().PlayerUIInstance.SetActive(false);
            }
        }
        #endregion
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

        public void SetupPlayer()
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
                _wasEnabled = new bool[_disableOnExit.Length];

                for (int i = 0; i < _disableOnExit.Length; i++)
                {
                    _wasEnabled[i] = _disableOnExit[i].enabled;
                }


                _isFirstSetup = false; ;
            }

            SetDefaults();
        }

        public void SetDefaults()
        {
            for (int i = 0; i < _disableOnExit.Length; i++)
            {
                _disableOnExit[i].enabled = _wasEnabled[i];
            }

            for (int i = 0; i < _disableGameObjectsOnExit.Length; i++)
            {
                _disableGameObjectsOnExit[i].SetActive(true);
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