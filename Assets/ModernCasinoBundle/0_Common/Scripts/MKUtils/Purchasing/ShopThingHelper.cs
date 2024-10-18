using UnityEngine;
using UnityEngine.UI;

/*
    30.06.2020 - first
*/

namespace Mkey
{
    public class ShopThingHelper : MonoBehaviour
    {
        public Button thingBuyButton;

        public ShopThingHelper Create(RectTransform parent, ShopThingData shopThingData)
        {
            ShopThingHelper shopThing = Instantiate(this, parent);
            shopThing.transform.localScale = Vector3.one;

            if (shopThing.thingBuyButton)
            {
                shopThing.thingBuyButton.onClick.RemoveAllListeners();
                shopThing.thingBuyButton.onClick = shopThingData.clickEvent;
            }
            return shopThing;
        }
    }
}