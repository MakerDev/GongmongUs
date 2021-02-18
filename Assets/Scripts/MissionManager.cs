using Assets.Scripts.MatchMaking;
using Assets.Scripts.MiniGames;
using Assets.Scripts.Networking;
using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class MissionManager : NetworkBehaviour
    {
        public static MissionManager Instance { get; private set; } = null;

        public List<MiniGame> AllMissions { get; private set; } = new List<MiniGame>();

        public Dictionary<string, bool> PlayerMissionsProgress { get; private set; } = new Dictionary<string, bool>();

        public int MissionsPerPlayer { get; private set; } = 2;
        public string MatchID { get; private set; }

        public List<OpenableDoor> OpenableDoors { get; private set; } = new List<OpenableDoor>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            var interactables = FindObjectsOfType<Interactable>();

            foreach (var interactable in interactables)
            {
                AllMissions.Add(interactable.MiniGame);
            }

            //TODO : 정렬방법 더 좋은 거 가져와
            AllMissions = AllMissions.OrderBy(x => x.gameObject.transform.parent.name).ToList();

            OpenableDoors = FindObjectsOfType<OpenableDoor>().ToList();

            AssignMissions(MatchManager.Instance.Match.MatchID.ToGuid());
        }

        //랜덤 Guid로 플레이어들을 정렬해서 미션을 분배하는 방식을 선택
        private void AssignMissions(Guid orderId)
        {
            //어차피 클라이언트에서 하는 거니까, 다른 유저에게 미션을 Assign할 필요가 없다!
            var skipNum = UnityEngine.Random.Range(0, AllMissions.Count - MissionsPerPlayer);
            Player.LocalPlayer.AssignMissions(AllMissions.Skip(skipNum).Take(MissionsPerPlayer));
        }

        /// <summary>
        /// Set server's PlayerMissionsProgress dictionary for simple state sync.
        /// </summary>
        /// <param name="matchID"></param>
        public void ConfigureForServer(string matchID, string professorID)
        {
            MatchID = matchID;

            var allPlayers = GameManager.Instance.Players.Values;

            PlayerMissionsProgress.Clear();

            foreach (var player in allPlayers)
            {
                if (player.MatchID == matchID && player.PlayerId != professorID)
                {
                    PlayerMissionsProgress.Add(player.PlayerId, false);
                }
            }
        }

        [Server]
        public void OnPlayerDisconnectGame(Player player)
        {
            PlayerMissionsProgress.Remove(player.PlayerId);

            if (player.State == PlayerState.Professor)
            {
                ServerCompleteMatch(MatchResult.StudentsWin, MatchID);
            }
            else if (player.State == PlayerState.Student)
            {
                //If all players exit the game, then professor wins.
                if (PlayerMissionsProgress.Count == 0)
                {
                    ServerCompleteMatch(MatchResult.ProfessorWins, MatchID);

                    return;
                }

                ServerEvaluateMissionState();
            }
        }

        [Server]
        public void ServerEvaluateMissionState()
        {
            if (CheckAllMissionsCleared())
            {
                RpcOpenExitDoors();
            }
        }

        [Server]
        public void ServerOnPlayerEscape(string playerId)
        {
            RemovePlayer(playerId);

            if (PlayerMissionsProgress.Count <= 0)
            {
                ServerCompleteMatch(MatchResult.StudentsWin, MatchID);
            }
        }

        //This is called by "LocalPlayer"
        [Server]
        public void ServerOnPlayerCaught(string playerId)
        {
            RemovePlayer(playerId);

            //If no more player is left, professor wins
            if (PlayerMissionsProgress.Count <= 0)
            {
                ServerCompleteMatch(MatchResult.ProfessorWins, MatchID);
            }

            //If all missions are cleared except for this caught student, the exit door should be opened.
            ServerEvaluateMissionState();
        }

        [Server]
        public void ServerCompleteMatch(MatchResult matchResult, string matchID)
        {
            BCNetworkManager.Instance.CompleteMatch(matchID);
            GameManager.Instance.ServerCompleteMatch(matchID);

            RpcCompleteMatch(matchResult);
        }

        [ClientRpc]
        private void RpcCompleteMatch(MatchResult matchResult)
        {
            MatchManager.Instance.MatchCompleted(matchResult);
            BCNetworkManager.Instance.MoveToResultScene();
        }

        /// <summary>
        /// Call this when a Player is caught by professor and becomes assistant.
        /// Or in case student exits game.
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            PlayerMissionsProgress.Remove(playerId);
        }

        private bool CheckAllMissionsCleared()
        {
            foreach (var playerId in PlayerMissionsProgress.Keys)
            {
                var player = GameManager.Instance.GetPlayer(playerId);

                if (player.State != PlayerState.Student)
                {
                    continue;
                }

                var isDone = PlayerMissionsProgress[playerId];

                if (isDone == false)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This is called by LocalPlayer
        /// </summary>
        /// <param name="playerId"></param>
        public void NotifyPlayerCompleteMissions(string playerId)
        {
            //Check whether all missions are completed.
            PlayerMissionsProgress[playerId] = true;

            GameManager.Instance.PrintMessage($"Player {playerId} has done his jobs", "SYSTEM", ChatType.Info);
        }

        /// <summary>
        /// This is called by server on command.
        /// </summary>
        /// <param name="playerId"></param>
        public void ServerNotifyPlayerCompleteMissions(string playerId)
        {
            //Check whether all missions are completed.
            PlayerMissionsProgress[playerId] = true;

            if (CheckAllMissionsCleared())
            {
                //If all completed
                RpcOpenExitDoors();
            }
        }

        [ClientRpc]
        private void RpcOpenExitDoors()
        {
            //TODO : Notify all missions complete.
            GameManager.Instance.PrintMessage("Go exit here", "SYSTEM", ChatType.Info);

            //TOOD : Open the exit doors.
            foreach (var openableDoor in OpenableDoors)
            {
                openableDoor.OpenDoor();
            }
        }
    }
}