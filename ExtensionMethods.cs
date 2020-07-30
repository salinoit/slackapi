using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace consoleApp
{
    public static class ExtensionMethods
    {
        public static string Bash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }

        public static string LeftOf(this String src, string s)
        {
            string ret = src;
            int idx = src.IndexOf(s);

            if (idx != -1)
            {
                ret = src.Substring(0, idx);
            }

            return ret;
        }

        public static string RightOf(this String src, string s)
        {
            string ret = String.Empty;
            int idx = src.IndexOf(s);

            if (idx != -1)
            {
                ret = src.Substring(idx + s.Length);
            }

            return ret;
        }

        public static string Between(this string src, string start, string end)
        {
            string ret = String.Empty;
            int idxStart = src.IndexOf(start);

            if (idxStart != -1)
            {
                idxStart += start.Length;
                int idxEnd = src.IndexOf(end, idxStart);

                if (idxEnd != -1)
                {
                    ret = src.Substring(idxStart, idxEnd - idxStart);
                }
            }

            return ret;
        }

        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
            {
                action(item);
            }
        }

        public static void ForEachWithIndex<T>(this IEnumerable<T> collection, Action<T, int> action)
        {
            int n = 0;

            foreach (var item in collection)
            {
                action(item, n++);
            }
        }
    }
}
