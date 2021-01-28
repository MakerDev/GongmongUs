
using FirstGearGames.Mirrors.Assets.FlexNetworkAnimators;
using Mirror;
using UnityEngine;

namespace FirstGearGames.Mirrors.Assets.FNAS.Demos
{

    public class MoveAndAnimateServerAuth : NetworkBehaviour
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
                float horizontal = Input.GetAxis("Horizontal");
                CmdUpdateHorizontal(horizontal);

                if (Input.GetKeyDown(KeyCode.Space))
                    CmdJump();
            }
        }

        [Command]
        private void CmdJump()
        {
            _fna.SetTrigger("Jump");
        }

        [Command]
        private void CmdUpdateHorizontal(float horizontal)
        {
            float moveRate = 1f;
            transform.position += new Vector3(horizontal, 0f, 0f) * moveRate * Time.deltaTime;

            _animator.SetFloat("Horizontal", horizontal);
        }


    }


}