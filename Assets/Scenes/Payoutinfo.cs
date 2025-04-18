using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Payoutinfo : MonoBehaviour
{
    public GameObject infoPopup;
    public static GameObject _infoPopup;
    public GameObject returnPopup;
    public static GameObject _returnPopup;
    
    // Start is called before the first frame update
    void Start()
    {
        _infoPopup = infoPopup;
        _returnPopup = returnPopup;
        DontDestroyOnLoad(this.gameObject);
        _infoPopup.SetActive(false);
    }

    public void offBtn()
    {
        _infoPopup.SetActive(false);
    }



}
