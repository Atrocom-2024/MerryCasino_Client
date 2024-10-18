using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    19.05.2021
    helper to create paytable help screen
    1) add script to symbols tab
    2) set symbols grid parent
    3) add prefab
    4) add inspector button, set editor and runtime mode. call method Create
 */
namespace Mkey
{
    public class CreatePaytableSymbs : MonoBehaviour
    {
        public PaytableSymb prefab;
        public Sprite[] sprites;
        public SlotController slotController;
        public RectTransform parent;
        public bool setNativeSize = false;

        public void Create()
        {
            Debug.Log("Create symbols: " + gameObject);
            if(sprites == null || sprites.Length == 0)
            {
                Debug.LogError("sprite array is empty");
                return;
            }

            if (!prefab)
            {
                Debug.LogError("prefab - failed");
                return;
            }

            if (!slotController)
            {
                Debug.LogError("slotController - failed, try to find in hierarchy");
                slotController = FindObjectOfType<SlotController>();
            }

            if (!slotController)
            {
                Debug.LogError("slotController - failed");
                return;
            }

            if (!parent)
            {
                Debug.LogError("parent - failed");
                return;
            }

            SlotIcon[] slotIcons = slotController.slotIcons;

           
            List<PayLine> payTable = slotController.payTable;

            Debug.Log("Paytable length: " + payTable.Count);

            PaytableSymb[] pSs = parent.GetComponentsInChildren<PaytableSymb>();
            foreach (var item in pSs)
            {
                DestroyImmediate(item.gameObject);
            }

            foreach (var item in sprites)
            {
                if (item)
                {
                    SlotIcon sI = GetIconBySprite(slotIcons, item);
                    int id = GetIndexBySprite(slotIcons, item);
                    Debug.Log("Sprite: " + item + "; ID: " + id);

                    PayLine payLine5x = GetPayLineForIndex(payTable, id, 5, 5);
                    if (payLine5x != null)
                    {
                        Debug.Log("payLine5x found: " + id);
                        PrintData.BufTostring(payLine5x.line, payLine5x.line.Length, 1, "", ' ');
                    }

                    PayLine payLine4x = GetPayLineForIndex(payTable, id, 4, 5);
                    if (payLine4x != null)
                    {
                        Debug.Log("payLine4x found: " + id);
                        PrintData.BufTostring(payLine4x.line, payLine4x.line.Length, 1, "", ' ');
                    }

                    PayLine payLine3x = GetPayLineForIndex(payTable, id, 3, 5);
                    if (payLine3x != null)
                    {
                        Debug.Log("payLine3x found: " + id);
                        PrintData.BufTostring(payLine3x.line, payLine3x.line.Length, 1, "", ' ');
                    }

                    prefab.Create(parent,payLine5x != null ? payLine5x.pay.ToString() :"0", payLine4x != null ? payLine4x.pay.ToString() : "0", payLine3x != null ? payLine3x.pay.ToString() : "0"," - ", item, setNativeSize);
                }
            }
        }

        private SlotIcon GetIconBySprite(SlotIcon[] slotIcons, Sprite s)
        {
            SlotIcon res = null;
            if(slotIcons == null)
            {
                Debug.LogError("slotcontroller sloticons array is empty");
                return res;
            }

            foreach (var item in slotIcons)
            {
                if (item.iconSprite == s) return item;
            }
            return res;
        }

        private int GetIndexBySprite(SlotIcon[] slotIcons, Sprite s)
        {
            int res = -1;
            if (slotIcons == null)
            {
                Debug.LogError("slotcontroller sloticons array is empty");
                return res;
            }

            int i = 0;
            foreach (var item in slotIcons)
            {
                if (item.iconSprite == s) return i;
                i++;
            }
            return res;
        }

        private PayLine GetPayLineForIndex(List <PayLine> payLines, int index, int count, int length)
        {
            PayLine res = null;

            if (payLines== null || payLines.Count == 0)
            {
                Debug.LogError("slot controller paylines array is empty");
                return res;
            }
            int i = 0;
            foreach (var item in payLines)
            {
                i++;
           //     Debug.Log("LINE ORDER NUMBER - " + i);
           //    PrintData.BufTostring(item.line, item.line.Length, 1,  "Line length : "+ item.line.Length +  " ;Search ID " + index + "; line ids : ", ' ');
                if (item!=null && item.line != null && item.line.Length == length)
                {
                    if (IsArrayWithIndexes(item.line, index, count)) return item;
                }
            }
            return res;
        }

        private bool IsArrayWithIndexes(int[] indexes, int index, int count)
        {
            if (indexes == null || count > indexes.Length) return false;
            for (int i = 0; i < indexes.Length; i++)
            {
                if ((i < count && indexes[i]!= index) || (i >= count && indexes[i] == index))
                {
                    return false;
                }
            }
            return true;
        }
    }
}