using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.MiniGames
{
    public class ElevatorDanbeg : MonoBehaviour
    {
        [SerializeField]
        private ElevatorMiniGame _miniGame;

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag == "Wall")
            {
                _miniGame.Fail();
            }

            if (other.gameObject.tag == "Elevator")
            {
                _miniGame.Success();
            }
        }
    }
}
