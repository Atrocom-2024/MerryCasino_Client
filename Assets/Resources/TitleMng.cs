using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TitleMng : MonoBehaviour
{
    public GameObject TitleImg;

    public GameObject LoginBtn;

    public float OnTitleTime;
    public float OnLoginTime;

    // Start is called before the first frame update
    void Start()
    {
        Invoke("titleImgOn", OnTitleTime);

    }

    public void titleImgOn()
    {
        TitleImg.SetActive(true);
        Invoke("loginBtnOn", OnLoginTime);
    }

    public void loginBtnOn()
    {
        
        LoginBtn.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
