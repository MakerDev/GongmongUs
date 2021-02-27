using Assets.Scripts.MatchMaking;
using Assets.Scripts.MiniGames;
using Assets.Scripts.Networking;
using Assets.Scripts.RoomManagement;
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
        private GameObject _deathEffect;
        [SerializeField]
        private GameObject _spawnEffect;

        [SerializeField]
        private PlayerInfoUI _playerInfo;

        private bool _isFirstSetup = true;
        #endregion

        [SerializeField]
        private GameObject _chatHubPrefab;

        public string PlayerId { get { return $"{GameManager.PLAYER_ID_PREFIX}{netId}"; } }

        [SyncVar(hook = nameof(OnNameSet))]
        private string _playerName = "player";
        public string PlayerName { get { return _playerName; } private set { _playerName = value; } }

        private PlayerState _playerState = PlayerState.Student;
        public PlayerState State { get { return _playerState; } private set { _playerState = value; } }

        private PlayerShoot _playerShootComponent;
        private PlayerController _playerController; //For animations

        public bool IsStunning { get; private set; } = false;

        [SyncVar]
        private bool _isHost = false;
        public bool IsHost
        {
            get { return _isHost; }
            set { _isHost = value; }
        }

        public bool HasExited { get; private set; } = false;
        public bool CanStart { get; private set; } = false;

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
            GameManager.Instance.NofityPlayerStateChanged(this); //얘는 확실히 불린다는 거고.
            _playerShootComponent.NotifyStateChanged(newState);
            _playerController.SetStateMaterial(newState);

            if (isLocalPlayer)
            {
                _playerController.OnPlayerStateChange(newState);
            }

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
        /// <summary>
        /// Network Callback이 Start보다 더 빨라서 여기에 배치
        /// </summary>
        public override void OnStartServer()
        {
            base.OnStartServer();

            GameManager.Instance.RegisterPlayer(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            GameManager.Instance.RegisterPlayer(this);
            MatchID = MatchManager.Instance.Match.MatchID;

            //If this is local player, set player name manually because this script being called 'Start'
            //means that this is joining the existing room and other players' name will be automatically synced.
            if (isLocalPlayer)
            {
                LocalPlayer = this;

                string userName = UserManager.Instance.User.Name;
                string netId = GetComponent<NetworkIdentity>().netId.ToString();
                var newName = $"{userName}{netId}";

                SetName(newName);

                RoomUIManager.Instance?.RefreshList(GameManager.Instance.Players.Values);

                CmdSetMatchChecker(MatchManager.Instance.Match.MatchID.ToGuid());
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            BCNetworkManager.Instance.SendUser();
            CmdGetConnectionID(MatchManager.Instance.Match.MatchID);
            RoomUIManager.Instance?.RefreshList(GameManager.Instance.Players.Values);
        }

        [Command]
        private void CmdGetConnectionID(string matchID)
        {
            //TODO : 이것도 BCNetworkManager의 OnClientNotifyUser로 옮기기.
            MatchID = matchID;
            GameManager.Instance.ServerOnPlayerConnect(matchID, PlayerId);
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

        private void OnDestroy()
        {
            GameManager.Instance.UnRegisterPlayer(PlayerId);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            GameManager.Instance.PrintMessage($"{PlayerName} leaved", null, ChatType.Info);
        }
        #endregion

        #region GAME SETUP
        [Client]
        public void KickPlayer(string playerId)
        {
            if (IsHost == false)
            {
                return;
            }

            CmdKickPlayer(playerId);
        }

        [Command]
        public void CmdKickPlayer(string playerId)
        {
            GameManager.Instance.ServerKickPlayer(playerId);
            RpcOnClientKick();
        }

        [ClientRpc]
        public void RpcOnClientKick()
        {
            GameManager.Instance.RefreshPlayerList();
        }

        [Server]
        public void MakeHost()
        {
            IsHost = true;
            RpcBecomeHost();
        }

        [ClientRpc]
        public void RpcBecomeHost()
        {
            IsHost = true;

            if (isLocalPlayer)
            {
                GameManager.Instance.OnBecomeHost();
                GameManager.Instance.PrintMessage("You are the host now.", "SYSTEM", ChatType.None);
            }

            RoomUIManager.Instance.RefreshList(GameManager.Instance.Players.Values);
        }

        public void TryStartGame()
        {
            if (IsHost == false)
            {
                return;
            }

            CmdTryStartGame(MatchID);
        }

        [Command]
        private async void CmdTryStartGame(string matchID)
        {
            if (GameManager.Instance.ServerCanStartGame(matchID))
            {
                await StartGameByServerAsync(matchID);
            }
            else
            {
                RpcOnFailToStartGame();
            }
        }

        [ClientRpc]
        private void RpcOnFailToStartGame()
        {
            GameManager.Instance.PrintMessage("모든 플레이어가 레디해야 시작할 수 있습니다.", "SYSTEM", ChatType.None);
        }

        [Command]
        public async void CmdStartGame(string matchId)
        {
            await StartGameByServerAsync(matchId);
        }

        [Server]
        public async UniTask StartGameByServerAsync(string matchID)
        {
            var professorID = GameManager.Instance.GetRandomPlayerIdForMatch(matchID);
            await GameManager.Instance.ServerStartMatchAsync(matchID, professorID);

            RpcStartGame(professorID);
        }

        [ClientRpc]
        public void RpcStartGame(string professorId)
        {
            GameManager.Instance.ConfigureGameOnStart(professorId);
        }

        public void UnReady()
        {
            IsReady = false;
            CmdUnReady();
        }

        [Command]
        private void CmdUnReady()
        {
            IsReady = false;
            RpcUnReady();
        }

        [ClientRpc]
        public void RpcUnReady()
        {
            IsReady = false;
            CanStart = false;
            RoomUIManager.Instance?.RefreshList(GameManager.Instance.Players.Values);
        }

        public void GetReady()
        {
            IsReady = true;
            CmdGetReady(MatchID);
        }

        [Command]
        private void CmdGetReady(string matchID)
        {
            //Store match id for retrieving proper MissionManager on server.
            MatchID = matchID;
            //Hook이 순서대로 불려서 넣어야함
            IsReady = true;

            var canStartGame = GameManager.Instance.OnPlayerReady(matchID, this);

            RpcGetReady(canStartGame);
        }

        [ClientRpc]
        public void RpcGetReady(bool canStart)
        {
            MatchID = MatchManager.Instance.Match.MatchID;
            IsReady = true;
            CanStart = canStart;
            Debug.Log("CanStart : " + CanStart);

            RoomUIManager.Instance?.RefreshList(GameManager.Instance.Players.Values);
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
                MissionManager.Instance.NotifyPlayerCompleteMissions(this);

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
            //To avoid double send.
            if (State != PlayerState.Student)
            {
                return;
            }

            State = PlayerState.Assistant;
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
                PlayerSetup.PlayerUI?.OnCaughtByProfessor();

                GameManager.Instance.DisablePlayerControl();
                PlayTransitionEffect();
                GameManager.Instance.EnablePlayerControl();

                //미니게임하던 중간에 잡히면, 컨트롤이 이상해짐
                if (Interactable.EnteredInteractable != null && Interactable.EnteredInteractable.MiniGame != null)
                {
                    Interactable.EnteredInteractable.MiniGame.CancelMiniGame();
                }

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
            //To avoid Assistants and professor to be stunned.
            if (State != PlayerState.Student)
            {
                return;
            }

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
            if (State != PlayerState.Student)
            {
                return;
            }

            _playerController.SetOnCaughtByAssistant(false, true);

            if (isLocalPlayer)
            {
                GameManager.Instance.DisableMove();
                IsStunning = true;
            }

            await UniTask.Delay(PlayerController.STUN_AMOUNT_SEC * 1000);

            if (isLocalPlayer)
            {
                GameManager.Instance.EnableMove();
                IsStunning = false;
            }


            //Assistant에게 잡혀서 스턴에 걸린 와중에 교수가 와서 잡으면, await 3초후에 얘가 다시 Mat을 학생으로 돌려버리는 버그가 있어서
            //이렇게 해결함.
            if (State != PlayerState.Student)
            {
                _playerController.SetOnCaughtByAssistant(true, false);
            }
            else
            {
                _playerController.SetOnCaughtByAssistant(true, true);
            }
        }

        #endregion

        private void PlayTransitionEffect()
        {
            GameObject effectObject = Instantiate(_deathEffect, transform.position, Quaternion.identity);
            Destroy(effectObject, 1.5f);
            _playerController?.TransformToAssistant();
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

        //Called on client local player
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

            GameObject effectObject = Instantiate(_deathEffect, transform.position, Quaternion.identity);
            Destroy(effectObject, 1.5f);

            if (isLocalPlayer)
            {
                //GameManager.Instance.SetSceneCameraActive(true);
                GameManager.Instance.SetEscapedLocalPlayerActive(true);
                //TODO : 성공 표시로 바꾸기. 끄지 말고
                GetComponent<PlayerSetup>().PlayerUIInstance.SetActive(false);
                GameManager.Instance.SetExitDoorIndicator(false);
            }
        }
        #endregion

        #region SETTINGS
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
            PlayerName = newName;
            CmdChangePlayerName(newName);
        }

        [Command]
        public void CmdChangePlayerName(string newName)
        {
            PlayerName = newName;
            RpcChangePlayerName(newName);
        }

        [ClientRpc]
        public void RpcChangePlayerName(string newName)
        {
            PlayerName = newName;
            RoomUIManager.Instance.RefreshList(GameManager.Instance.Players.Values);

            if (isLocalPlayer)
            {
                //HACK
                PlayerSetup.PlayerUI?.SetLocalPlayerName(newName);
            }

            _playerInfo.SetPlayer(this);
            GameManager.Instance.RefreshPlayerList();
        }

        public void SetupPlayer()
        {
            if (isLocalPlayer)
            {
                GameManager.Instance.SetEscapedLocalPlayerActive(false);
                GetComponent<PlayerSetup>().PlayerUIInstance.SetActive(true);
            }

            //This is client side health regen
            //CurrentHealth = _maxHealth;
            CmdBroadCastNewPlayerSetUp();
        }

        [Command(ignoreAuthority = true)]
        private void CmdBroadCastNewPlayerSetUp()
        {
            RpcSetupPlayerOnAllClients();
        }

        [ClientRpc]
        private void RpcSetupPlayerOnAllClients()
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