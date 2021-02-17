using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkTransforms.Demos
{

    public class MotorBasic : NetworkBehaviour
    {
        /// <summary>
        /// Layer for platforms.
        /// </summary>
        public LayerMask PlatformLayer;
        /// <summary>
        /// How quickly to move.
        /// </summary>
        public float MoveRate = 3f;
        /// <summary>
        /// FNT reference on this object.
        /// </summary>
        private FlexNetworkTransform _fnt;
        /// <summary>
        /// Transform the motor follows around.
        /// </summary>
        private Transform _motorTarget;
        /// <summary>
        /// Last object hit while tracing for platforms.
        /// </summary>
        private GameObject _lastHitObject = null;
        /// <summary>
        /// True to use platform detection. Used for debug.
        /// </summary>
        [Tooltip("True to use platform detection. Used for debug.")]
        [SerializeField]
        private bool _usePlatforms = true;

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();
            _fnt = GetComponent<FlexNetworkTransform>();
            _motorTarget = new GameObject().transform;
            _motorTarget.gameObject.name = "Motor3D Target";
        }

        private void OnDestroy()
        {
            if (_motorTarget != null)
                Destroy(_motorTarget.gameObject);
        }

        // Update is called once per frame
        private void Update()
        {
            if (!base.hasAuthority)
                return;

            /* START LOCAL MOVEMENT CODE. NOT RELATED TO FNT ATTACH FEATURE. */
            if (Input.GetKeyDown(KeyCode.P))
                _usePlatforms = true;
            else if (Input.GetKeyDown(KeyCode.O))
                _usePlatforms = false;

            /* Local movement, nothing special. */
            float horizontal = Input.GetAxis("Horizontal");
            float forward = Input.GetAxis("Vertical");
            Vector3 nextPos = new Vector3(horizontal, 0f, forward) * MoveRate * Time.deltaTime;

            //No platform, move normally.
            if (_motorTarget.parent == null)
            {
                transform.position += nextPos;
            }
            else
            {
                nextPos.x *= _motorTarget.localScale.x;
                nextPos.y *= _motorTarget.localScale.y;
                nextPos.z *= _motorTarget.localScale.z;
                _motorTarget.localPosition += nextPos;

                transform.position = _motorTarget.position;
                transform.rotation = _motorTarget.rotation;

            }
            /* END LOCAL MOVEMENT CODE. NOT RELATED TO FNT ATTACH FEATURE. */


            /* Check to set the platform.
             * Here is where if a platform is hit
             * the value is set to FlexNetworkTransform. 
             * Also notice that when the platform is not hit,
             * I am setting null. */
            Ray ray = new Ray(transform.position + new Vector3(0f, 1f, 0f), -transform.up);
            RaycastHit hit;
            //If hit.
            if (_usePlatforms && Physics.Raycast(ray, out hit, 25f, PlatformLayer))
            {
                //Same object hit, no reason to process again.
                if (hit.collider.gameObject == _lastHitObject)
                    return;
                _lastHitObject = hit.collider.gameObject;

                NetworkIdentity ni = hit.collider.transform.root.GetComponent<NetworkIdentity>();
                if (ni != null)
                {
                    /* ChildIndex used. This is populated if object to attach
                     * to isn't object root. */
                    sbyte childTargetIndex = -1;

                    //If not parent object hit then try to get find target for the child object.
                    if (hit.collider.transform.parent != null)
                    {
                        if (ni.GetComponent<FlexAttachTargets>() is FlexAttachTargets fct)
                            childTargetIndex = fct.ReturnTargetIndex(hit.collider.gameObject);
                    }
                    _fnt.SetAttached(ni, childTargetIndex);
            
                    /* START LOCAL MOVEMENT CODE. NOT RELATED TO FNT ATTACH FEATURE. */

                    /* Target to attach to locally. This isn't related to FNT. The
                     * platform target is simply to keep my character stuck to the platform
                     * locally, and move with it. How this is done will vary based on your motor. */
                    Transform ptParent = (childTargetIndex >= 0) ? hit.collider.transform : ni.transform;
                    _motorTarget.SetParent(ptParent);
                    _motorTarget.transform.position = hit.point;
                    _motorTarget.localRotation = Quaternion.identity;

                    /* END LOCAL MOVEMENT CODE. NOT RELATED TO FNT ATTACH FEATURE. */
                }
            }
            //No hit.
            else
            {
                _fnt.SetAttached(null);
                _lastHitObject = null;
            }
        }



    }


}