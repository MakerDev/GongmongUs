using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElevatorMiniGame : MiniGame
{
    [SerializeField]
    private float _speed = 2.0f;
    [SerializeField]
    private Text _fail;
    [SerializeField]
    private Text _success;
    [SerializeField]
    private GameObject _danbeg;

    void Update()
    {
        if (Input.GetKey(KeyCode.UpArrow))
        {
            _danbeg.transform.Translate(new Vector3(0, 1, 0) * _speed);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            _danbeg.transform.Translate(new Vector3(0, -1, 0) * _speed);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _danbeg.transform.Translate(new Vector3(-1, 0, 0) * _speed);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            _danbeg.transform.Translate(new Vector3(1, 0, 0) * _speed);
        }
    }    

    public override void ResetGame()
    {
        _fail.text = "";
        _success.text = "";
        _danbeg.GetComponent<RectTransform>().anchoredPosition = new Vector3(300, 0, 0);

        Debug.Log("Resetted");
    }

    public void Fail()
    {
        _fail.text = "Fail";
        Invoke(nameof(FailMiniGame), _offTime);
    }

    public void Success()
    {
        _success.text = "Success";
        MiniGameResult.Passed = true;
        Invoke(nameof(CallCompleteMiniGame), _offTime);
    }

    private void CallCompleteMiniGame()
    {
        CompleteMiniGame(MiniGameResult);
    }
}
