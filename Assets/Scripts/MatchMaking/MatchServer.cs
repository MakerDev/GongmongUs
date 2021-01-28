using BattleCampusMatchServer.Models;
using BattleCampusMatchServer.Models.DTOs;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.VR;

namespace Assets.Scripts.MatchMaking
{
    public class ServerUserDTO
    {
        public IpPortInfo IpPortInfo { get; set; }
        public GameUser User { get; set; }
    }

    public class LoginForm
    {
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// Hub for Match API Server
    /// </summary>
    public class MatchServer
    {
#if UNITY_STANDALONE_LINUX || UNITY_WEBGL
        private const string BASE_ADDRESS = "https://battlecampusmatchserver.azurewebsites.net/api/";
#else
        private const string BASE_ADDRESS = "https://localhost:4001/api/";
#endif
        private static MatchServer _instance = null;
        public static MatchServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new MatchServer();
                }
                return _instance;
            }
        }

        public async UniTask<List<MatchDTO>> GetAllMatchesAsync()
        {
            var result = await UnityWebRequest.Get(BASE_ADDRESS + "matches").SendWebRequest();

            if (result.isHttpError || result.isNetworkError)
            {
                return null;
            }

            return JsonConvert.DeserializeObject<List<MatchDTO>>(result.downloadHandler.text);
        }

        public async UniTask<MatchCreationResultDTO> CreateMatchAsync(string name)
        {
            var userJson = JsonConvert.SerializeObject(UserManager.Instance.User);
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}matches/create?name={name}", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(userJson));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");

            var response = await request.SendWebRequest();
            var matchCreationResultString = response.downloadHandler.text;

            return JsonConvert.DeserializeObject<MatchCreationResultDTO>(matchCreationResultString);
        }

        //TODO: chagne all signitures
        public async UniTask<MatchJoinResultDTO> JoinMatchAsync(IpPortInfo ipPortInfo, string matchID, GameUser user)
        {
            var serverUser = new ServerUserDTO
            {
                User = user,
                IpPortInfo = ipPortInfo,
            };
            var serverUserJson = JsonConvert.SerializeObject(serverUser);
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}matches/join?matchID={matchID}", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(serverUserJson));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");

            var response = await request.SendWebRequest();
            var matchJoinResultString = response.downloadHandler.text;

            return JsonConvert.DeserializeObject<MatchJoinResultDTO>(matchJoinResultString);
        }

        public async UniTask NotifyUserConnect(IpPortInfo ipPortInfo, int connectionID, GameUser user)
        {
            user.ConnectionID = connectionID;

            var serverUser = new ServerUserDTO
            {
                User = user,
                IpPortInfo = ipPortInfo,
            };

            var serverUserJson = JsonConvert.SerializeObject(serverUser);
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}matches/notify/connect?connectionID={connectionID}", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(serverUserJson));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();
        }

        public async UniTask<bool> LoginPortal(LoginForm loginForm)
        {
            var formJson = JsonConvert.SerializeObject(loginForm);
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}user/portal/login", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(formJson));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.responseCode == 200)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //This also acts as notification of player exiting game.
        public async UniTask NotifyUserDisconnect(IpPortInfo ipPortInfo, int connectionID)
        {
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}matches/notify/disconnect?connectionID={connectionID}", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ipPortInfo)));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
        }

        public async UniTask<bool> RegisterServerAsync(string serverName, IpPortInfo ipPortInfo, int maxMatches = 5)
        {
            var jsonContent = JsonConvert.SerializeObject(ipPortInfo);
            var request = UnityWebRequest.Post($"{BASE_ADDRESS}server/register/{serverName}?maxMatches={maxMatches}", "");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonContent));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");

            var response = await request.SendWebRequest();

            return (!response.isNetworkError && !response.isHttpError);
        }

        public async UniTask TurnOffServerAsync(IpPortInfo ipPortInfo)
        {
            var request = UnityWebRequest.Delete($"{BASE_ADDRESS}server/turnoff/{ipPortInfo}");
            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ipPortInfo)));
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            Debug.Log($"Unregister server {ipPortInfo}");
        }
    }
}
