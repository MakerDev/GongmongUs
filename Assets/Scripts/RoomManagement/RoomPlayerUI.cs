using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.RoomManagement
{
    public class RoomPlayerUI: MonoBehaviour
    {
        private Player _player;

        public void SetPlayer(Player player)
        {
            _player = player;
        }
    }
}
