﻿using Cysharp.Threading.Tasks;
using FirstGearGames.Mirrors.Assets.FlexNetworkAnimators;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerController : MonoBehaviour
    {
        public const int STUN_AMOUNT_SEC = 3;

        private const float WALK_SPEED = 5.0f;
        private const float RUN_SPEED = 10.0f;

        #region STATS
        [Header("Stats")]
        [SerializeField]
        private float _speed = 5.0f;
        [SerializeField]
        private float _lookSensitivity = 6.0f;
        [SerializeField]
        private float _thrusterForce = 0.01f;
        [SerializeField]
        private float _thrusterFuelBurnSpeed = 1f;
        [SerializeField]
        private float _thrusterFuelRegenSpeed = 0.3f;
        private float _thrusterFuelAmount = 1f;
        #endregion

        private PlayerMotor _motor;
        [SerializeField]
        private CustomNetworkAnimator _animator;

        [SerializeField]
        private SkinnedMeshRenderer _bodyRenderer;
        [SerializeField]
        private Material _assistantMaterial;
        [SerializeField]
        private Material _professorMaterial;
        [SerializeField]
        private Material _studentMaterial;
        [SerializeField]
        private Material _onCaughtMaterial;

        private void Start()
        {
            _motor = GetComponent<PlayerMotor>();
            _animator = GetComponent<CustomNetworkAnimator>();
        }

        public float GetThrusterFuelAmount()
        {
            return _thrusterFuelAmount;
        }

        public void SetStateMaterial(PlayerState playerState)
        {
            if (playerState == PlayerState.Professor)
            {
                _bodyRenderer.material = _professorMaterial;
            }
            else if (playerState == PlayerState.Assistant)
            {
                _bodyRenderer.material = _assistantMaterial;
            }
        }

        public void PlayCatchAnimation()
        {
            _animator.Play("Catch");
        }

        /// <summary>
        /// This is called by all target players.
        /// </summary>
        public void TransformToAssistant()
        {
            _animator.Animator.Play("Transform");
            SetStateMaterial(PlayerState.Assistant);
        }

        public void SetOnCaughtByAssistant(bool isReleased)
        {
            if (isReleased)
            {
                _bodyRenderer.material = _studentMaterial;
                _animator.SetBoolLocal("IsStunned", false);
            }
            else
            {
                _bodyRenderer.material = _onCaughtMaterial;
                _animator.Animator.Play("Stunned");
            }
        }

        private void FixedUpdate()
        {
            if (GameManager.DisableControl)
            {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                _motor.SetSpeed(RUN_SPEED);                
            }
            else
            {
                _motor.SetSpeed(WALK_SPEED);
            }

            float xMov = Input.GetAxisRaw("Horizontal");
            float zMov = Input.GetAxisRaw("Vertical");

            Vector3 movHorizontal = transform.right * xMov;
            Vector3 movVertical = transform.forward * zMov;
            Vector3 velocity = (movHorizontal + movVertical) * _speed * Time.fixedDeltaTime;

            var isWalking = zMov != 0 || xMov != 0;

            _animator.PlayWalkOrRun(isWalking, zMov, _motor.Speed);

            _motor.Move(velocity);

            float yRot = Input.GetAxisRaw("Mouse X");

            Vector3 rotation = new Vector3(0, yRot, 0) * _lookSensitivity;

            _motor.Rotate(rotation);

            float xRot = Input.GetAxisRaw("Mouse Y");

            _motor.RotateCamera(xRot * _lookSensitivity);

            Vector3 thrusterForce = Vector3.zero;
            if (Input.GetButton("Jump") && _thrusterFuelAmount >= 0)
            {
                _animator.Jump(true);
                _thrusterFuelAmount -= _thrusterFuelBurnSpeed * Time.fixedDeltaTime;

                if (_thrusterFuelAmount >= 0.01f)
                {
                    thrusterForce = Vector3.up * _thrusterForce;
                }
            }
            else
            {
                _animator.Jump(false);
                _thrusterFuelAmount += _thrusterFuelRegenSpeed * Time.fixedDeltaTime;
            }

            _thrusterFuelAmount = Mathf.Clamp(_thrusterFuelAmount, 0, 1);

            _motor.ApplyThruster(thrusterForce);
        }
    }
}

