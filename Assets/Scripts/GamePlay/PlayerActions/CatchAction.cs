using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class CatchAction : RangeBasedPlayerAction
    {
        public CatchAction()
        {
            Range = 10.0f;
        }

        public override void Execute()
        {
            if (Input.GetKeyDown("Fire1"))
            {
                var player = _hit.collider.gameObject.GetComponent<Player>();

                if (player.State == PlayerState.Student)
                {
                    player.CaughtByProfessor();
                }
            }
        }
    }
}
