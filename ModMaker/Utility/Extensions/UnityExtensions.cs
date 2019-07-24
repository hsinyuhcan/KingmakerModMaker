using System;
using System.Collections.Generic;
using System.Text;

namespace ModMaker.Utility
{
    public static class UnityExtensions
    {
        public static bool IsNullOrDestroyed<T>(this T value)
        {
            return value == null || ((value is UnityEngine.Object UnityObj) && UnityObj == null);
        }
    }
}
