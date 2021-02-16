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
        public int LeftMissionsCount { get; private set; } = 2;

        public List<OpenableDoor> OpenableDoors { get; private set; } = new List<OpenableDoor>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Start()
        {
            var interactables = FindObjectsOfType<Interactable>();

            foreach (var interactable in interactables)
            {
                AllMissions.Add(interactable.MiniGame);
            }

            //TODO : 정렬방법 더 좋은 거 가져와
            AllMissions = AllMissions.OrderBy(x => x.gameObject.transform.parent.name).ToList();

            OpenableDoors = FindObjectsOfType<OpenableDoor>().ToList();

            if (isClient)
            {
                AssignMissions(MatchManager.Instance.Match.MatchID.ToGuid());
            }
        }

        private void OnDestroy()
        {
            if (isClient)
            {
                BCNetworkManager.Instance.MoveToResultScene();
            }
        }

        //랜덤 Guid로 플레이어들을 정렬해서 미션을 분배하는 방식을 선택
        private void AssignMissions(Guid orderId)
        {
            var players = GameManager.Instance.Players.Values.OrderBy(p => p.PlayerId).ToList();

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                //PlayerState might not be updated here. However, Missions assigned to Profesor has no meaning.
                if (player.State != PlayerState.Student)
                {
                    continue;
                }

                PlayerMissionsProgress.Add(player.PlayerId, false);

                player.AssignMissions(AllMissions.Skip(i * MissionsPerPlayer).Take(MissionsPerPlayer));
            }

            LeftMissionsCount = players.Count * MissionsPerPlayer;
        }
        
        //서버랑 동기화하려면 얘네를 다 커맨드로 바꾸고, playerID랑 LocalPlayer랑 비교해서 역할 다르게 해야할듯.
        //This is called by Localplayer of the playerId
        public void OnPlayerExit(string playerId)
        {
            RemovePlayer(playerId);

            var isLocalPlayer = Player.LocalPlayer.PlayerId == playerId;

            //If there is no students left, then Students win as it means all students
            //has exited.
            if (isLocalPlayer && PlayerMissionsProgress.Count <= 0)
            {
                CmdCompleteMatch(MatchResult.StudentsWin, Player.LocalPlayer.PlayerId);
            }
        }

        //This is called by "LocalPlayer"
        public bool OnPlayerCaught(string playerId)
        {
            RemovePlayer(playerId);

            //If no more player is left, professor wins
            if (PlayerMissionsProgress.Count <= 0)
            {
                CmdCompleteMatch(MatchResult.ProfessorWins, Player.LocalPlayer.PlayerId);
            }

            //If all missions are cleared except for this caught student, the exit door should be opened.
            if (CheckAllMissionsCleared())
            {
                CmdOnAllMissionsComplete();
            }

            return false;
        }

        [Command(ignoreAuthority = true)]
        private void CmdNotifyMatchResult(string matchId)
        {
            BCNetworkManager.Instance.CompleteMatch(matchId);
        }

        [Command(ignoreAuthority = true)]
        public void CmdCompleteMatch(MatchResult matchResult, string issuerPlayerId)
        {
            RpcCompleteMatch(matchResult, issuerPlayerId);
        }

        [ClientRpc]
        private void RpcCompleteMatch(MatchResult matchResult, string issuerPlayerId)
        {
            var isIssuer = Player.LocalPlayer.PlayerId == issuerPlayerId;
            MatchManager.Instance.MatchCompleted(matchResult);

            //To make sure when MissionManager is destoryed the match result is all synced, NotifyMatchResult to 
            //Networkmanager here.
            if (isIssuer)
            {
                CmdNotifyMatchResult(MatchManager.Instance.Match.MatchID);
            }
        }

        /// <summary>
        /// Call this when a Player is caught by professor and becomes assistant.
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
        [Client]
        public void NotifyPlayerCompleteMissions(string playerId)
        {
            //Check whether all missions are completed.
            PlayerMissionsProgress[playerId] = true;

            var isLocalPlayer = Player.LocalPlayer.PlayerId == playerId;

            GameManager.Instance.PrintMessage($"Player {playerId} has done his jobs", "SYSTEM", ChatType.Info);

            if (isLocalPlayer && CheckAllMissionsCleared())
            {
                //If all completed
                CmdOnAllMissionsComplete();
            }
        }

        [Command(ignoreAuthority = true)]
        private void CmdOnAllMissionsComplete()
        {
            RpcCompleteMission();
        }

        [ClientRpc]
        private void RpcCompleteMission()
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