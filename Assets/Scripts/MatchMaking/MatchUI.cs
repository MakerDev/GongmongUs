using Assets.Scripts.MatchMaking;
using Assets.Scripts.Networking;
using BattleCampusMatchServer.Models.DTOs;
using Cysharp.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    //TODO : 이제 얘가 스스로 자기 정보만 동기화 하도록 해도 괜찮을 듯.
    public class MatchUI : MonoBehaviour
    {
        private MatchDTO _match;

        [SerializeField]
        private Text _matchNameText;
        [SerializeField]
        private Text _currentPlayerInfoText;
        [SerializeField]
        private Text _matchIDText;
        [SerializeField]
        private Button _joinButton;

        public void UpdateInfo(MatchDTO match)
        {
            _match = match;

            _matchIDText.text = match.MatchID;
            _matchNameText.text = match.Name;
            _currentPlayerInfoText.text = $"{match.CurrentPlayersCount}/{match.MaxPlayers}";
        }

        public async void JoinMatch()
        {
            _joinButton.enabled = false;

            await LobbyManager.Instance.JoinMatchAsync(_match);

            _joinButton.enabled = true;
        }
    }
}