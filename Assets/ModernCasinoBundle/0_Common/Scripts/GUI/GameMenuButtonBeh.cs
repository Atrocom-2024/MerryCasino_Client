using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class GameMenuButtonBeh : MonoBehaviour
    {
        [SerializeField]
        private Button b; 
        [SerializeField]
        private Transform menuPanel;
        [SerializeField]
        private Transform endT;
        private bool start = true;
        Vector3 startPos;
        Vector3 endPos;

        private void Start()
        {
            if (menuPanel) startPos = menuPanel.position;
            if (endT) endPos = endT.position;
            b = GetComponent<Button>();
        }

        public void Click()
        {
            if (!endT || !menuPanel) return;
            Vector3 sP = (start) ? startPos : endPos;
            Vector3 eP = (!start) ? startPos : endPos;

            if (b)
            {
                if (start) b.SetPressed();
                else b.Release();
                b.interactable = false;
            }
            if (menuPanel && endT) SimpleTween.Move(menuPanel.gameObject, sP, eP, 0.1f).AddCompleteCallBack(()=> { if(b)b.interactable = true; }); 
            start = !start;
        }
    }
}