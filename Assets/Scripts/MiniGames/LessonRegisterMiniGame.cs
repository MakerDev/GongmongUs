using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LessonRegisterMiniGame : MiniGame
{
    [SerializeField]
    private Text timerText;

    [SerializeField]
    private List<Image> _registerButtons = new List<Image>();
    [SerializeField]
    private Text _successText;
    [SerializeField]
    private Text _failText;

    private float _timesLeft = 10;
    private int _allLecturesCount;
    private int _registeredCount;

    // Start is called before the first frame update
    void Start()
    {
        _allLecturesCount = 11;
        _registeredCount = 0;
    }

    private void FixedUpdate()
    {
        if (MiniGameResult.Passed)
        {
            return;
        }

        if (_timesLeft > 0)
            _timesLeft -= Time.fixedDeltaTime;

        timerText.text = "남은시간 : " + string.Format("{0:N2}", _timesLeft);

        if (_timesLeft <= 0)
        {
            _failText.gameObject.SetActive(true);
            Invoke(nameof(CancelMiniGame), 1.5f);
        }
    }

    public override void ResetGame()
    {
        base.ResetGame();

        _registeredCount = 0;
        _timesLeft = 10;
        _successText.gameObject.SetActive(false);
        _failText.gameObject.SetActive(false);

        foreach (var button in _registerButtons)
        {
            button.gameObject.SetActive(true);
        }
    }

    public void OnRegisterButtonClick(int index)
    {
        _registerButtons[index].gameObject.SetActive(false);

        _registeredCount += 1;

        if (_registeredCount >= _allLecturesCount)
        {
            MiniGameResult.Passed = true;
            _successText.gameObject.SetActive(true);
            Invoke(nameof(CompleteMission), 1.5f);
        }
    }


    private void CompleteMission()
    {
        CompleteMiniGame(MiniGameResult);
    }
}
