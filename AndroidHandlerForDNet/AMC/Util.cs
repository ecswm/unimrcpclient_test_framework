using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMC
{
    public static class Util
    {
        public static long GetMilliseconds(long tick){
            return (tick * 100) / 1000 / 1000;
        }
        public static long GetTicket(long ms)
        {
            return ms * 1000 * 1000 / 100;
        }
        public static long NowMs
        {
            get{
                return GetMilliseconds(DateTime.Now.Ticks);
            }
        }
        public static long DelayMs(long delay)
        {
            return GetMilliseconds(DateTime.Now.Ticks) + delay;
        }
    }
}
