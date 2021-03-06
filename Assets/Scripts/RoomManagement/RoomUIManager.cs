﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.RoomManagement
{
    public class RoomUIManager : MonoBehaviour
    {
        public static RoomUIManager Instance { get; private set; }

        [SerializeField]
        private Text _localPlayerName;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void Update()
        {
            if (Player.LocalPlayer != null)
            {
                _localPlayerName.text = Player.LocalPlayer.PlayerName;
            }
        }
    }
}
