using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RunAwaySubMission : MiniGame
{
    private int spacekey = 0;
    private int Teacher_show = 0;
    private int Teacher_see_num = 0;
    private Image Teacher_Image;
    private Image Danbeg_Image;

    [SerializeField]
    private GameObject danbeg;

    [SerializeField]
    private GameObject teacher;

    [SerializeField]
    private GameObject door;

    [SerializeField]
    private Sprite teacher_front;

    [SerializeField]
    private float speed;

    [SerializeField]
    private Sprite teacher_back;

    [SerializeField]
    private Sprite danbeg_stand;

    [SerializeField]
    private Sprite danbeg_sit;
  

    public void Cancle() => CancelMiniGame();

    // Start is called before the first frame update
    void Start()
    {
        Teacher_Image = teacher.GetComponent<Image>();
        Danbeg_Image = danbeg.GetComponent<Image>();
        InvokeRepeating("Teacher_see", 1, 1);
    }

    void Teacher_see()
    {
        if (Random.Range(0, 5) == 3 && Teacher_see_num < 3)
        {
            Teacher_Image.sprite = teacher_front;
            Invoke("Teacher_showing", 1);
            Invoke("Teacher_back", 3);
        }
    }

    void Teacher_showing()
    {
        Teacher_show = 1;
    }

    void Teacher_back()
    {
        Teacher_see_num++;
        Teacher_show = 0;
        Teacher_Image.sprite = teacher_back;
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            spacekey = 1;
        }

        if (Input.GetKeyUp(KeyCode.Space) && Input.GetKey(KeyCode.DownArrow) == false)
        {
            if (spacekey == 1)
            {
                spacekey = 0;
                danbeg.transform.Translate(new Vector3(1, 0, 0) * speed);


                Debug.Log(danbeg.transform.position.x);
                Debug.Log(door.transform.position.x);
            }
        }

        if(Teacher_show == 1)
        {
            if(Input.GetKey(KeyCode.DownArrow) == false)
            {
                CancelMiniGame();
            }
        }

        if (Input.GetKey(KeyCode.DownArrow) == true)
        {
            Danbeg_Image.sprite = danbeg_sit;
        }

        if (Input.GetKey(KeyCode.DownArrow) == false)
        {
            Danbeg_Image.sprite = danbeg_stand;
        }

        if (danbeg.transform.position.x > door.transform.position.x)
        {
            MiniGameResult.Passed = true;
            CompleteMiniGame(MiniGameResult);
        }
    }
}
