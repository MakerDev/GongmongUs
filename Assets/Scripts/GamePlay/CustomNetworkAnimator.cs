using Mirror;
using UnityEngine;

namespace Assets.Scripts
{
    public class CustomNetworkAnimator: NetworkBehaviour
    {
        [SerializeField]
        private Animator _animator;

        private bool _wasWalking;
        private float _lastForwardVelocity = 0.0f;
        private float _lastSpeed = 0.0f;

        private bool _wasJumping = false;

        public void Play(string name)
        {
            _animator.Play(name);
        }

        public void Jump(bool isJumping)
        {
            _animator.SetBool("IsJumping", isJumping);

            if (_wasJumping != isJumping)
            {
                CmdSetJump(isJumping);
            }

            _wasJumping = isJumping;
        }

        [Command]
        private void CmdSetJump(bool isJumping)
        {
            RpcSetJump(isJumping);
        }

        [ClientRpc]
        private void RpcSetJump(bool isJumping)
        {
            _animator.SetBool("IsJumping", isJumping);
        }

        public void PlayWalkOrRun(bool isWalking, float forwardVelocity, float speed)
        {
            var executeCommand = true;

            if (isWalking == _wasWalking && forwardVelocity == _lastForwardVelocity && speed == _lastSpeed)
            {
                executeCommand = false;
            }

            _animator.SetBool("IsWalking", isWalking);
            _animator.SetFloat("ForwardVelocity", forwardVelocity);
            _animator.SetFloat("Speed", speed);

            if (executeCommand)
            {
                CmdPlayWalkOrRun(isWalking, forwardVelocity, speed);
            }

            _wasWalking = isWalking;
            _lastForwardVelocity = forwardVelocity;
            _lastSpeed = speed;
        }

        [Command]
        private void CmdPlayWalkOrRun(bool isWalking, float forwardVelocity, float speed)
        {
            RpcPlayWalkOrRun(isWalking, forwardVelocity, speed);
        }

        [ClientRpc]
        private void RpcPlayWalkOrRun(bool isWalking, float forwardVelocity, float speed)
        {
            _animator.SetBool("IsWalking", isWalking);
            _animator.SetFloat("ForwardVelocity", forwardVelocity);
            _animator.SetFloat("Speed", speed);
        }
    }
}

