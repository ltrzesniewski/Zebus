﻿using System;

namespace Abc.Zebus.Testing.Extensions
{
    public static class ExtendSystemDateTime
    {
        public static DateTime RoundToMillisecond(this DateTime input)
        {
            return input.AddTicks(-(input.Ticks % TimeSpan.FromMilliseconds(1).Ticks));
        }
    }
}