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

        public Dictionary<string, bool> MissionCompletedPlayers { get; set; } = new Dictionary<string, bool>();

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
            AllMissions = FindObjectsOfType<MiniGame>().ToList();
            OpenableDoors = FindObjectsOfType<OpenableDoor>().ToList();
        }

        //랜덤 Guid로 플레이어들을 정렬해서 미션을 분배하는 방식을 선택
        public void AssignMissions(Guid orderId)
        {
            var players = GameManager.Instance.Players.Values.OrderBy(p => orderId).ToList();

            for (int i = 0; i < players.Count * MissionsPerPlayer; i += MissionsPerPlayer)
            {

            }

            LeftMissionsCount = players.Count * MissionsPerPlayer;
        }

        /// <summary>
        /// Call this when a Player is caught by professor and becomes assistant.
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            MissionCompletedPlayers.Remove(playerId);
        }

        [Client]
        public void NotifyPlayerCompleteMissions(string playerId)
        {
            //Check whether all missions are completed.
            MissionCompletedPlayers[playerId] = true;

            foreach (var isDone in MissionCompletedPlayers.Values)
            {
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

            //TOOD : Open the exit doors.
            foreach (var openableDoor in OpenableDoors)
            {
                openableDoor.OpenDoor();
            }
        }
    }
}