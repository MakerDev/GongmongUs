﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EmailMiniGame : MiniGame
{
    public GameObject Success_text;
    public GameObject Fail_text;

    public Image answer;

    public Sprite step2;
    public Sprite step3;
    public Sprite step4;
    public Sprite step5;

    public GameObject First;
    public GameObject Second;
    public GameObject Third;
    public GameObject Fourth;


    public void Cancel()
    {
        CancelMiniGame();
    }

    public void Success_first()
    {
        answer.sprite = step2;
        First.SetActive(false);
        Second.SetActive(true);
    }

    public void Success_second()
    {
        answer.sprite = step3;
        Second.SetActive(false);
        Third.SetActive(true);
    }

    public void Success_third()
    {
        answer.sprite = step4;
        Third.SetActive(false);
        Fourth.SetActive(true);
    }

    public void Success_end()
    {
        Fourth.SetActive(false);
        answer.sprite = step5;
        Success_text.SetActive(true);
        Invoke("Complete", 2);
    }

    public void Fail()
    {
        Fail_text.SetActive(true);
        Invoke("Cancel", 2);
    }

    public void Complete()
    {
        MiniGameResult.Passed = true;
        CompleteMiniGame(MiniGameResult);
    }
}
