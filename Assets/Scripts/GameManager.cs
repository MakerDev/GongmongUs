using Assets.Scripts.MatchMaking;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance;
        public MatchSetting MatchSetting;

        [SerializeField]
        private GameObject _sceneCamera;
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
        private Text _matchNameText;

        [SerializeField]
        private GameObject _playerListPanel;
        [SerializeField]
        private GameObject _playerListItemPrefab;
        private List<GameObject> _playerListItems = new List<GameObject>();

        private static Dictionary<string, Player> _players = new Dictionary<string, Player>();

        public bool IsMenuOpen { get; private set; } = false;

        private const int MAX_MESSAGES = 10;
        private List<ChatMessage> _messages = new List<ChatMessage>();

#if UNITY_WEBGL
        private const KeyCode MENU_KEY = KeyCode.LeftControl;
#else
        private const KeyCode MENU_KEY = KeyCode.Escape;
#endif

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

        private void Start()
        {
            _menuCanvas.SetActive(false);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _matchNameText.text = $"<{MatchManager.Instance.Match.Name}>";
        }

        private void Update()
        {
            HandleCursor();

            HandleChat();

            if (Input.GetKeyDown(MENU_KEY))
            {
                if (_menuCanvas.activeSelf)
                {
                    Resume();
                }
                else
                {
                    OpenMenu();
                }
            }
        }

        private void HandleCursor()
        {
            //TODO : find more intelligent way
            if (IsMenuOpen || MiniGame.IsPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void HandleChat()
        {
            if (_chatInputField.text != "")
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    if (ChatHub.Instance != null)
                    {
                        ChatHub.Instance.PrintMessage(_chatInputField.text, Player.LocalPlayer.PlayerName, ChatType.Player);
                    }

                    _chatInputField.text = "";
                    _chatInputField.DeactivateInputField();
                }
            }
            else
            {
                if (!_chatInputField.isFocused && Input.GetKeyDown(KeyCode.Return))
                {
                    _chatInputField.ActivateInputField();
                }
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
            chatMessage.TextObject = Instantiate(_textObjectPrefab, _chatPanel.transform).GetComponent<Text>();

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

        public void SetSceneCameraActive(bool isActive)
        {
            if (_sceneCamera == null)
            {
                return;
            }

            _sceneCamera.SetActive(isActive);
        }

        public void OpenMenu()
        {
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                IsMenuOpen = true;
            }
        }

        public void Resume()
        {
            if (_menuCanvas != null)
            {
                _menuCanvas.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                IsMenuOpen = false;
            }
        }

        public void RenameLocalPlayer()
        {
            Player.LocalPlayer.SetName(_renameInputField.text);
        }

        #region PLAYER TRACKING
        public const string PLAYER_ID_PREFIX = "Player";

        /// <summary>
        /// Key : PlayerId == transform.name
        /// </summary>
        public void RegisterPlayer(string netId, Player player)
        {
            string playerId = PLAYER_ID_PREFIX + netId;
            _players.Add(playerId, player);
            player.transform.name = playerId;

            if (isClient)
            {
                RefreshPlayerList();
            }

            Debug.Log($"Client : {playerId} is registered");
        }

        public void UnRegisterPlayer(string playerId)
        {
            _players.Remove(playerId);

            if (isClient)
            {
                RefreshPlayerList();
            }

            Debug.Log($"Client : {playerId} is removed");
        }

        public void RefreshPlayerList()
        {
            foreach (var item in _playerListItems)
            {
                Destroy(item);
            }

            _playerListItems.Clear();

            foreach (var player in _players.Values)
            {
                var playerListItem = Instantiate(_playerListItemPrefab, _playerListPanel.transform);
                _playerListItems.Add(playerListItem);
                var playerListItemText = playerListItem.GetComponent<Text>();
                playerListItemText.text = player.PlayerName;
            }
        }

        public Player GetPlayer(string playerId)
        {
            if (_players.ContainsKey(playerId))
            {
                return _players[playerId];
            }

            throw new System.Exception($"No player with ID {playerId} is found");
        }

        //private void OnGUI()
        //{
        //    GUILayout.BeginArea(new Rect(200, 200, 200, 200));
        //    GUILayout.BeginVertical();

        //    foreach (var playerId in _players.Keys)
        //    {
        //        GUILayout.Label(playerId + " - " + _players[playerId].transform.name);
        //    }

        //    GUILayout.EndVertical();
        //    GUILayout.EndArea();
        //}
        #endregion
    }
}