using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SongdoSkySubMission : MiniGame
{
    private int[] x_position = new int[5] { 644, 694, -43, 104, 614 };
    private int[] y_position = new int[5] { -97, 222, 212, -217, -357 };

    private float answer_x;
    private float answer_y;

    [SerializeField]
    private Image image;

    [SerializeField]
    private float speed;

    [SerializeField]
    private float error_margin;

    [SerializeField]
    private Image answer_image;

    [SerializeField]
    private Sprite one;

    [SerializeField]
    private Sprite two;

    [SerializeField]
    private Sprite three;

    [SerializeField]
    private Sprite four;

    [SerializeField]
    private Sprite five;

    // Start is called before the first frame update
    void Start()
    {
        int answer = Random.Range(0, 5);
        answer_x = x_position[answer];
        answer_y = y_position[answer];

        if(answer == 0)
        {
            answer_image.sprite = one;
        }
        else if(answer == 1)
        {
            answer_image.sprite = two;
        }
        else if(answer == 2)
        {
            answer_image.sprite = three;
        }
        else if(answer == 3)
        {
            answer_image.sprite = four;
        }
        else
        {
            answer_image.sprite = five;
        }

        Debug.Log(answer_x);
        Debug.Log(answer_y);
    }

    public void Right_move() {
        if (image.transform.position.x > -50)
        {
            Debug.Log(image.transform.position.x);
            Debug.Log(image.transform.position.y);
            image.transform.Translate(new Vector3(-1, 0, 0) * speed);
            Check_answer();
        }
    }

    public void Left_move() {
        if (image.transform.position.x < 754)
        {
            Debug.Log(image.transform.position.x);
            Debug.Log(image.transform.position.y);
            image.transform.Translate(new Vector3(1, 0, 0) * speed);
            Check_answer();
        }
    }

    public void Top_move()
    {
        if (image.transform.position.y > -600)
        {
            Debug.Log(image.transform.position.x);
            Debug.Log(image.transform.position.y);
            image.transform.Translate(new Vector3(0, -1, 0) * speed);
            Check_answer();
        }
    }

    public void Bottom_move()
    {
        if (image.transform.position.y < 642)
        {
            Debug.Log(image.transform.position.x);
            Debug.Log(image.transform.position.y);
            image.transform.Translate(new Vector3(0, 1, 0) * speed);
            Check_answer();
        }
    }

    public void Check_answer()
    {
        if(image.transform.position.y < answer_y + error_margin && image.transform.position.y > answer_y - error_margin)
        {
            if (image.transform.position.x < answer_x + error_margin && image.transform.position.x > answer_x - error_margin)
            {
                MiniGameResult.Passed = true;
                CompleteMiniGame(MiniGameResult);
            }
        }
    }

    public void Cancle() => CancelMiniGame();
}
