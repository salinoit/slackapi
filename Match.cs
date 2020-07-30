using System;
using System.Threading.Tasks;

namespace consoleApp
{
    public static class MatchExtensionMethods
    {
        public static (bool, T) Match<T>(this T item, Func<T, bool> qualifier, Action<T> action)
        {
            return (false, item).Match(qualifier, action);
        }

        public static (bool, T) Match<T>(this (bool hasMatch, T item) src, Func<T, bool> qualifier, Action<T> action)
        {
            bool hasMatch = src.hasMatch;

            if (!hasMatch)
            {
                hasMatch = qualifier(src.item);

                if (hasMatch)
                {
                    action(src.item);
                }
            }

            return (hasMatch, src.item);
        }

        public static void Match<T>(this T val, params (Func<T, bool> qualifier, Action<T> action)[] matches)
        {
            foreach (var match in matches)
            {
                if (match.qualifier(val))
                {
                    match.action(val);
                    break;
                }
            }
        }

        public static U MatchReturn<T, U>(this T val, params (Func<T, bool> qualifier, Func<T, U> func)[] matches)
        {
            U ret = default(U);

            foreach (var match in matches)
            {
                if (match.qualifier(val))
                {
                    ret = match.func(val);
                    break;
                }
            }

            return ret;
        }

        public async static void MatchAsync<T>(this T val, params (Func<T, bool> qualifier, Action<T> action)[] matches)
        {
            foreach (var match in matches)
            {
                if (await Task.Run(() => match.qualifier(val)))
                {
                    await Task.Run(() => match.action(val));
                    break;
                }
            }
        }

        public static void MatchAll<T>(this T val, params (Func<T, bool> qualifier, Action<T> action)[] matches)
        {
            foreach (var match in matches)
            {
                if (match.qualifier(val))
                {
                    match.action(val);
                }
            }
        }

        public static void ForEach(this int n, Action<int> action)
        {
            for (int i = 0; i < n; i++)
            {
                action(i);
            }
        }
    }
}