using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Extend
{
    public static class ExtendObject
    {
        internal static string ToJson(this object source) => JsonUtility.ToJson(source);
        internal static TValue FromJson<TValue>(this string jsonData) => JsonUtility.FromJson<TValue>(jsonData);
    }
}

