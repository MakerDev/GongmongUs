using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class Motor3D : NetworkBehaviour
    {
        public LayerMask PlatformLayer;
        public float MoveRate = 3f;

        private FlexNetworkTransform _fnt;

        private NetworkIdentity _localPlatform;
        private Transform _platformTarget;
        private bool _usePlatforms = true;

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            _fnt = GetComponent<FlexNetworkTransform>();
            _platformTarget = new GameObject().transform;
            _platformTarget.gameObject.name = "Motor3D Target";
        }

        private void OnDestroy()
        {
            if (_platformTarget != null)
                Destroy(_platformTarget.gameObject);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!base.hasAuthority)
                return;

            if (Input.GetKeyDown(KeyCode.P))
                _usePlatforms = true;
            else if (Input.GetKeyDown(KeyCode.O))
                _usePlatforms = false;

            /* Local movement, nothing special. */
            float horizontal = Input.GetAxis("Horizontal");
            float forward = Input.GetAxis("Vertical");
            Vector3 nextPos = new Vector3(horizontal, 0f, forward) * MoveRate * Time.deltaTime;

            //No platform, move normally.
            if (_platformTarget.parent == null)
            {
                transform.position += nextPos;
            }
            else
            {
                nextPos.x *= _platformTarget.localScale.x;
                nextPos.y *= _platformTarget.localScale.y;
                nextPos.z *= _platformTarget.localScale.z;
                _platformTarget.localPosition += nextPos;
                transform.position = _platformTarget.position;
                transform.rotation = _platformTarget.rotation;
            }


            /* Check to set the platform.
             * Here is where if a platform is hit
             * the value is set to FlexNetworkTransform. 
             * Also notice that when the platform is not hit,
             * I am setting null. */
            Ray ray = new Ray(transform.position + new Vector3(0f, 1f, 0f), -transform.up);
            RaycastHit hit;
            //If hit.
            if (_usePlatforms && Physics.Raycast(ray, out hit, 15f, PlatformLayer))
            {
                _localPlatform = hit.collider.gameObject.GetComponent<NetworkIdentity>();
                _fnt.SetPlatform(_localPlatform);
                if (_platformTarget.parent == null)
                {
                    _platformTarget.transform.position = hit.point;
                    _platformTarget.SetParent(_localPlatform.transform);
                    _platformTarget.localRotation = Quaternion.identity;
                }
            }
            //No hit.
            else
            {
                _localPlatform = null;
                _fnt.SetPlatform(null);
            }
        }




    }


}