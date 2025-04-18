using System;

public class EnvReader
{
    public static void Load(string filePath)
    {
#if UNITY_EDITOR
        Environment.SetEnvironmentVariable("API_DOMAIN", "https://atrocom.com");
        //Environment.SetEnvironmentVariable("API_DOMAIN", "http://taewoomac.iptime.org:3000");
        //Environment.SetEnvironmentVariable("API_DOMAIN", "http://localhost:3000");
#else
        Environment.SetEnvironmentVariable("API_DOMAIN", "https://atrocom.com");
#endif
    }
}
