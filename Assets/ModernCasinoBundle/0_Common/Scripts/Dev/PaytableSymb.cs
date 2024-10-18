using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class PaytableSymb : MonoBehaviour
    {
        public Text pay5x;
        public Text pay4x;
        public Text pay3x;
        public Image image;

        public void Create(RectTransform parent, string pay5xS, string pay4xS, string pay3xS, string prefix, Sprite symbSprite, bool setNativeSize)
        {
            TextExtension.SetText(pay5x, prefix + pay5xS);
            TextExtension.SetText(pay4x, prefix + pay4xS);
            TextExtension.SetText(pay3x, prefix + pay3xS);
            ImageExtension.SetImageSprite(image, symbSprite, setNativeSize);
            PaytableSymb pS = Instantiate(this, parent);
            pS.gameObject.name = name + "(" + symbSprite.name + ")";
        }
    }
}
