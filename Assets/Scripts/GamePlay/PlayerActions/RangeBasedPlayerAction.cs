using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class RangeBasedPlayerAction : IPlayerAction
    {
        public float Range { get; protected set; } = 4f;
        public LayerMask TargetLayerMask { get; protected set; } = LayerMask.NameToLayer("RemotePlayer");

        //TODO : Set this properly
        public Transform RaycastTransform { get; protected set; }

        protected RaycastHit _hit;

        public RangeBasedPlayerAction(Transform raycastTransform)
        {
            RaycastTransform = raycastTransform;
        }

        public virtual bool CanExecute()
        {
            var isHit = Physics.Raycast(RaycastTransform.position,
                                        RaycastTransform.forward,
                                        out _hit,
                                        Range,
                                        TargetLayerMask);

            //TODO : Move this to high order component?
            PlayerSetup.PlayerUI.SetCrossHair(isHit);

            return isHit;
        }

        public virtual void TryExecute()
        {

        }
    }
}
