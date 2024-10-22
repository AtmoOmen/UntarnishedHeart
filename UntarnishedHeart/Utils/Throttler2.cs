﻿using System;

namespace UntarnishedHeart.Utils
{
    internal static class Throttler2
    {
        static long NextCommandAt = 0;
        internal static bool Throttle(int ms)
        {
            if(Environment.TickCount64 > NextCommandAt)
            {
                if (ms > 0)
                {
                    NextCommandAt = Environment.TickCount64 + ms;
                }
                return true;
            }
            return false;
        }

        internal static void Rethrottle(int ms)
        {
            if (NextCommandAt - Environment.TickCount64 < ms)
            {
                NextCommandAt = Environment.TickCount64 + ms;
            }
        }
    }
}
