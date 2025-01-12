using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class JackPotController : MonoBehaviour
	{
        [SerializeField]
        private TextMesh jackPotTitle;
        [SerializeField]
        private TextMesh jackPotAmount;
        [SerializeField]
        private LampsController[] lamps;
        [SerializeField]
        private CoinProcAnim[] coinsFountains;
        [SerializeField]
        private SpriteRenderer[] winRenderers;

        #region properties
        public TextMesh JackPotTitle { get { return jackPotTitle; } }
        public TextMesh JackPotAmount { get { return jackPotAmount; } }
        public LampsController[] Lamps { get { return lamps; } }
        public CoinProcAnim[] CoinsFoutains { get { return coinsFountains; } }
        public SpriteRenderer[] WinRenderers { get { return winRenderers; } }
        #endregion properties

        #region temp vars

        #endregion temp vars


        #region regular
        private void Awake()
        {
            // 잭팟 타이틀 자동 할당
            if (jackPotTitle == null)
            {
                jackPotTitle = GetComponentInChildren<TextMesh>();
                if (jackPotTitle == null)
                {
                    Debug.LogWarning("❗ JackPotTitle이 할당되지 않았습니다.");
                }
            }

            // 잭팟 금액 자동 할당
            if (jackPotAmount == null)
            {
                jackPotAmount = GetComponentInChildren<TextMesh>();
                if (jackPotAmount == null)
                {
                    Debug.LogWarning("❗ JackPotAmount가 할당되지 않았습니다.");
                }
            }

            // 램프 자동 할당
            if (lamps == null || lamps.Length == 0)
            {
                lamps = GetComponentsInChildren<LampsController>();
                if (lamps.Length == 0)
                    Debug.LogWarning("❗ Lamps가 할당되지 않았습니다.");
            }

            // 코인 효과 자동 할당
            if (coinsFountains == null || coinsFountains.Length == 0)
            {
                coinsFountains = GetComponentsInChildren<CoinProcAnim>();
                if (coinsFountains.Length == 0)
                    Debug.LogWarning("❗ CoinsFoutains가 할당되지 않았습니다.");
            }

            // 잭팟 이펙트 자동 할당
            if (winRenderers == null || winRenderers.Length == 0)
            {
                winRenderers = GetComponentsInChildren<SpriteRenderer>();
                if (winRenderers.Length == 0)
                    Debug.LogWarning("❗ WinRenderers가 할당되지 않았습니다.");
            }
        }
		
		private void Start()
		{	
			
		}
		#endregion regular

	}
}
