using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

namespace Mkey
{
    public class JackPotWin : MonoBehaviour
    {
        [SerializeField]
        private AudioClip coinsClip;

        #region temp vars
        private TextMesh jackPotTitle;
        [SerializeField]
        private TextMesh jackPotAmount;
        [SerializeField]
        private TextMesh jackPotAmountTemp;
        private LampsController[] lamps;
        private CoinProcAnim[] coinsFountains;
        private JackPotController jpController;
        private List<TextMesh> rend;
        private List<Color> colors;
        private SoundMaster MSound { get { return SoundMaster.Instance; } }
        private MeshRenderer mR;
        // 추가된 부분
        private Coroutine fountainCoroutine; // 코루틴 핸들 추가
        #endregion temp vars

        #region regular
        private void Awake()
        {
            // 추가된 부분
            rend = new List<TextMesh>();
            colors = new List<Color>();
        }

        private void Start()
        {
            // 1. JackPotController 연결 확인
            jpController = GetComponentInParent<JackPotController>();
            if (jpController == null)
            {
                Debug.LogWarning("JackPotController가 연결되지 않았습니다.");
                return;
            }

            // 2. 잭팟 렌더러(WinRenderer) 활성화
            foreach (var item in jpController.WinRenderers)
            {
                if (item != null) item.enabled = true;
            } 

            // 3. JackPotAmount 초기화
            jackPotAmount = jpController.JackPotAmount;
            if (jackPotAmount != null)
            {
                jackPotAmountTemp = Instantiate(jackPotAmount.gameObject, jackPotAmount.transform.position, jackPotAmount.transform.rotation).GetComponent<TextMesh>();

                mR = jackPotAmount.GetComponent<MeshRenderer>();
                if (mR) mR.enabled = false;

                rend.Add(jackPotAmountTemp); // 추가된 부분
            }
            else
            {
                Debug.LogWarning("❗ JackPotAmount가 연결되지 않았습니다.");
            }

            // 4. JackPotTitle 초기화
            jackPotTitle = jpController.JackPotTitle;

            if (jackPotTitle != null)
            {
                rend.Add(jackPotTitle); // 추가된 부분
            }
            else
            {
                Debug.LogWarning("❗ JackPotTitle이 연결되지 않았습니다.");
            }

            // 5. rend 리스트가 비어있다면 임시 TextMesh 추가
            if (rend.Count == 0)
            {
                Debug.LogWarning("❗ rend 리스트가 비어 있습니다. 임시 TextMesh를 추가합니다.");
                GameObject tempTextObj = new GameObject("TempJackpotText");
                tempTextObj.transform.SetParent(transform);
                TextMesh tempText = tempTextObj.AddComponent<TextMesh>();
                tempText.text = "Jackpot!";
                tempText.fontSize = 100;
                rend.Add(tempText);
            }

            // 6. 색상 초기화
            colors = new List<Color>();
            foreach (var item in rend)
            {
                colors.Add(item.color);
            }

            // 7. 램프와 코인 효과 초기화
            lamps = jpController.Lamps;
            coinsFountains = jpController.CoinsFoutains;

            if (lamps != null)
            {
                foreach (var item in lamps)
                {
                    if (item != null) item.lampFlash = LampsFlash.Sequence;
                }
            }

            // 8. rend 리스트 확인 후 Flashing 실행
            if (rend.Count > 0)
            {
                fountainCoroutine = StartCoroutine(FountainC());
                Flashing(true);
            }
            else
            {
                Debug.LogError("❗ rend 리스트가 여전히 비어 있습니다. Flashing 효과가 실행되지 않습니다.");
            }
        }

        private void OnDestroy()
        {
            if (jpController != null)
            {
                foreach (var item in jpController.WinRenderers)
                {
                    if (item) item.enabled = false;
                }
            }

            if (mR != null) mR.enabled = true;
            if (jackPotAmountTemp != null) Destroy(jackPotAmountTemp.gameObject);

            // 코루틴 안전하게 중지
            if (fountainCoroutine != null)
            {
                StopCoroutine(FountainC());
                fountainCoroutine = null;
            }

            if (lamps != null)
            {
                foreach (var item in lamps)
                {
                    if (item) item.lampFlash = LampsFlash.NoneDisabled;
                }
            }
            Flashing(false);
        }
        #endregion regular


        /// <summary>
        /// 코인 분수 효과
        /// </summary>
        /// <returns></returns>
        private IEnumerator FountainC()
        {
            if (coinsFountains == null || coinsFountains.Length == 0)
            {
                Debug.LogWarning("❗ coinsFountains가 설정되지 않았습니다.");
                yield break;
            }

            while (true)
            {
                foreach (var item in coinsFountains)
                {
                    if (item != null)
                    {
                        item.Jump();
                        if (coinsClip != null)
                        {
                            MSound.PlayClip(0.2f, coinsClip);
                        }
                        yield return new WaitForSeconds(0.5f);
                    }
                }
                yield return new WaitForSeconds(2);
            }
            
        }

        /// <summary>
        /// 잭팟 타이틀 및 금액 반짝임
        /// </summary>
        /// <param name="flashing"></param>
        private void Flashing(bool flashing)
        {
            if (rend == null || rend.Count == 0)
            {
                Debug.LogWarning("❗ Flashing - rend 리스트가 비어있습니다.");
                return;
            }

            //Color c;
            if (flashing)
            {
                //Color nC;
                SimpleTween.Value(gameObject, 0, Mathf.PI * 2f, 1f).SetOnUpdate((float val) =>
                {
                    for (int i = 0; i < rend.Count; i++)
                    {
                        if (rend[i] != null)
                            continue;

                        float k = 0.5f * (Mathf.Cos(val) + 1f);
                        Color c = colors[i];
                        Color nC = new Color(c.r, c.g, c.b, c.a * k);
                        rend[i].color = nC;

                    }
                }).SetCycled();
            }
            else
            {
                SimpleTween.Cancel(gameObject, false);
                for (int i = 0; i < rend.Count; i++)
                {
                    //c = colors[i];
                    if (rend[i] != null)
                    {
                        rend[i].color = colors[i];
                    }
                }
            }
        }
    }
}
