using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompressMissionMiniGame : MiniGame
{
    private const int TOGGLES_COUNT = 6;

    [SerializeField]
    private Toggle[] _toggles = new Toggle[TOGGLES_COUNT];
    private bool[] _answers = new bool[TOGGLES_COUNT] { true, false, false, false, true, false };

    public override void ResetGame()
    {
        foreach (var toggle in _toggles)
        {
            toggle.isOn = false;
        }
    }

    public void Cancle()
    {
        CancelMiniGame();
    }

    public void Submit()
    {
        for (int i = 0; i < 6; i++)
        {
            if (_toggles[i].isOn != _answers[i])
            {
                Debug.Log("Fail");
                FailMiniGame();
                return;
            }
        }

        MiniGameResult.Passed = true;
        CompleteMiniGame(MiniGameResult);
    }
}
