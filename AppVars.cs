using System;
using System.Threading;

namespace ImgSoh
{
    public static class AppVars
    {
        public static Progress<string> Progress { get; set; }
        public static ManualResetEvent SuspendEvent { get; set; }
        public static bool ShowXOR { get; set; }
        public static bool ImportRequested { get; set; }

        private static readonly Random _random = new Random();
        public static int RandomNext(int maxValue)
        {
            int result;
            if (Monitor.TryEnter(_random, AppConsts.LockTimeout)) {
                try {
                    result = _random.Next(maxValue);
                }
                finally { 
                    Monitor.Exit(_random); 
                }
            }
            else {
                throw new Exception();
            }

            return result;
        }

        public static DateTime DateTakenLast { get; set; }
    }
}