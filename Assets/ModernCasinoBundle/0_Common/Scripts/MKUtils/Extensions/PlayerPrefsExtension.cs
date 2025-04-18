using System.Globalization;
using UnityEngine;

/*
  02.07.2020 - first
*/

namespace Mkey
{
    public static class PlayerPrefsExtension
    {
        #region long
        public static void SetLong(string prefsName, long prefsValue)
        {
            if (string.IsNullOrEmpty(prefsName)) return;
            PlayerPrefs.SetString(prefsName, prefsValue.ToString(new CustomProvider(CultureInfo.InvariantCulture)));
        }

        public static long GetLong(string prefsName, long prefsDefaultValue)
        {
            string longString = PlayerPrefs.GetString(prefsName, null);
            if (string.IsNullOrEmpty(longString)) return prefsDefaultValue;

            bool parsed;
            long prefsValue = StringToLongInt(longString, out parsed);
            return (parsed) ? prefsValue : prefsDefaultValue; 
        }

        public static long StringToLongInt(string s, out bool parsed)
        {
            long longResult = 0;
            parsed = false;
            if (!long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out longResult))
            {
                parsed = false;
            }
            else
                parsed = true;
            return longResult;
        }
        #endregion long

        #region bool
        public static void SetBool(string prefsName, bool prefsValue)
        {
            if (string.IsNullOrEmpty(prefsName)) return;
            PlayerPrefs.SetInt(prefsName, prefsValue ? 1 : 0);
        }

        public static bool GetBool(string prefsName, bool prefsDefaultValue)
        {
            int def = prefsDefaultValue ? 1 : 0;
            return (PlayerPrefs.GetInt(prefsName, def) > 0);
        }
        #endregion bool

        #region serialize object
        public static void SetObject<T>(string prefsName, T prefsValue) where T:class
        {
            if (string.IsNullOrEmpty(prefsName) || prefsValue == null) return;
            string json = JsonUtility.ToJson(prefsValue);
            PlayerPrefs.SetString(prefsName, json);
        }

        public static T GetObject<T>(string prefsName, T prefsDefaultValue) where T : class
        {
            if (string.IsNullOrEmpty(prefsName)) return prefsDefaultValue;
            string json = PlayerPrefs.GetString(prefsName, null);
            if (string.IsNullOrEmpty(json)) return prefsDefaultValue;
            T prefsValue = JsonUtility.FromJson<T>(json);
            return (prefsValue==null) ? prefsDefaultValue : prefsValue;
        }
        #endregion serialize object
    }
}
