using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapedPlayerMouseMove : MonoBehaviour
{
    public float MouseSensitivity = 260f;

    private float _xRotation = 0f;
    private float _maxRotation = 50f;

    [SerializeField]
    private Transform _playerBody;

    void Update()
    {
        if (Player.LocalPlayer.HasExited == false)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity * Time.fixedDeltaTime;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -_maxRotation, _maxRotation);

        transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);

        _playerBody.Rotate(Vector3.up * mouseX);
    }
}
