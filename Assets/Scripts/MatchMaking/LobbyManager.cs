using Assets.Scripts.MatchMaking;
using BattleCampusMatchServer.Models.DTOs;
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts.Networking
{
    /// <summary>
    /// 룸 매니저의 역할은, 유저가 룸(매치)를 Host or Join하게 하여
    /// 적절한 MatchGuid를 부여받고 해당룸으로 이동하게 하는 것이다. 
    /// 게임씬(또는 게임 Ready 씬)부터는 GameManager 같은 애들이 handle하는 것이고, 씬이 전환될때, RoomManager는 굳이 필요가 없다.
    /// 얘는 그냥 서버에만 있고, LobbyPlayer가 Create랑 Join을 호출하게만 하면 될 거 같은데..?
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [SerializeField]
        private GameObject _matchUIPrefab;
        [SerializeField]
        private GameObject _matchUIPanel;
        [SerializeField]
        private Toggle _fullScreenToggle;
        [SerializeField]
        private Toggle _setBgmToggle;

        private List<GameObject> _matchUIInstances = new List<GameObject>();

        [SerializeField]
        private Canvas _createMatchCanvas;
        [SerializeField]
        private Canvas _createTourCanvas;
        [SerializeField]
        private TMP_InputField _newMatchNameInputField;
        [SerializeField]
        private TMP_InputField _newTourNameInputField;

        [SerializeField]
        private Button _createMatchButton;

        [SerializeField]
        private Canvas _loadingCanvas;

        [Header("Debug")]
        [SerializeField]
        private TextMeshProUGUI _requestResultText;

        [SerializeField]
        private GameObject _menuObject;

        private List<GameObject> _matchUIs = new List<GameObject>();

        private UniTask _fetchTask;
        private bool _fetchRecursively = true;

        private void Start()
        {
            Instance = this;

            _requestResultText.text = "";

            FetchRecursive();

            MenuManager.RegisterToggles(_setBgmToggle, _fullScreenToggle);

            SoundManager.Instance.SetBGM("SweetCampusLobbyBGM");
        }

        //TODO : implement cancellation
        private void OnDestroy()
        {
            _fetchRecursively = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _menuObject.SetActive(!_menuObject.activeSelf);
            }
        }

        public void TurnOffMenu()
        {
            _menuObject.SetActive(false);
        }

        private async void FetchRecursive()
        {
            while (_fetchRecursively)
            {
                _fetchTask = FetchAllMatchesAsync();

                await _fetchTask;
                await UniTask.Delay(4500);
            }
        }

        public async void RefreshLobby()
        {
            await FetchAllMatchesAsync();
        }

        public async UniTask FetchAllMatchesAsync()
        {
            List<MatchDTO> matches;

            try
            {
                matches = await MatchServer.Instance.GetAllMatchesAsync();
            }
            catch (Exception)
            {
                _requestResultText.text = "Failed to fetch data. Server might not be running";
                return;
            }

            if (matches == null)
            {
                return;
            }

            foreach (var matchUIInstance in _matchUIInstances)
            {
                Destroy(matchUIInstance);
            }

            _matchUIInstances.Clear();
            _matchUIs.Clear();

            foreach (var matchDto in matches)
            {
                AddMatch(matchDto);
            }
        }

        public void CreateGameMatch()
        {
            CreateNewMatch(MatchType.GameMode);
        }

        public void CreateTourMatch()
        {
            CreateNewMatch(MatchType.TourMode);
        }

        public async void CreateNewMatch(MatchType matchType)
        {
            _loadingCanvas.enabled = true;
            _createMatchButton.enabled = false;

            var matchName = _newMatchNameInputField.text;

            if (matchType == MatchType.TourMode)
            {
                matchName = _newTourNameInputField.text;
            }

            if (string.IsNullOrEmpty(matchName))
            {
                _requestResultText.text = "Match name cannot be empty";
                CloseCreateMatchPrompt();
                return;
            }

            _newMatchNameInputField.text = "";
            _newTourNameInputField.text = "";

            try
            {
                await CreateNewMatchAsync(matchName, matchType);
            }
            catch (Exception)
            {
                _requestResultText.text = "ERROR : Server might be down";
            }
            finally
            {
                CloseCreateMatchPrompt();
            }
        }

        public async UniTask JoinMatchAsync(MatchDTO match)
        {
            try
            {
                var result = await MatchServer.Instance.JoinMatchAsync(match.IpPortInfo, match.MatchID, UserManager.Instance.User);

                if (result.JoinSucceeded == false)
                {
                    Debug.LogError(result.JoinFailReason);
                    _requestResultText.text = result.JoinFailReason;
                    return;
                }
                UserManager.Instance.User.IsHost = false;
                MoveToMatch(result.Match);
            }
            catch (Exception)
            {
                _requestResultText.text = "ERROR : Server might be down";
            }
        }

        public void MoveToMatch(MatchDTO match)
        {
            //Configure MatchManager
            UserManager.Instance.User.MatchID = match.MatchID;
            MatchManager.Instance.ConfigureMatchInfo(match);

            if (match.MatchType == MatchType.GameMode)
            {
                SceneManager.LoadScene("GameScene");
            }
            else
            {
                SceneManager.LoadScene("TourGameScene");
            }
        }

        public async UniTask CreateNewMatchAsync(string matchName, MatchType matchType)
        {
            var result = await MatchServer.Instance.CreateMatchAsync(matchName, matchType);

            if (result.IsCreationSuccess == false)
            {
                _requestResultText.text = result.CreationFailReason;
                return;
            }

            _requestResultText.text = "";

            UserManager.Instance.User.IsHost = false;
            MoveToMatch(result.Match);
        }

        public void OpenCreateMatchPrompt()
        {
            _createMatchCanvas.enabled = true;
        }

        public void OpenCreateTourPrompt()
        {
            _createTourCanvas.enabled = true;
        }

        public void CloseCreateMatchPrompt()
        {
            _createMatchButton.enabled = true;
            _loadingCanvas.enabled = false;
            _createMatchCanvas.enabled = false;
            _createTourCanvas.enabled = false;
        }

        private void AddMatch(MatchDTO match)
        {
            var matchUIInstance = Instantiate(_matchUIPrefab, _matchUIPanel.transform);
            var matchUI = matchUIInstance.GetComponent<MatchUI>();

            _matchUIInstances.Add(matchUIInstance);
            matchUI.UpdateInfo(match);

            _matchUIs.Add(matchUIInstance);
        }
    }
}