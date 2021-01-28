using Assets.Scripts.MatchMaking;
using BattleCampusMatchServer.Models;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts
{

    public class UserManager : MonoBehaviour
    {
        public const string LOGIN_ADDRESS = "https://ysweb.yonsei.ac.kr/ysbus_main.jsp";
        public static UserManager Instance;

        public GameUser User { get; private set; } = new GuestUser();

        [SerializeField]
        private InputField _idInputField;
        [SerializeField]
        private InputField _passwordInputField;
        [SerializeField]
        private List<Button> _loginButtons = new List<Button>();
        [SerializeField]
        private Text _loginResultText;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
                return;
            }

            Destroy(this.gameObject);
        }

        public void ResetMatchInfo()
        {
            User.MatchID = null;
            User.IsHost = false;
            User.ConnectionID = -1;
        }

        public async void Login()
        {
            foreach (var button in _loginButtons)
            {
                button.enabled = false;
            }

            var id = _idInputField.text;
            var password = _passwordInputField.text;
            var loginSuccess = await LoginAsync(id, password);

            if (loginSuccess)
            {
                User.ID = Guid.NewGuid();
                User.StudentID = _idInputField.text;
                SceneManager.LoadScene("Lobby");
            }
            else
            {
                _loginResultText.text = "Failed to login.";
                Debug.LogError("Failed to login");                
            }

            foreach (var button in _loginButtons)
            {
                button.enabled = true;
            }
        }

        public void LoginAsGuest()
        {
            User = new GuestUser();

            SceneManager.LoadScene("Lobby");
        }

        private async UniTask<bool> LoginAsync(string userid, string password)
        {
            try
            {
                return await MatchServer.Instance.LoginPortal(new LoginForm
                {
                    UserId = userid,
                    Password = password,
                });
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void LogOut()
        {
            //Return to login scene
            User.StudentID = null;
            SceneManager.LoadScene("Login");
        }
    }
}