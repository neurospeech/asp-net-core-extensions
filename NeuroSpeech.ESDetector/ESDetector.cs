using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace NeuroSpeech
{
    public static class ESDetector
    {

        private static (bool? mobile, string browser, int major)[] minimum = new (bool? mobile, string browser, int major)[] { 
            (null, "chrome", 49),
            (null, "edge", 14),
            (null, "firefox", 45),
            (null, "safari", 10),
            (null, "samsungbrowser", 13),
            (null, "ucbrowser", 12),
            (true, "opera", 62),
            (false, "opera", 38)
        };

        public static bool SupportsES6(string userAgent)
        {
            var parser = UAParser.Parser.GetDefault();
            var ua = parser.ParseUserAgent(userAgent);
            foreach(var (mobile, browser, version) in minimum)
            {
                if(mobile != null)
                {
                    var isMobile = userAgent.ContainsIgnoreCase("mobile");
                    if (isMobile != mobile.Value)
                        continue;
                }
                if(userAgent.ContainsIgnoreCase(browser))
                {
                    if (ParseInt(ua.Major) >= version)
                        return true;
                    return false;
                }
            }
            return false;            
        }

        private static int ParseInt(this string text)
        {
            int i = 0;
            text = text.TrimStart();
            foreach(var ch in text)
            {
                if (!char.IsDigit(ch))
                    break;
                i = i * 10 + (ch - '0');
            }
            return i;
        }

        /// <summary>
        ///  Equals (Case Insensitive) 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="test"></param>
        /// <returns></returns>
        private static bool ContainsIgnoreCase(this string text, string test)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
            if (string.IsNullOrWhiteSpace(test))
                return false;
            return text.IndexOf(test, StringComparison.OrdinalIgnoreCase) != -1;
        }
    }
}
