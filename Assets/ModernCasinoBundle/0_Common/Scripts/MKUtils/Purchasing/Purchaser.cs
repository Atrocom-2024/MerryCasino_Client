using System;
using UnityEngine;

/* changes
 28032019
  -add webgl purchasing stub

 -  28032019
  -add life
  -set infinite life

 -31072019
   -add purchase events component

 -30.08.19
    -add #define NOIAP - symbol

09.03.2020 
        - change  NOIAP -> ADDIAP – symbol (from player settings)

30.06.2020
        - add events, remove reference to purchaseeven script

        public Action <string, string> GoodPurchaseEvent;   // <id, name>
        public Action <string, string> FailedPurchaseEvent; // <id, name>

16.09.2020 
    -  public void BuyProductID(string productId)

10.12.2020
    - remove shppthingdatareal

18.05.2021
   -  FailedPurchaseEvent?.Invoke(productId, "unknown");
 */
#if ((UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID) && ADDIAP)
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif
/*
 Integrating Unity IAP In Your Game 
 https://unity3d.com/learn/tutorials/topics/ads-analytics/integrating-unity-iap-your-game

*/

namespace Mkey
{

#if ((UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID) && ADDIAP)
    // Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
    public class Purchaser : MonoBehaviour, IDetailedStoreListener
#else
    public class Purchaser : MonoBehaviour  // 인앱 결제와 관련된 주요 로직을 담당하는 클래스
#endif

    {
        [Header("Consumables: ", order = 1)]
        public ShopThingData[] consumable; // 소비성 제품: 구매 후 사용할 수 있는 제품(게임 내 코인, 아이템 등)

        //[Header("Non consumables: ", order = 1)]
        //public ShopThingData[] nonConsumable;  // 비소비성 제품: 한 번 구매하면 영구적으로 사용할 수 있는 제품(특정 기능 해제, 광고 제거 등)

        //[Header("Subscriptions: ", order = 1)]
        //public ShopThingData[] subscriptions; // 구독형 제품

        public static Purchaser Instance;  // 싱글톤 패턴 -> 게임에서 이 클래스를 하나의 인스턴스만 유지하기 위해 사용

        public PurchaseProduct purchaseProduct;

        #region events
        public Action <string, string> GoodPurchaseEvent;   // <id, name>
        public Action <string, string> FailedPurchaseEvent; // <id, name>
        #endregion events

#if ((UNITY_EDITOR || UNITY_IOS || UNITY_ANDROID) && ADDIAP)

        private static IStoreController m_StoreController; // 구매 과정을 제어하는 함수 제공자
        private static IExtensionProvider m_StoreExtensionProvider; // 여러 플랫폼을 위한 확장 처리 제공자

        [Space(8, order = 0)]
        [Header("Store keys: ", order = 1)]
        public string appKey = "com.Atrocom.MerryCasino"; 
        public string googleKey = "com.Atrocom.MerryCasino";

        void Awake()
        {
            if (Instance) Destroy(gameObject);
            else
            {
                Instance = this;
            }
        }

        void Start() // initialize purchaser
        {
            // If we haven't set up the Unity Purchasing reference
            if (m_StoreController == null)
            {
                InitializePurchasing(); // Begin to configure our connection to Purchasing
            }
        }

        public void InitializePurchasing()
        {
            if (IsInitialized()) // If we have already connected to Purchasing ...
            {
                return;
            }

            // Create a builder, first passing in a suite of Unity provided stores.
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            #region build consumables
            for (int i = 0; i < consumable.Length; i++)
            {
                string productId = consumable[i].kProductID;
                builder.AddProduct(productId, ProductType.Consumable, new IDs() { { productId, GooglePlay.Name } });
            }

            //builder.AddProduct("coin_pack_1", ProductType.Consumable, new IDs() { { "coin_pack_1", GooglePlay.Name } });
            //builder.AddProduct("coin_pack_2", ProductType.Consumable, new IDs() { { "coin_pack_2", GooglePlay.Name } });
            //builder.AddProduct("coin_pack_3", ProductType.Consumable, new IDs() { { "coin_pack_3", GooglePlay.Name } });
            //builder.AddProduct("coin_pack_4", ProductType.Consumable, new IDs() { { "coin_pack_4", GooglePlay.Name } });
            #endregion build consumables

            UnityPurchasing.Initialize(this, builder);
        }

        private bool IsInitialized()
        {
            // Only say we are initialized if both the Purchasing references are set.
            return m_StoreController != null && m_StoreExtensionProvider != null;
        }

        public void BuyProductID(string productId)
        {
            // If the stores throw an unexpected exception, use try..catch to protect my logic here.
            try
            {
                // If Purchasing has been initialized ...
                if (IsInitialized())
                {
                    // ... look up the Product reference with the general product identifier and the Purchasing system's products collection.
                    Product product = m_StoreController.products.WithID(productId);

                    // If the look up found a product for this device's store and that product is ready to be sold ... 
                    if (product != null && product.availableToPurchase)
                    {
                        Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously.
                        m_StoreController.InitiatePurchase(product);
                    }
                    // Otherwise ...
                    else
                    {
                        // ... report the product look-up failure situation  
                        Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                    }
                }
                // Otherwise ...
                else
                {
                    // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or retrying initiailization.
                    Debug.Log("BuyProductID FAIL. Not initialized.");
                }
            }
            // Complete the unexpected exception handling ...
            catch (Exception e)
            {
                // ... by reporting any unexpected exception for later diagnosis.
                Debug.Log("BuyProductID: FAIL. Exception during purchase. " + e);
            }
        }

        /// <summary>
        /// Restore purchases previously made by this customer. Some platforms automatically restore purchases.
        /// Apple currently requires explicit purchase restoration for IAP.
        /// </summary>
        public void RestorePurchases()
        {
            // If Purchasing has not yet been set up ...
            if (!IsInitialized())
            {
                // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                return;
            }

            // 애플 유저
            //if (Application.platform == RuntimePlatform.IPhonePlayer ||
            //    Application.platform == RuntimePlatform.OSXPlayer)
            //{
            //    // ... begin restoring purchases
            //    Debug.Log("RestorePurchases started ...");

            //    // Fetch the Apple store-specific subsystem.
            //    var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            //    // Begin the asynchronous process of restoring purchases. Expect a confirmation response in the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
            //    apple.RestoreTransactions((result) =>
            //    {
            //    // The first phase of restoration. If no more responses are received on ProcessPurchase then no purchases are available to be restored.
            //    Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            //    });
            //}
            // Otherwise ...
            else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
            }
        }

        #region IStoreListener
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            m_StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            m_StoreExtensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            Debug.Log("OnInitializeFailed" + error);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            ShopThingData prod = GetProductById(args.purchasedProduct.definition.id);
            if (prod != null)
            {
                Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", prod.kProductID));
                if (prod.PurchaseEvent != null)
                {
                    prod.PurchaseEvent.Invoke();
                }
                else
                {
                    Debug.Log("PurchaseEvent failed");
                }
                GoodPurchaseEvent?.Invoke(prod.kProductID, prod.name);
            }

            else // Or ... an unknown product has been purchased by this user. Fill in additional products here.
            {
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }// Return a flag indicating wither this product has completely been received, or if the application needs to be reminded of this purchase at next app launch. Is useful when saving purchased products to the cloud, and when that save is delayed.

            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            ShopThingData prod = GetProductById(product.definition.id);
            if (prod != null)
            {
                FailedPurchaseEvent?.Invoke(prod.kProductID, prod.name);
            }

            // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing this reason with the user.
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }
        #endregion IStoreListener

        ShopThingData GetProductById(string id)
        {
            if (consumable != null && consumable.Length > 0)
                for (int i = 0; i < consumable.Length; i++)
                {
                    if (consumable[i] != null)
                        if (String.Equals(id, consumable[i].kProductID, StringComparison.Ordinal))
                            return consumable[i];
                }

            //if (nonConsumable != null && nonConsumable.Length > 0)
            //    for (int i = 0; i < nonConsumable.Length; i++)
            //    {
            //        if (nonConsumable[i] != null)
            //            if (String.Equals(id, nonConsumable[i].kProductID, StringComparison.Ordinal))
            //                return nonConsumable[i];
            //    }

            //if (subscriptions != null && subscriptions.Length > 0)
            //    for (int i = 0; i < subscriptions.Length; i++)
            //    {
            //        if (subscriptions[i] != null)
            //            if (String.Equals(id, subscriptions[i].kProductID, StringComparison.Ordinal))
            //                return subscriptions[i];
            //    }
            return null;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message) {
            throw new NotImplementedException();
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            throw new NotImplementedException();
        }

#else
        void Awake()
        {
            if (Instance) Destroy(gameObject);
            else
            {
                Instance = this;
            }
        }

        void Start() 
        {
            InitializeProducts();
            InitializePurchasing();
        }

        public void InitializePurchasing()
        {
            // 인앱 결제(IAP) 제품들을 Unity의 결제 시스템에 등록하고, 각 제품이 구매될 수 있도록 이벤트를 설정
            // 소비성(consumable), 비소비성(non-consumable), 그리고 구독형(subscription) 제품들이 결제될 수 있도록 준비하는 역할

            // 유니티가 제공하는 스토어 모음을 먼저 통과하는 빌더를 만듭니다.
            // 일반 식별자와 스토어별 식별자를 연결하여 해당 식별자를 통해 판매/복원할 제품을 추가
            #region build 

            if (consumable != null && consumable.Length > 0)
            {
                for (int i = 0; i < consumable.Length; i++)
                {
                    if (consumable[i] != null && !string.IsNullOrEmpty(consumable[i].kProductID))
                    {
                        string prodID = consumable[i].kProductID;
                        consumable[i].clickEvent.RemoveAllListeners();
                        consumable[i].clickEvent.AddListener(() => { BuyProductID(prodID); });
                    }
                }
            }

            if (nonConsumable != null && nonConsumable.Length > 0)
            {
                for (int i = 0; i < nonConsumable.Length; i++)
                {
                    if (nonConsumable[i] != null && !string.IsNullOrEmpty(nonConsumable[i].kProductID))
                    {
                        string prodID = nonConsumable[i].kProductID;
                        nonConsumable[i].clickEvent.RemoveAllListeners();
                        nonConsumable[i].clickEvent.AddListener(() => { BuyProductID(prodID); });
                    }
                }
            }
            if (subscriptions != null && subscriptions.Length > 0)
            {
                for (int i = 0; i < subscriptions.Length; i++)
                {
                    if (subscriptions[i] != null && !string.IsNullOrEmpty(subscriptions[i].kProductID))
                    {
                        string prodID = subscriptions[i].kProductID;
                        nonConsumable[i].clickEvent.RemoveAllListeners();
                        nonConsumable[i].clickEvent.AddListener(() => { BuyProductID(prodID); });
                    }
                }
            }
    #endregion build 
        }
        private void InitializeProducts() {
            // 소비성 제품 배열 초기화
            consumable = new ShopThingData[4];
            consumable[0] = new ShopThingData() {
                name = "500000 Coins",
                kProductID = "coin_pack_1",
                PurchaseEvent = new UnityEvent(),
                clickEvent = new Button.ButtonClickedEvent()
            };

            consumable[1] = new ShopThingData() {
                name = "1000000 Coins",
                kProductID = "coin_pack_2",
                PurchaseEvent = new UnityEvent(),
                clickEvent = new Button.ButtonClickedEvent()
            };
            
            consumable[2] = new ShopThingData() {
                name = "5000000 Coins",
                kProductID = "coin_pack_3",
                PurchaseEvent = new UnityEvent(),
                clickEvent = new Button.ButtonClickedEvent()
            };
            
            consumable[3] = new ShopThingData() {
                name = "10000000 Coins",
                kProductID = "coin_pack_4",
                PurchaseEvent = new UnityEvent(),
                clickEvent = new Button.ButtonClickedEvent()
            };
        }

        /// <summary>
        /// 실제 제품 구매를 담당하는 메서드. 서버로 구매 요청은 해당 메서드에서 진행
        /// </summary>
        /// <param name="productId"></param>
        public void BuyProductID(string productId)
        {
            ShopThingData prod = GetProductById(productId);
            if (prod != null)
            {
                prod.PurchaseEvent?.Invoke();
                GoodPurchaseEvent?.Invoke(productId, prod.name);
            }
            else
            {
                FailedPurchaseEvent?.Invoke(productId, "Unknown product");
            }
        }

        private ShopThingData GetProductById(string id)
        {
            if (consumable != null && consumable.Length > 0)
                for (int i = 0; i < consumable.Length; i++)
                {
                    if (consumable[i] != null)
                        if (String.Equals(id, consumable[i].kProductID, StringComparison.Ordinal))
                            return consumable[i];
                }

            if (nonConsumable != null && nonConsumable.Length > 0)
                for (int i = 0; i < nonConsumable.Length; i++)
                {
                    if (nonConsumable[i] != null)
                        if (String.Equals(id, nonConsumable[i].kProductID, StringComparison.Ordinal))
                            return nonConsumable[i];
                }

            if (subscriptions != null && subscriptions.Length > 0)
                for (int i = 0; i < subscriptions.Length; i++)
                {
                    if (subscriptions[i] != null)
                        if (String.Equals(id, subscriptions[i].kProductID, StringComparison.Ordinal))
                            return subscriptions[i];
                }
            return null;
        }

        //public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) {
        //    ShopThingData prod = GetProductById(args.purchasedProduct.definition.id);
        //    if (prod != null) {
        //        Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", prod.kProductID));
        //        if (prod.PurchaseEvent != null) {
        //            prod.PurchaseEvent.Invoke();
        //        } else {
        //            Debug.Log("PurchaseEvent failed");
        //        }
        //        GoodPurchaseEvent?.Invoke(prod.kProductID, prod.name);

        //        // 서버로 결제 성공 데이터 전송
        //        StartCoroutine(SendPurchaseDataToServer(prod.kProductID, prod.name));
        //    } else {
        //        Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
        //    }

        //    // Return a flag indicating wither this product has completely been received, 
        //    // or if the application needs to be reminded of this purchase at next app launch. 
        //    return PurchaseProcessingResult.Complete;
        //}

        //private IEnumerator SendPurchaseDataToServer(string productId) {
        //    // JSON 데이터 생성
        //    string userId = "user123"; // 유저 ID, 실제 유저 ID로 교체
        //    string jsonData = JsonUtility.ToJson(new PurchaseData {
        //        userId = userId,
        //        productId = productId,
        //        purchaseDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        //    });

        //    // UnityWebRequest를 사용하여 서버에 POST 요청
        //    using (UnityWebRequest request = new UnityWebRequest("http://your-node-server.com/api/purchase", "POST")) {
        //        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        //        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        //        request.downloadHandler = new DownloadHandlerBuffer();
        //        request.SetRequestHeader("Content-Type", "application/json");

        //        // 요청을 보내고 대기
        //        yield return request.SendWebRequest();

        //        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError) {
        //            Debug.LogError($"Server request failed: {request.error}");
        //        } else {
        //            Debug.Log($"Server response: {request.downloadHandler.text}");
        //        }
        //    }
        //}
#endif

    }
}
