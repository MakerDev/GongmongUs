using Mirror;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class MouseMove : MonoBehaviour
    {
        public float MouseSensitivity = 260f;

        public Transform PlayerBody;

        private float _xRotation = 0f;
        private float _maxRotation = 50f;

        void Update()
        {
            if (GameManager.Instance.DisableControl)
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.fixedDeltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.fixedDeltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -_maxRotation, _maxRotation);

            transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);

            PlayerBody.Rotate(Vector3.up * mouseX);
        }
    }
}