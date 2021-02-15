using Assets.Scripts.MatchMaking;
using Assets.Scripts.MiniGames;
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

            AllMissions = AllMissions.OrderBy(x => x.gameObject.transform.parent.name).ToList();

            OpenableDoors = FindObjectsOfType<OpenableDoor>().ToList();

            if (isClient)
            {
                AssignMissions(MatchManager.Instance.Match.MatchID.ToGuid());
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

                player.AssignMissions(AllMissions.Skip(i*MissionsPerPlayer).Take(MissionsPerPlayer));                
            }

            LeftMissionsCount = players.Count * MissionsPerPlayer;
        }
        
        public bool OnPlayerExit(string playerId)
        {
            //If there is no students left, then Students win as it means all students
            //has exited.

            return false;
        }

        public bool OnPlayerCaught(string playerId)
        {
            //If no more player is left, professor wins
            if (PlayerMissionsProgress.Count <= 0)
            {
                MoveToResult(professorIsWinner: true);
            }

            return false;
        }

        public void MoveToResult(bool professorIsWinner)
        {

        }

        /// <summary>
        /// Call this when a Player is caught by professor and becomes assistant.
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            PlayerMissionsProgress.Remove(playerId);
        }

        [Client]
        public void NotifyPlayerCompleteMissions(string playerId)
        {
            //Check whether all missions are completed.
            PlayerMissionsProgress[playerId] = true;

            foreach (var isDone in PlayerMissionsProgress.Values)
            {
                var player = GameManager.Instance.GetPlayer(playerId);

                if (player.State != PlayerState.Student)
                {
                    continue;
                }

                if (isDone == false)
                {
                    return;
                }
            }

            //If all completed
            CmdCompleteMission();
        }

        [Command(ignoreAuthority = true)]
        private void CmdCompleteMission()
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