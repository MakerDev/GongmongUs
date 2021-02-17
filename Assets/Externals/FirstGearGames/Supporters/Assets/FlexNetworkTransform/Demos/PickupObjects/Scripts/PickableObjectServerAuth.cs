using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{


    public class PickableObjectServerAuth : NetworkBehaviour
    {
        /// <summary>
        /// Layers this object can attach to.
        /// </summary>
        public LayerMask AttachableLayers;
        /// <summary>
        /// FNT on this object.
        /// </summary>
        private FlexNetworkTransform _fnt;
        /// <summary>
        /// Target the ball should follow.
        /// </summary>
        private Transform _ballTarget = null;

        private void Awake()
        {
            _fnt = GetComponent<FlexNetworkTransform>();
        }

        private void LateUpdate()
        {
            /* IMPORTANT IMPORTANT 
             *  IMPORTANT IMPORTANT 
             *   IMPORTANT IMPORTANT */
            /* Notice I lock the ball onto the target in LateUpdate.
             * This is important because FNT processes smoothing in
             * update. If the ball were to move before smoothing is
             * run on FNT it may jitter on spectators. When using server
             * side Attached logic it's best to move with your target
             * in late update. This does not matter when using client authoritative
             * attached. */
            if (_ballTarget != null)
                transform.position = _ballTarget.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            //This example uses a server side ball.
            if (!NetworkServer.active)
                return;

            //Wrong layer.
            if (other.gameObject.layer != ToLayer(AttachableLayers.value))
                return;

            //Get the network identity on root.
            NetworkIdentity ni = other.transform.root.GetComponent<NetworkIdentity>();
            FlexAttachTargets fct = other.transform.root.GetComponent<FlexAttachTargets>();
            //This example is to show attaching to child objects, so don't proceed if components are missing.
            if (ni == null || fct == null)
                return;

            //Follow transform which set off trigger. 
            _ballTarget = other.transform;
            //Set attached on this balls FNT so other players see it follow flawlessly.
            _fnt.SetAttached(ni, fct.ReturnTargetIndex(other.gameObject));
        }


        /// <summary>
        /// Converts a layer bitmask to int.
        /// </summary>
        /// <param name="bitmask"></param>
        /// <returns></returns>
        private int ToLayer(int bitmask)
        {
            int result = bitmask > 0 ? 0 : 31;
            while (bitmask > 1)
            {
                bitmask = bitmask >> 1;
                result++;
            }
            return result;
        }
    }

}