using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CMS.Util.Extend
{
    public static class ExtendTransform
    {
        internal static Transform FindChildByRecursion(this Transform rootParent, string targetName)
        {
            if (rootParent == null)
                return null;

            Transform result = rootParent.Find(targetName);
            if (result != null)
                return result;

            foreach (Transform child in rootParent.transform)
            {
                result = child.FindChildByRecursion(targetName);
                if (result != null)
                    return result;
            }

            return null;
        }

        private static void Recursive<T>(Transform nthTransform, ref List<T> stageOrganizers)
        {
            for (int i = 0; i < nthTransform.childCount; i++)
            {
                T dummy = nthTransform.GetChild(i).GetComponent<T>();
                if (dummy != null)
                {
                    stageOrganizers.Add(dummy);
                }
                else if (nthTransform.GetChild(i).childCount > 0)
                {
                    Recursive(nthTransform.GetChild(i), ref stageOrganizers);
                }
            }
        }

        internal static List<T> GetComponentsInChildrenForEveryActive<T>(this Transform transform)
        {
            List<T> returningList = new List<T>();

            Recursive(transform, ref returningList);

            return returningList;
        }
    }

}