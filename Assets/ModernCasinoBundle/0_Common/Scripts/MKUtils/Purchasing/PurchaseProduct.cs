using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    16.09.2020 - first
*/

namespace Mkey
{
    public class PurchaseProduct : MonoBehaviour
    {
        private Purchaser P { get { return Purchaser.Instance; } }

        public string productID;

        public void PurchaseByID()
        {
            Debug.Log($"{productID} 제품 구매 시도");
            if (P) P.BuyProductID(productID);
        }
    }
}