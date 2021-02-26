using Assets.Scripts.MatchMaking;
using Assets.Scripts.MiniGames;
using Assets.Scripts.Networking;
using Assets.Scripts.RoomManagement;
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Assets.Scripts
{
    public class ServerMatchInfo
    {
        public bool Started { get; set; } = false;
        public List<Player> Players { get; set; } = new List<Player>();
    }

    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;
        public MatchSetting MatchSetting;

        private const int MAX_MESSAGES = 10;
        private readonly List<ChatMessage> _messages = new List<ChatMessage>();

        #region SCENE UIs
        [SerializeField]
        private GameObject _escapedLocalPlayer;
        [SerializeField]
        private GameObject _menuCanvas;
        [SerializeField]
        private InputField _renameInputField;

        [SerializeField]
        private GameObject _chatPanel;
        [SerializeField]
        private GameObject _textObjectPrefab;
        [SerializeField]
        private InputField _chatInputField;
        [SerializeField]
        private TextMeshProUGUI _matchNameText;
        [SerializeField]
        private TextMeshProUGUI _gameInfoMatchNameText;

        [SerializeField]
        private GameObject _playerListPanel;
        [SerializeField]
        private GameObject _playerListItemPrefab;

        [SerializeField]
        private GameObject _minimapUI;
        [SerializeField]
        private GameObject _startGameUI;

        [SerializeField]
        private MiniMap _minimapOnTab;
        [SerializeField]
        private MiniMap _staticMinimap;
        #endregion
        [SerializeField]
        private Button _readyButton;
        [SerializeField]
        private Button _unreadyButton;
        [SerializeField]
        private Toggle _bgmToggle;
        [SerializeField]
        private Toggle _fullscreenToggle;

        [SerializeField]
        private GameObject _gameLobbyUI;

        [SerializeField]
        private GameObject _exitDoorIndicator;

        private List<GameObject> _playerListItems = new List<GameObject>();
        public bool GameStarted { get; private set; } = false;

        public Dictionary<string, Player> Players { get; private set; } = new Dictionary<string, Player>();

        /// <summary>
        /// Key : match ID
        /// </summary>
        public Dictionary<string, ServerMatchInfo> ServerPlayersOfMatch { get; private set; } = new Dictionary<string, ServerMatchInfo>();

        /// <summary>
        /// The number of remaining students;
        /// </summary>
        public int StudentCount { get; private set; }
        public int ExitedStudents { get; private set; } = 0;

        private bool _disableControl = true;
        public bool DisableControl { get { return _disableControl || IsChating; } private set { _disableControl = value; } }
        public bool IsMenuOpen { get; private set; } = false;
        public bool IsChating
        {
            get
            {
                return _chatInputField != null && _chatInputField.isFocused;
            }
        }

#if UNITY_WEBGL
        private const KeyCode MENU_KEY = KeyCode.LeftControl;
#else
        private const KeyCode MENU_KEY = KeyCode.Escape;
#endif
        private const KeyCode MINIMAP_KEY = KeyCode.Tab;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Multiple GameManagers in a scene");
            }
            else
            {
                Instance = this;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _menuCanvas.SetActive(false);
            _gameLobbyUI.SetActive(true);

            //At first, mouse needs to be enabled to press start button.
            DisablePlayerControl();
            GameStarted = false;

            _matchNameText.text = $"<{MatchManager.Instance.Match.Name}>";
            _gameInfoMatchNameText.text = $"<{MatchManager.Instance.Match.Name}>";

            _readyButton.gameObject.SetActive(true);

            _bgmToggle.isOn = SoundManager.Instance.IsPlayingBGM;
            _fullscreenToggle.isOn = Screen.fullScreenMode == FullScreenMode.FullScreenWindow;

            //TODO : change this to another BGM.
            SoundManager.Instance.StopBGM();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            SyncConnectionsToMatchServer();
        }

        public void DisableMove()
        {
            DisableControl = true;
        }

        public void EnableMove()
        {
            DisableControl = false;
        }

        public void DisablePlayerControl()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DisableControl = true;

            //TODO : Set mouse pointer poinsion to the center of screen.
        }

        public void EnablePlayerControl()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            DisableControl = false;
        }

        [Server]
        private async void SyncConnectionsToMatchServer()
        {
            var connectionIds = Players.Values.Select(p => p.netId).ToList();

            await BCNetworkManager.Instance.SyncConnectionIdsAsync(connectionIds);
            UniTask.Delay(30000).ContinueWith(() => SyncConnectionsToMatchServer());
        }

        private void Update()
        {
            if (isServer)
            {
                return;
            }

            HandleChat();

            if (GameStarted == false)
            {
                return;
            }

            if (Input.GetKeyDown(MENU_KEY))
            {
                if (_menuCanvas.activeSelf)
                {
                    Resume();
                }
                else if (MiniGame.IsPlaying == false)
                {
                    OpenMenu();
                }

                return;
            }

            if (Input.GetKeyDown(MINIMAP_KEY))
            {
                if (_minimapUI.activeSelf)
                {
                    Resume();
                }
                else if (MiniGame.IsPlaying == false)
                {
                    OpenMiniMap();
                }
            }
        }

        public void ReadyGame()
        {
            Player.LocalPlayer.GetReady();
            _readyButton.enabled = false;
            _readyButton.interactable = false;
            _readyButton.gameObject.SetActive(false);
            _unreadyButton.gameObject.SetActive(true);
            _unreadyButton.interactable = true;
        }

        public void UnReadyGame()
        {
            Player.LocalPlayer.UnReady();
            _readyButton.gameObject.SetActive(true);
            _readyButton.enabled = true;
            _readyButton.interactable = true;

            _unreadyButton.interactable = false;
            _unreadyButton.gameObject.SetActive(false);
        }

        private bool ServerCanStartGame(string matchId)
        {
            var hasMatch = ServerPlayersOfMatch.TryGetValue(matchId, out var matchInfo);

            if (hasMatch == false)
            {
                return false;
            }

            //Can not double start.
            //This happens when late joiner is kicked out.
            if (matchInfo.Started)
            {
                return false;
            }

            if (matchInfo.Players.Count < 3)
            {
                return false;
            }

            foreach (var player in matchInfo.Players)
            {
                if (player.IsReady == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Notify when player is ready
        /// </summary>
        /// <param name="matchID"></param>
        /// <param name="player"></param>
        /// <returns>Whether to start game</returns>
        [Server]
        public bool OnPlayerReady(string matchID, Player player)
        {
            var hasMatch = ServerPlayersOfMatch.TryGetValue(matchID, out var players);

            if (hasMatch == false)
            {
                //TODO : 플레이어 다 내쫓아..?
                return false;
            }

            Debug.Log($"Player {player.PlayerId} is ready for {matchID}");

            return ServerCanStartGame(matchID);
        }

        [Server]
        public void ServerCompleteMatch(string matchId)
        {
            ServerPlayersOfMatch.Remove(matchId);
        }

        public async UniTask ServerStartMatchAsync(string matchID, string professorID)
        {
            var serverInfo = ServerPlayersOfMatch[matchID];

            serverInfo.Started = true;

            var serverMissionManager = BCNetworkManager.Instance.SpawnMissionManager(matchID);
            serverMissionManager.ConfigureForServer(matchID, professorID);
            await BCNetworkManager.Instance.NotifyStartGame(matchID);
        }

        [Server]
        public bool ServerOnPlayerConnect(string matchID, string playerId)
        {
            ServerMatchInfo serverMatchInfo;

            if (ServerPlayersOfMatch.ContainsKey(matchID) == false)
            {
                serverMatchInfo = new ServerMatchInfo();
                ServerPlayersOfMatch.Add(matchID, serverMatchInfo);
            }
            else
            {
                serverMatchInfo = ServerPlayersOfMatch[matchID];
            }

            var player = GetPlayer(playerId);

            //If this match has already started, disconnect this player.
            if (serverMatchInfo.Started)
            {
                Debug.LogError($"Disconnect {playerId} as match already started.");
                player.connectionToClient.Disconnect();
                return false;
            }

            serverMatchInfo.Players.Add(player);

            BCNetworkManager.Instance.SpawnChatHubIfNotExists(matchID);

            return true;
        }

        [Client]
        public void ConfigureGameOnStart(string professorId)
        {
            //이건 이미 한 번 이게 실행되고 또 실행됐다는 거니까 뭔가 문제있음
            if (GameStarted || _gameLobbyUI == null)
            {
                Debug.LogError("Game lobby manager is null");
                return;
            }

            Debug.Log("Proper execution");

            _startGameUI.SetActive(false);
            _gameLobbyUI.SetActive(false);
            Destroy(_gameLobbyUI);

            _minimapOnTab.ReigsterPlayerObjects(Players.Values);
            _staticMinimap.ReigsterPlayerObjects(Players.Values);

            foreach (var player in Players.Values)
            {
                if (player.PlayerId == professorId)
                {
                    player.SetState(PlayerState.Professor);
                }
                else
                {
                    player.SetState(PlayerState.Student);
                }
            }

            StudentCount = Players.Count - 1;
            ExitedStudents = 0;

            GameStarted = true;

            SoundManager.Instance.SetBGM("SoYoung");

            EnablePlayerControl();

            Debug.Log($"You're {Player.LocalPlayer.State}");
        }

        public void NofityPlayerStateChanged(Player player)
        {
            _staticMinimap.NotifyStateChanged(player);
            _minimapOnTab.NotifyStateChanged(player);

            if (player.State == PlayerState.Assistant)
            {
                StudentCount -= 1;
            }
        }


        #region MENU
        public void OpenMenu()
        {
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(true);

                DisablePlayerControl();
            }
        }

        public void SetBGMOnOff(bool _)
        {
            if (SoundManager.Instance.IsPlayingBGM == false)
            {
                SoundManager.Instance.PlayBGM();
            }
            else
            {
                SoundManager.Instance.MuteSound();
            }
        }

        public void SetFullScreen(bool _)
        {
            if (Screen.fullScreen)
            {
                Screen.fullScreenMode = FullScreenMode.Windowed;
            }
            else
            {
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
            }
        }

        #endregion
        public void Resume()
        {
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(false);

                IsMenuOpen = false;
            }

            if (_minimapUI != null)
            {
                _minimapUI.SetActive(false);
            }

            if (GameStarted)
            {
                EnablePlayerControl();
            }
        }

        private void OpenMiniMap()
        {
            _minimapUI.SetActive(true);

            DisablePlayerControl();
        }

        public void SetEscapedLocalPlayerActive(bool isActive)
        {
            if (_escapedLocalPlayer == null)
            {
                return;
            }

            _escapedLocalPlayer.SetActive(isActive);
        }

        public void RenameLocalPlayer()
        {
            var newName = _renameInputField.text;

            if (string.IsNullOrEmpty(newName) || newName.Length >= 8)
            {
                PrintMessage("이름은 빈칸이거나 8자 이상이어서는 안됩니다.", "SYSTEM");
                return;
            }

            if (Player.LocalPlayer.PlayerName == newName)
            {
                PrintMessage("현재 이름입니다.", "SYSTEM");
                return;
            }

            Player.LocalPlayer.SetName(_renameInputField.text);
        }

        #region CHAT
        private void HandleChat()
        {
            var enterDown = Input.GetKeyDown(KeyCode.Return);

            if (_chatInputField.text != "")
            {
                if (enterDown)
                {
                    if (ChatHub.Instance != null)
                    {
                        ChatHub.Instance.BroadcastMessage(_chatInputField.text, Player.LocalPlayer.PlayerName, ChatType.Player);
                    }

                    _chatInputField.text = "";
                    _chatInputField.DeactivateInputField();
                }
            }
            else
            {

                if (!_chatInputField.isFocused && enterDown)
                {
                    _chatInputField.ActivateInputField();
                }
            }

            if (_chatInputField.isFocused && enterDown)
            {
                _chatInputField.DeactivateInputField();
            }

            //만약 isFocus인데 아무것도 입력안하면? -> Deactivate
        }

        [Client]
        public void SetExitDoorIndicator(bool isActive)
        {
            if (_exitDoorIndicator != null)
            {
                _exitDoorIndicator.SetActive(isActive);
            }
        }

        [Client]
        public void PrintMessage(string message, string sender, ChatType chatType = ChatType.None)
        {
            if (_messages.Count >= MAX_MESSAGES)
            {
                Destroy(_messages[0].TextObject.gameObject);
                _messages.RemoveAt(0);
            }

            var chatMessage = new ChatMessage();
            chatMessage.Message = message;
            chatMessage.Sender = sender;
            chatMessage.TextObject = Instantiate(_textObjectPrefab, _chatPanel.transform).GetComponent<TextMeshProUGUI>();

            var hasSender = string.IsNullOrEmpty(sender);

            if (chatType != ChatType.None)
            {
                chatMessage.ChatType = chatType;
            }
            else
            {
                chatMessage.ChatType = hasSender ? ChatType.KillInfo : ChatType.Player;
            }

            chatMessage.TextObject.text = hasSender ? message : $"{sender}: {message}";
            chatMessage.TextObject.color = GetMessageColor(chatMessage.ChatType);

            _messages.Add(chatMessage);
        }

        private Color GetMessageColor(ChatType chatType)
        {
            switch (chatType)
            {
                case ChatType.Info:
                    return Color.green;

                case ChatType.Player:
                    return Color.black;

                case ChatType.KillInfo:
                    return Color.red;
                default:
                    break;
            }

            return Color.black;
        }
        #endregion

        public string GetRandomPlayerId()
        {
            var index = UnityEngine.Random.Range(0, Players.Count);

            return Players.Values.ToArray()[index].PlayerId;
        }

        public string GetRandomPlayerIdForMatch(string matchId)
        {
            var players = ServerPlayersOfMatch[matchId].Players;

            var index = UnityEngine.Random.Range(0, players.Count);

            return players[index].PlayerId;
        }

        #region PLAYER TRACKING
        public const string PLAYER_ID_PREFIX = "Player";

        /// <summary>
        /// Key : PlayerId == transform.name
        /// </summary>
        public void RegisterPlayer(Player player)
        {
            string playerId = player.PlayerId;
            Players.Add(playerId, player);
            player.transform.name = playerId;

            if (isClient)
            {
                RefreshPlayerList();
                RoomUIManager.Instance?.RefreshList(Players.Values);
            }

            Debug.Log($"Client : {playerId} is registered. Now {Players.Count} players");
        }

        public async void UnRegisterPlayer(string playerId)
        {
            if (playerId == null)
            {
                return;
            }

            var hasPlayer = Players.TryGetValue(playerId, out var player);

            if (hasPlayer == false)
            {
                Debug.LogError($"Failed to unregister {playerId} as it doesn't exist");
                return;
            }

            Players.Remove(playerId);

            if (isClient)
            {
                RefreshPlayerList();
                RoomUIManager.Instance?.RefreshList(Players.Values);
            }

            _minimapOnTab.RemovePlayer(player);
            _staticMinimap.RemovePlayer(player);

            Debug.Log($"{playerId} is removed. Now {Players.Count} players");

            if (isServer == false)
            {
                return;
            }

            //If Server
            if (player.MatchID == null)
            {
                Debug.LogError($"Player NetID : {netId} | PlayerID : {playerId} had null match ID.");
                return;
            }

            var hasMatch = ServerPlayersOfMatch.TryGetValue(player.MatchID, out var matchInfo);

            if (hasMatch == false)
            {
                return;
            }

            matchInfo.Players.Remove(player);

            if (matchInfo.Players.Count <= 0)
            {
                ServerPlayersOfMatch.Remove(player.MatchID);
                BCNetworkManager.Instance.TryRemoveChatHub(player.MatchID);
            }

            if (ServerCanStartGame(player.MatchID))
            {
                await matchInfo.Players[0].StartGameByServerAsync(player.MatchID);
                Debug.Log("Start by exiting.");
            }
        }

        public void RefreshPlayerList()
        {
            foreach (var item in _playerListItems)
            {
                Destroy(item);
            }

            _playerListItems.Clear();

            foreach (var player in Players.Values)
            {
                var playerListItem = Instantiate(_playerListItemPrefab, _playerListPanel.transform);
                _playerListItems.Add(playerListItem);
                var playerListItemText = playerListItem.GetComponent<TextMeshProUGUI>();
                playerListItemText.text = player.PlayerName;

                if (player.isLocalPlayer)
                {
                    playerListItemText.color = Color.blue;
                }
            }
        }

        public Player GetPlayer(string playerId)
        {
            if (Players.ContainsKey(playerId))
            {
                return Players[playerId];
            }

            throw new System.Exception($"No player with ID {playerId} is found");
        }

        private void OnDestroy()
        {
            DisablePlayerControl();
        }
        #endregion
    }
}