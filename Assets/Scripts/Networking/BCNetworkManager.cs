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
        public static BCNetworkManager Instance { get; private set; }

        public Dictionary<string, GameObject> MissionManagers { get; private set; } = new Dictionary<string, GameObject>();
        public Dictionary<string, GameObject> ChatHubs { get; private set; } = new Dictionary<string, GameObject>();

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
                else if (arg == "-maxConn")
                {
                    maxConnections = int.Parse(arguments[i + 1]);
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

        public MissionManager GetMissionManager(string matchID)
        {
            if (MissionManagers.ContainsKey(matchID) == false)
            {
                Debug.LogError($"No manager for {matchID}");
                return null;
            }

            return MissionManagers[matchID].GetComponent<MissionManager>();
        }

        [Client]
        public async UniTask<bool> NotifyUserConnect(int netId, GameUser user)
        {
            user.ConnectionID = netId;
            Debug.Log($"Notify User of netID : {netId}");
            return await MatchServer.Instance.NotifyUserConnect(_ipPortInfo, netId, user);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            //반드시 불러줘야한다!!
            base.OnClientConnect(conn);
        }

        public async UniTask NotifyStartGame(string matchID)
        {
            await MatchServer.Instance.NotifyMatchStarted(_ipPortInfo, matchID);
        }

        //This is called on server. 
        public override async void OnServerDisconnect(NetworkConnection conn)
        {
            var nedId = conn.identity.netId;
            await MatchServer.Instance.NotifyUserDisconnect(_ipPortInfo, (int)nedId);
            Debug.Log($"Disconnected user {nedId}");

            base.OnServerDisconnect(conn);
        }

        public override async void OnStopServer()
        {
            await MatchServer.Instance.TurnOffServerAsync(_ipPortInfo);

            base.OnStopServer();
        }

        [Server]
        public bool SpawnChatHubIfNotExists(string matchID)
        {
            if (ChatHubs.ContainsKey(matchID))
            {
                return false;
            }

            var chatHub = Instantiate(spawnPrefabs[0]);
            NetworkServer.Spawn(chatHub);
            chatHub.GetComponent<NetworkMatchChecker>().matchId = matchID.ToGuid();

            ChatHubs.Add(matchID, chatHub);

            Debug.Log($"Spawning hub for {matchID}");

            return true;
        }

        [Server]
        public MissionManager SpawnMissionManager(string matchId)
        {
            var missionManager = Instantiate(spawnPrefabs[1]);
            missionManager.GetComponent<NetworkMatchChecker>().matchId = matchId.ToGuid();

            if (MissionManagers.ContainsKey(matchId))
            {
                MissionManagers[matchId] = missionManager;
            }
            else
            {
                MissionManagers.Add(matchId, missionManager);
            }

            NetworkServer.Spawn(missionManager);

            return missionManager.GetComponent<MissionManager>();
        }

        [Server]
        public async void CompleteMatch(string matchId)
        {
            if (matchId == null)
            {
                Debug.LogError("Null matchId on complete");
                return;
            }

            await MatchServer.Instance.NotifyMatchCompleteAsync(_ipPortInfo, matchId);

            //Destroy MissionManager
            var hasManager = MissionManagers.TryGetValue(matchId, out var missionManager);

            if (hasManager)
            {
                MissionManagers.Remove(matchId);
                Destroy(missionManager);
            }

            var hasChatHub = ChatHubs.TryGetValue(matchId, out var chathub);

            if (hasChatHub)
            {
                Debug.Log($"Destroy chathub for {matchId}");
                Destroy(chathub);
                ChatHubs.Remove(matchId);
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