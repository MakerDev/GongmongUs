using FirstGearGames.Utilities.Objects;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    /// <summary>
    /// Floating UI above the players
    /// </summary>
    public class PlayerInfoUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _playerNameText;

        private Player _player;

        public void SetPlayer(Player player)
        {
            _player = player;
            _playerNameText.text = player.PlayerName;
        }
    }
}