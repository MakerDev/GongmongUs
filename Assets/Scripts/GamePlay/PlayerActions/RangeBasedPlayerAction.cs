using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.GamePlay.PlayerActions
{
    public class RangeBasedPlayerAction : CooltimeAction
    {
        public float Range { get; protected set; } = 2.5f;
        public LayerMask TargetLayerMask { get; protected set; } = LayerMask.NameToLayer("RemotePlayer");

        //TODO : Set this properly
        public Transform RaycastTransform { get; protected set; }

        protected RaycastHit _hit;

        public RangeBasedPlayerAction(Transform raycastTransform)
            : base("MainAction")
        {
            RaycastTransform = raycastTransform;
        }

        public override bool CanExecute()
        {
            if (base.CanExecute() == false)
            {
                return false;
            }

            var isHit = Physics.Raycast(RaycastTransform.position,
                                        RaycastTransform.forward,
                                        out _hit,
                                        Range,
                                        TargetLayerMask);

            //TODO : Move this to high order component?
            PlayerSetup.PlayerUI.SetCrossHair(isHit);

            return isHit;
        }
    }
}
