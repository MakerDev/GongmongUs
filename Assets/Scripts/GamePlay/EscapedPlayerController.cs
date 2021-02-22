using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapedPlayerController : MonoBehaviour
{
    public static EscapedPlayerController Instance { get; private set; } = null;

    [SerializeField]
    private float _speed;      // 캐릭터 움직임 스피드.
    [SerializeField]
    private float _jumpSpeed; // 캐릭터 점프 힘.
    [SerializeField]
    private float _gravity;    // 캐릭터에게 작용하는 중력.

    private CharacterController _controller; // 현재 캐릭터가 가지고있는 캐릭터 컨트롤러 콜라이더.
    private Vector3 _moveDir;                // 캐릭터의 움직이는 방향.

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Start()
    {
        _moveDir = Vector3.zero;
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (Player.LocalPlayer.HasExited == false)
        {
            return;
        }

        // 현재 캐릭터가 땅에 있는가?
        if (_controller.isGrounded)
        {
            // 위, 아래 움직임 셋팅. 
            _moveDir = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            // 벡터를 로컬 좌표계 기준에서 월드 좌표계 기준으로 변환한다.
            _moveDir = transform.TransformDirection(_moveDir);
            // 스피드 증가.
            _moveDir *= _speed;

            // 캐릭터 점프
            if (Input.GetButton("Jump"))
                _moveDir.y = _jumpSpeed;
        }

        // 캐릭터에 중력 적용.
        _moveDir.y -= _gravity * Time.deltaTime;

        // 캐릭터 움직임.
        _controller.Move(_moveDir * Time.deltaTime);
    }
}
