using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Upload2SubMission : MiniGame
{
    public InputField input;

    private string answer = "2019142008김단벡결과레포트";


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Cancle()
    {
        CancelMiniGame();
    }
    
    public void Submit()
    {
        if (input.text.Equals(answer))
        {
            Debug.Log("성공");
            MiniGameResult.Passed = true;
            CompleteMiniGame(MiniGameResult);
        }
        else
        {
            Cancle();
        }
    }
}
