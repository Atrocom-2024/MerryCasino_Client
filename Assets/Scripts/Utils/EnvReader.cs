using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class EnvReader
{
    public static void Load(string filePath)
    {
        //if (!File.Exists(filePath))
        //    throw new FileNotFoundException($"The file '{filePath}' does not exist.");

        //foreach (var line in File.ReadAllLines(filePath))
        //{
        //    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
        //        continue; // Skip empty lines and comments

        //    var parts = line.Split('=', 2);
        //    if (parts.Length != 2)
        //        continue; // Skiip lines that are not key-value pairs

        //    var key = parts[0].Trim();
        //    var value = parts[1].Trim();
        //    Environment.SetEnvironmentVariable(key, value);
        //    Debug.Log($"Environment variable set: {key} = {value}");
        //}

#if UNITY_EDITOR
        Environment.SetEnvironmentVariable("API_DOMAIN", "https://atrocom.com");
        //Environment.SetEnvironmentVariable("API_DOMAIN", "http://localhost:3000");
#else
        Environment.SetEnvironmentVariable("API_DOMAIN", "https://atrocom.com");
#endif
    }
}
