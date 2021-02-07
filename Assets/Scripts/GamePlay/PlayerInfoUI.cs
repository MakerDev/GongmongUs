using FirstGearGames.Utilities.Objects;
using System.Collections;
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
        private Text _playerNameText;

        private Player _player;

        public void SetPlayer(Player player)
        {
            Debug.Log("Set player is called on Player" + player.PlayerName);
            _player = player;

            _playerNameText.text = player.PlayerName;
        }
    }
}