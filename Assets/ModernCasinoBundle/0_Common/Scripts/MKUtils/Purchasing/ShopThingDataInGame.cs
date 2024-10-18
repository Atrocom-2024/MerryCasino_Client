
/*
    31.10.2020 
 */

namespace Mkey
{
    [System.Serializable]
    public class ShopThingDataInGame : ShopThingData
    {
        public float thingPrice;

        public ShopThingDataInGame(ShopThingDataInGame prod) : base(prod)
        {
            if (prod == null) return;
            thingPrice = prod.thingPrice;
        }
    }
}