using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UploadSubMission : MiniGame
{
    private int[] result = new int[6] { 0, 0, 0, 0, 0, 0 };
    private int[] answer = new int[6] { 1, 0, 0, 0, 1, 0 };

    public Toggle Toggle1;
    public Toggle Toggle2;
    public Toggle Toggle3;
    public Toggle Toggle4;
    public Toggle Toggle5;
    public Toggle Toggle6;

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
        if (Toggle1.isOn)
            result[0] = 1;
        if (Toggle2.isOn)
            result[1] = 1;
        if (Toggle3.isOn)
            result[2] = 1;
        if (Toggle4.isOn)
            result[3] = 1;
        if (Toggle5.isOn)
            result[4] = 1;
        if (Toggle6.isOn)
            result[5] = 1;

        for(int i = 0; i < 6; i++)
        {
            if(result[i] != answer[i])
            {
                Cancle();
            }
        }
        Debug.Log("성공");
        MiniGameResult.Passed = true;
        CompleteMiniGame(MiniGameResult);

    }
}
