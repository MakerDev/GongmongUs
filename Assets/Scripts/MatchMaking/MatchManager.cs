using BattleCampusMatchServer.Models;
using BattleCampusMatchServer.Models.DTOs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MatchMaking
{
    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        public MatchDTO Match { get; private set; }
        public IpPortInfo IpPortInfo { get; private set; } = new IpPortInfo();

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