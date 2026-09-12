using System.Collections.Generic;
using System.Linq;

namespace CMS.Util.Extend
{
    public static class ExtendList
    {
        private static System.Random random = new System.Random();

        internal static void Shuffle<T>(this IList<T> list)
        {
            //int n = list.Count;
            //while (n > 1)
            //{
            //    n--;
            //    int k = random.Next(n + 1);
            //    T value = list[k];
            //    list[k] = list[n];
            //    list[n] = value;
            //}
            int rnd;
            T value;

            for (int i = list.Count - 1; i > 0; i--)
            {
                rnd = random.Next(i);

                value = list[rnd];
                
                list[rnd] = list[i];
                list[i] = value;
            }
        }

        internal static Stack<T> ToStack<T>(this List<T> list)
        {
            Stack<T> stack = new Stack<T>();
            foreach (T t in list)
                stack.Push(t);

            return stack;
        }

        internal static Stack<T> Shuffle<T>(this Stack<T> stack)
        {
            List<T> list = stack.ToList();
            list.Shuffle();
            return list.ToStack();
        }
    }

}

