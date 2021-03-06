﻿using Assets.Scripts.GamePlay.PlayerActions;
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
        private Text _playerNameText;

        [SerializeField]
        private Image _crossHairImage;
        [SerializeField]
        private Sprite _crossHairDefault;
        [SerializeField]
        private Sprite _crossHairOnTarget;
        [SerializeField]
        private Text _stateText;

        [SerializeField]
        private Text _leftMissionsText;
        [SerializeField]
        private RectTransform _progressImage;

        [SerializeField]
        private GameObject _cooldownIndicator;
        [SerializeField]
        private RectTransform _coolDownImage;

        private PlayerController _controller;
        private Player _player;
        private bool _wasOnTarget = false;

        private CooltimeAction _cooltimeAction;

        private void Start()
        {
            _crossHairImage.sprite = _crossHairDefault;
        }

        private void Update()
        {
            SetFuelAmout(_controller.GetThrusterFuelAmount());
        }

        public void SetCooltimeAction(CooltimeAction cooltimeAction)
        {
            _cooltimeAction = cooltimeAction;

            DisplayCooldownIndicator();
        }

        private void FixedUpdate()
        {
            if (_cooltimeAction == null)
            {
                return;
            }

            _coolDownImage.localScale = new Vector3(1, _cooltimeAction.ChargeAmount, 1);
            _cooltimeAction.Charge(_cooltimeAction.RechargeSpeed * Time.fixedDeltaTime);
        }

        public void DisplayCooldownIndicator()
        {
            _cooldownIndicator.SetActive(true);
        }

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

        public void SetState()
        {
            _stateText.text = _player.State.ToString();
        }

        public void SetPlayerMissionProgress(int missionsCount, int completedMissionsCount)
        {
            _leftMissionsText.text = $"{completedMissionsCount}/{missionsCount}";
            _progressImage.localScale = new Vector3(completedMissionsCount / missionsCount, 1);
        }

        void SetFuelAmout(float amount)
        {
            _thrusterFuelFill.localScale = new Vector3(1, amount, 1);
        }
    }
}