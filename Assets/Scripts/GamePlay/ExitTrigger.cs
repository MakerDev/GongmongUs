using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GamePlay
{
    public class ExitTrigger : NetworkBehaviour
    {
        private int _localPlayerLayer;

        private void Start()
        {
            _localPlayerLayer = LayerMask.NameToLayer("LocalPlayer");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _localPlayerLayer && Player.LocalPlayer.State == PlayerState.Student)
            {
                Player.LocalPlayer.Escape();
            }
        }
    }
}
