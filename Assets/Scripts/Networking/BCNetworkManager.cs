using Assets.Scripts.MatchMaking;
using BattleCampusMatchServer.Models;
using Cysharp.Threading.Tasks;
using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.Networking
{
    public class BCNetworkManager : NetworkManager
    {
        [SerializeField]
        private GameObject _gamePlayerPrefab;

        public static BCNetworkManager Instance { get; private set; }

        public Dictionary<Guid, GameObject> MissionManagers { get; private set; } = new Dictionary<Guid, GameObject>();

        private string _serverName = null;

        //This is for server instance. It's not correctly configured on the client side.
        private IpPortInfo _ipPortInfo = new IpPortInfo();

        private IpPortInfo ConfigureIpPortInfo()
        {
            var arguments = Environment.GetCommandLineArgs();

            for (int i = 0; i < arguments.Length; i++)
            {
                var arg = arguments[i];

                if (arg == "-ip")
                {
                    _ipPortInfo.IpAddress = arguments[i + 1];
                }
                else if (arg == "-desktopPort")
                {
                    _ipPortInfo.DesktopPort = int.Parse(arguments[i + 1]);
                }
                else if (arg == "-websocketPort")
                {
                    _ipPortInfo.WebsocketPort = int.Parse(arguments[i + 1]);
                }
                else if (arg == "-serverName")
                {
                    _serverName = arguments[i + 1];
                }
            }

            return _ipPortInfo;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Instance = this;
        }

        public override async void OnStartServer()
        {
            base.OnStartServer();

            Instance = this;

            var serverIpPortInfo = ConfigureIpPortInfo();

            Debug.Log("IP : " + serverIpPortInfo.IpAddress);

            var name = _serverName == null ? $"Server:{serverIpPortInfo.IpAddress}" : _serverName;

            var result = await MatchServer.Instance.RegisterServerAsync(name, serverIpPortInfo);
            //TODO : do proper error handling
            if (result == false)
            {
                Debug.LogError("Failed to register to match server");
            }
        }

        public async void NotifyUserConnect(int connectionId, GameUser user)
        {
            user.ConnectionID = connectionId;

            await MatchServer.Instance.NotifyUserConnect(MatchManager.Instance.Match.IpPortInfo, connectionId, user);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            //반드시 불러줘야한다!!
            base.OnClientConnect(conn);

            //If the match has already started, disconnect
            //GameManager말고 매치서버에서 확인해야될듯
            //if (GameManager.Instance.GameStarted)
            //{
            //    Debug.LogError("disconnect as game already started");
            //    conn.Disconnect();                
            //    //TODO: Report MatchServer.
            //}
        }

        public async UniTask NotifyStartGame(string matchID)
        {
            await MatchServer.Instance.NotifyMatchStarted(_ipPortInfo, matchID);
        }

        //This is called on server. 
        public override async void OnServerDisconnect(NetworkConnection conn)
        {
            await MatchServer.Instance.NotifyUserDisconnect(_ipPortInfo, conn.connectionId);
            Debug.Log($"Disconnected user {conn.connectionId}");

            base.OnServerDisconnect(conn);
        }

        //Don't reset MatchManager, UserManager
        //리셋하면 게임 재시작이 불가능해짐
        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            //UserManager.Instance.ResetMatchInfo();
            //MatchManager.Instance.ResetMatchInfo();
        }

        public override async void OnStopServer()
        {
            await MatchServer.Instance.TurnOffServerAsync(_ipPortInfo);

            base.OnStopServer();
        }

        [Server]
        public void SpawnMissionManager(Guid matchId)
        {
            var missionManager = Instantiate(spawnPrefabs[1]);
            missionManager.GetComponent<NetworkMatchChecker>().matchId = matchId;


            if (MissionManagers.ContainsKey(matchId))
            {
                MissionManagers[matchId] = missionManager;
            }
            else
            {
                MissionManagers.Add(matchId, missionManager);
            }

            NetworkServer.Spawn(missionManager);
        }

        [Server]
        public void CompleteMatch(Guid matchId)
        {
            //TODO : report result to Match Server

            //Destroy MissionManager
            var hasManager = MissionManagers.TryGetValue(matchId, out var missionManager);

            if (hasManager)
            {
                NetworkServer.Destroy(missionManager);
            }
        }

        [Client]
        public void MoveToResultScene()
        {
            //Re-enable mouse controls
            GameManager.Instance.DisablePlayerControl();

            offlineScene = "MatchResultScene";
            StopClient();
        }

        [Client]
        public void ExitGame()
        {
            offlineScene = "Lobby";
            StopClient();
        }
    }
}