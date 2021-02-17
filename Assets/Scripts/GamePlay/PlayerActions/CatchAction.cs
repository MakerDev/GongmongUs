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
        private PlayerController _playerController;

        public CatchAction(Transform raycastTransform,
            LayerMask targetLayers,
            PlayerController playerController)
            : base(raycastTransform)
        {
            TargetLayerMask = targetLayers;
            _playerController = playerController;
        }

        public override void TryExecute()
        {
            _playerController.PlayCatchAnimation();

            if (CanExecute() == false)
            {
                return;
            }

            var player = _hit.collider.gameObject.GetComponent<Player>();

            if (player.State == PlayerState.Student)
            {
                StartCooldown();
                Debug.Log($"Caught {player.PlayerId}");
                player.CaughtByProfessor();
            }
        }
    }
}
