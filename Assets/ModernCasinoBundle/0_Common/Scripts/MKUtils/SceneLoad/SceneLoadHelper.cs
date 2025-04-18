using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    26.01.2021
    04.04.2021 - add autoload
 */
namespace Mkey
{
    public class SceneLoadHelper : MonoBehaviour
    {
        private SceneLoader SL => SceneLoader.Instance;

        public bool autoLoad = false;
        [ShowIfTrue("autoLoad")]
        public float autoLoadDelay = 0f;
        [ShowIfTrue("autoLoad")]
        public int autoLoadSceneIndex = 0;

        private IEnumerator Start()
        {
            if (autoLoad) 
            {
                yield return new WaitForSeconds(autoLoadDelay);
                LoadSceneByIndex(autoLoadSceneIndex);
            }
        }

        /// <summary>
        /// Load scene by build index
        /// </summary>
        /// <param name="scene"></param>
        public void LoadSceneByIndex(int scene)
        {
            if (SL) SL.LoadScene(scene);
        }
    }
}