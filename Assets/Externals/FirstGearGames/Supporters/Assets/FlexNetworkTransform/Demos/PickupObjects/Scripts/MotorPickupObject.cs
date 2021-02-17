using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class MotorPickupObject : NetworkBehaviour
    {
        public LayerMask _groundLayer;
        public float MoveRate = 3f;
        private float _rotateRate = 480f;

        private Quaternion _targetRotation;
        // Update is called once per frame
        private void Update()
        {
            if (!base.hasAuthority)
                return;

            //Snap to ground.
            RaycastHit hit;
            Ray ray = new Ray(transform.position + new Vector3(0f, 1f, 0f), Vector3.down);
            if (Physics.Raycast(ray, out hit, 20f, _groundLayer))
                transform.position = hit.point;


            /* Local movement, nothing special. */
            float horizontal = Input.GetAxis("Horizontal");
            float forward = Input.GetAxis("Vertical");
            Vector3 nextPos = new Vector3(horizontal, 0f, forward) * MoveRate * Time.deltaTime;
            transform.position += nextPos;

            if (nextPos != Vector3.zero)
                _targetRotation = Quaternion.LookRotation(new Vector3(horizontal, 0f, forward));

            transform.rotation = Quaternion.RotateTowards(transform.rotation, _targetRotation, Time.deltaTime * _rotateRate);
        }



    }


}