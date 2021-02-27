using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.RoomManagement
{
    public class RoomPlayerUI : MonoBehaviour
    {
        [SerializeField]
        private Sprite _readySprite;
        [SerializeField]
        private Sprite _notReadySprite;
        [SerializeField]
        private TextMeshProUGUI _playerName;
        [SerializeField]
        private List<Sprite> _danbegSprites = new List<Sprite>();
        [SerializeField]
        private Image _isReadyImage;
        [SerializeField]
        private Image _danbegImage;
        [SerializeField]
        private Button _kickButton;

        private Player _player;

        public void SetPlayer(Player player)
        {
            _player = player;

            if (player.IsReady || player.IsHost)
            {
                _isReadyImage.sprite = _readySprite;
            }
            else
            {
                _isReadyImage.sprite = _notReadySprite;
            }

            var danbegSpriteIndex = UnityEngine.Random.Range(0, _danbegSprites.Count);
            _danbegImage.sprite = _danbegSprites[danbegSpriteIndex];
            _playerName.text = player.PlayerName;

            //TODO : Display something if he is the host.
            if (Player.LocalPlayer != null && Player.LocalPlayer.IsHost)
            {
                if (player.IsHost == false)
                {
                    _kickButton.gameObject.SetActive(true);
                }
            }
            else
            {
                _kickButton.gameObject.SetActive(false);
            }
        }

        public void KickPlayer()
        {
            if (Player.LocalPlayer.IsHost)
            {
                Player.LocalPlayer.KickPlayer(_player.PlayerId);
            }
        }
    }
}
