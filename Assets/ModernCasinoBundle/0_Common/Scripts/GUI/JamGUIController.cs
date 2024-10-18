using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class JamGUIController : MonoBehaviour
	{
        [SerializeField]
        private Text jamAmountText;

        #region temp vars
        private TweenIntValue jamTween;
        private SlotPlayer MPlayer { get { return SlotPlayer.Instance; } }
        private GuiController MGui { get { return GuiController.Instance; } }
        private string jamsFormat = "0,0";
        #endregion temp vars

        #region regular
        private IEnumerator Start()
        {
            while (!MPlayer)
            {
                yield return new WaitForEndOfFrame();
            }
            MPlayer.ChangeJamsEvent += ChangeBalanceHandler;
            MPlayer.LoadJamsEvent += LoadBalanceHandler;
            if (jamAmountText) jamTween = new TweenIntValue(jamAmountText.gameObject, MPlayer.Jams, 1, 3, true, (b) => { if (this && jamAmountText) jamAmountText.text = (b > 0) ? b.ToString(jamsFormat) : "0"; });
            Refresh();
        }
        #endregion regular
        private void OnDestroy()
        {
            if (MPlayer)
            {
                // remove player event handlers
                MPlayer.ChangeJamsEvent -= ChangeBalanceHandler;
                MPlayer.LoadJamsEvent -= LoadBalanceHandler;
            }
        }
        /// <summary>
        /// Refresh gui balance
        /// </summary>
        private void Refresh()
        {
            if (jamAmountText && MPlayer) jamAmountText.text = (MPlayer.Jams > 0) ? MPlayer.Jams.ToString(jamsFormat) : "0";
        }
        public void Purchase1000()
        {
            MPlayer.Jams += 1000;
            Refresh();
        }
        public void Purchase2000()
        {
            MPlayer.Jams += 2000;
            Refresh();
        }
        public void Purchase3000()
        {
            MPlayer.Jams += 3000;
            Refresh();
        }
        #region eventhandlers
        private void ChangeBalanceHandler(int newBalance)
        {
            if (jamTween != null) jamTween.Tween(newBalance, 100);
            else
            {
                if (jamAmountText) jamAmountText.text = (newBalance > 0) ? newBalance.ToString(jamsFormat) : "0";
            }
        }

        private void LoadBalanceHandler(int newBalance)
        {
            if (jamAmountText) jamAmountText.text = (newBalance > 0) ? newBalance.ToString(jamsFormat) : "0";
        }
        #endregion eventhandlers
    }
}
