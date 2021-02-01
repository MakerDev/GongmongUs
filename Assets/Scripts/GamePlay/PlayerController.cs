using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
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
        private Animator _animator;

        private void Start()
        {
            _motor = GetComponent<PlayerMotor>();
            _animator = GetComponent<Animator>();
        }

        public float GetThrusterFuelAmount()
        {
            return _thrusterFuelAmount;
        }

        private void FixedUpdate()
        {
            float xMov = Input.GetAxisRaw("Horizontal");
            float zMov = Input.GetAxisRaw("Vertical");

            Vector3 movHorizontal = transform.right * xMov;
            Vector3 movVertical = transform.forward * zMov;

            Vector3 velocity = (movHorizontal + movVertical) * _speed * Time.fixedDeltaTime;

            _animator.SetFloat("ForwardVelocity", zMov);
            _motor.Move(velocity);

            float yRot = Input.GetAxisRaw("Mouse X");

            Vector3 rotation = new Vector3(0, yRot, 0) * _lookSensitivity;

            _motor.Rotate(rotation);

            float xRot = Input.GetAxisRaw("Mouse Y");

            _motor.RotateCamera(xRot * _lookSensitivity);

            Vector3 thrusterForce = Vector3.zero;
            if (Input.GetButton("Jump") && _thrusterFuelAmount >= 0)
            {
                _thrusterFuelAmount -= _thrusterFuelBurnSpeed * Time.fixedDeltaTime;

                if (_thrusterFuelAmount >= 0.01f)
                {
                    thrusterForce = Vector3.up * _thrusterForce;
                }
            }
            else
            {
                _thrusterFuelAmount += _thrusterFuelRegenSpeed * Time.fixedDeltaTime;
            }

            _thrusterFuelAmount = Mathf.Clamp(_thrusterFuelAmount, 0, 1);

            _motor.ApplyThruster(thrusterForce);
        }
    }
}

