using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

//[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : NetworkBehaviour
{
    [SerializeField]
    private Camera _camera;

    private Vector3 _thrusterForce = Vector3.zero;

    [SerializeField]
    private float _cameraRotationLimit = 75f;
    private CharacterController _controller;

    [SerializeField]
    private Transform _groundCheck;
    private float _groundDistance = 0.2f;

    [SerializeField]
    private LayerMask _groundLayerMask;

    private bool _isGrounded = false;

    public float gravity = -9.8f;
    [SerializeField]
    private float _speed = 9f;

    private bool _isPlayingMiniGame = false;
    private Vector3 _velocity;
    private Vector3 _rotation;
    private float _cameraRotationX;

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        MiniGame.OnStartMiniGame += () =>
        {
            _isPlayingMiniGame = true;
        };
        MiniGame.OnCompletedMiniGame += (result) =>
        {
            _isPlayingMiniGame = false;
        };
    }

    public void Move(Vector3 velocity)
    {
        _velocity = velocity;
    }

    public void Rotate(Vector3 rotation)
    {
        _rotation = rotation;
    }

    public void RotateCamera(float rotationX)
    {
        _cameraRotationX = rotationX;
    }

    public void ApplyThruster(Vector3 thruster)
    {
        _thrusterForce = thruster;
    }

    private void FixedUpdate()
    {
        PerformMovement();
    }

    private void PerformMovement()
    {
        if ((!isLocalPlayer && !gameObject.activeSelf) || _isPlayingMiniGame)
        {
            return;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        _controller.Move(move * _speed * Time.deltaTime);

        _isGrounded = Physics.CheckSphere(_groundCheck.position, _groundDistance, _groundLayerMask);

        if (!_isGrounded && gameObject.transform.position.y >= 1)
        {
            var newPos = gameObject.transform.position - (Vector3.up * (5.3f) * Time.fixedDeltaTime);
            if (newPos.y <= 1)
            {
                newPos.y = 1;
            }

            gameObject.transform.position = newPos;
        }

        if (_thrusterForce != Vector3.zero)
        {
            _controller.Move(Vector3.up * 4f * Time.fixedDeltaTime);
        }
    }
}

