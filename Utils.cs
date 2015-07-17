using System;

namespace NPossible
{
    namespace Common
    {
        public class Utils
        {
            public static DateTime FromUnixTime(long unixTime)
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return epoch.AddSeconds(unixTime);
            }
            public static Int64 ToUnixTime(DateTime pDateTime)
            {
                return (int)(pDateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            }
            public static string Base64Encode(string plainText)
            {
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                return System.Convert.ToBase64String(plainTextBytes);
            }
            public static string Base64Decode(string base64EncodedData)
            {
                byte[] base64EncodedBytes;
                try
                {

                    base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                }
                catch (Exception)
                {
                    return "";
                }
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
        }
    }
}
