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
        public CatchAction(Transform raycastTransform, LayerMask targetLayers)
            : base(raycastTransform)
        {
            TargetLayerMask = targetLayers;
        }

        public override void TryExecute()
        {
            var player = _hit.collider.gameObject.GetComponent<Player>();

            if (player.State == PlayerState.Student)
            {
                Debug.Log($"Caught {player.PlayerId}");
                player.CaughtByProfessor();
            }
        }
    }
}
