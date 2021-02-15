using Assets.Scripts.Networking;
using BattleCampusMatchServer.Models;
using BattleCampusMatchServer.Models.DTOs;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scripts.MatchMaking
{
    public enum MatchResult
    {
        ProfessorWins,
        StudentsWin,
    }

    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public MatchDTO Match { get; private set; }
        public IpPortInfo IpPortInfo { get; private set; } = new IpPortInfo();

        public MatchResult MatchResult { get; private set; }

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

        public void MatchCompleted(MatchResult matchResult)
        {
            MatchResult = matchResult;
        }

        public void ClearMatchResult()
        {
            //TODO : Do stuffs if needed.
        }

        public void ConfigureMatchInfo(MatchDTO match)
        {
            Match = match;
            IpPortInfo = match.IpPortInfo;
        }

        public void ResetMatchInfo()
        {
            Match = null;
            IpPortInfo = null;
        }
    }
}