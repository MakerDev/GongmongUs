using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private Text _playerName;
        [SerializeField]
        private List<Sprite> _danbegSprites = new List<Sprite>();
        [SerializeField]
        private Image _isReadyImage;
        [SerializeField]
        private Image _danbegImage;

        public void SetPlayer(Player player)
        {
            if (player.IsReady)
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
        }
    }
}
