// WARNING: Do not modify! Generated file.

namespace UnityEngine.Purchasing.Security {
    public class GooglePlayTangle
    {
        private static byte[] data = System.Convert.FromBase64String("2cwJxlK2zRGrwUjIzNEAY8iW9iyH6SY4fw3wHTD9/WXuxCMBQRjvV46ttnc63PqCSXINurzMT4yFOD0OX8JrR/jKKL0b0Hvmnc8eQT3sS/YsuNvYTxjrVeTW6EdPqrEEImaL406pvaL/NublDhAZvckxuw0E5lihgqcEJ0nX/hI7DILaa4z/xEXgZObkZ2lmVuRnbGTkZ2dmqizkKE+CL1bkZ0RWa2BvTOAu4JFrZ2dnY2ZlkkVt7jnJIQTuZU8+kV4xUZ1GUIUCEDNRCdio79YptkG2xS4lZD1F/F4KH9fm3zhVeqjBtIZd3/hva4x5EqYyMmGjRBOr4EJqBF2dbSENHGNSnj+iHlKbpqblgcOkHVwG84SQzxdnfb6HOSfwvWRlZ2Zn");
        private static int[] order = new int[] { 8,11,5,7,4,13,9,11,8,10,13,12,13,13,14 };
        private static int key = 102;

        public static readonly bool IsPopulated = true;

        public static byte[] Data() {
        	if (IsPopulated == false)
        		return null;
            return Obfuscator.DeObfuscate(data, order, key);
        }
    }
}
