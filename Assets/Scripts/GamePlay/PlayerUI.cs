using FirstGearGames.Utilities.Objects;
using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    public class PlayerUI : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _thrusterFuelFill;

        [SerializeField]
        private RectTransform _hpBar;
        [SerializeField]
        private Text _playerNameText;

        [SerializeField]
        private Image _crossHairImage;
        [SerializeField]
        private Sprite _crossHairDefault;
        [SerializeField]
        private Sprite _crossHairOnTarget;

        private PlayerController _controller;
        private Player _player;

        private void Start()
        {
            _crossHairImage.sprite = _crossHairDefault;
        }

        private void Update()
        {
            SetFuelAmout(_controller.GetThrusterFuelAmount());
            _hpBar.SetScale(new Vector3(_player.GetCurrentHpRatio(), 1, 1));
        }

        private bool _wasOnTarget = false;

        public void SetCrossHair(bool onTarget)
        {
            if (_wasOnTarget == onTarget)
            {
                return;
            }

            _wasOnTarget = onTarget;

            if (onTarget)
            {
                _crossHairImage.sprite = _crossHairOnTarget;
            }
            else
            {
                _crossHairImage.sprite = _crossHairDefault;
            }
        }

        public void SetLocalPlayerName(string name)
        {
            _playerNameText.text = name;
        }

        public void SetController(PlayerController playerController)
        {
            _controller = playerController;
        }

        public void SetPlayer(Player player)
        {
            _player = player;
        }

        void SetFuelAmout(float amount)
        {
            _thrusterFuelFill.localScale = new Vector3(1, amount, 1);
        }
    }
}