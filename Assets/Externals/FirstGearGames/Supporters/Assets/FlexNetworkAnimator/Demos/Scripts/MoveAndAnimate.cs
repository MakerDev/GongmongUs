
using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FlexNetworkAnimators.Demos
{

    public class MoveAndAnimate : NetworkBehaviour
    {
        private FlexNetworkAnimator _fna;
        private Animator _animator;


        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _fna = GetComponent<FlexNetworkAnimator>();
        }

        private void Update()
        {
            if (base.hasAuthority)
            {
                //Move.
                float horizontal = Input.GetAxis("Horizontal");
                float moveRate = 1f;
                transform.position += new Vector3(horizontal, 0f, 0f) * moveRate * Time.deltaTime;

                //Animator.
                _animator.SetFloat("Horizontal", horizontal);

                if (Input.GetKeyDown(KeyCode.Space))
                    _fna.SetTrigger("Jump");
            }
        }


    }


}