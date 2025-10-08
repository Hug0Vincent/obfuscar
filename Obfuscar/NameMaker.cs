#region Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>

/// <copyright>
/// Copyright (c) 2007 Ryan Williams <drcforbin@gmail.com>
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in
/// all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
/// THE SOFTWARE.
/// </copyright>

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Obfuscar
{
    static class NameMaker
    {
        static List<string> genericNames;
        static List<string> namespaceNames;
        static List<string> typeNames;

        static int numGenericNames;
        static int numNamespaceNames;
        static int numTypeNames;

        const string defaultChars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";

        const string unicodeChars = /* unicode block */ "\u00A0\u1680" +
            "\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200A\u200B\u2010\u2011\u2012\u2013\u2014\u2015" +
            "\u2022\u2024\u2025\u2027\u2028\u2029\u202A\u202B\u202C\u202D\u202E\u202F" +
            "\u2032\u2035\u2033\u2036\u203E" +
            "\u2047\u2048\u2049\u204A\u204B\u204C\u204D\u204E\u204F\u2050\u2051\u2052\u2053\u2054\u2055\u2056\u2057\u2058\u2059" +
            "\u205A\u205B\u205C\u205D\u205E\u205F\u2060" +
            "\u2061\u2062\u2063\u2064\u206A\u206B\u206C\u206D\u206E\u206F\u3000";

        private static readonly string koreanChars;

        static NameMaker()
        {
            var chars = new List<char>(128);
            var rnd = new Random();
            var startPoint = rnd.Next(0xAC00, 0xD5D0);
            for (int i = startPoint; i < startPoint + 128; i++)
                chars.Add((char)i);

            ShuffleArray(chars, rnd);
            koreanChars = new string(chars.ToArray());
        }

        private static void ShuffleArray<T>(IList<T> list, Random rnd)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        public static string UniqueName(int index, string sep = null)
        {
            return GenerateName(genericNames, numGenericNames, index, sep);
        }

        public static string UniqueTypeName(int index)
        {
            return GenerateName(typeNames ?? genericNames, numTypeNames > 0 ? numTypeNames : numGenericNames, index % (numTypeNames > 0 ? numTypeNames : numGenericNames), ".");
        }

        public static string UniqueNamespace(int index)
        {
            return GenerateName(namespaceNames ?? genericNames, numNamespaceNames > 0 ? numNamespaceNames : numGenericNames, index / (numNamespaceNames > 0 ? numNamespaceNames : numGenericNames), ".");
        }

        public static string UniqueNestedTypeName(int index) => UniqueName(index);

        private static string GenerateName(List<string> names, int count, int index, string sep)
        {
            if (index < count)
                return names[index];

            Stack<string> stack = new Stack<string>();

            do
            {
                stack.Push(names[index % count]);
                if (index < count)
                    break;
                index /= count;
            } while (true);

            var builder = new StringBuilder();
            builder.Append(stack.Pop());
            while (stack.Count > 0)
            {
                if (sep != null)
                    builder.Append(sep);
                builder.Append(stack.Pop());
            }

            return builder.ToString();
        }

        internal static void DetermineNames(Settings settings)
        {
            genericNames = LoadWordList(settings.WordListFilePath);
            typeNames = LoadWordList(settings.TypeWordListFilePath);
            namespaceNames = LoadWordList(settings.NamespaceWordListFilePath);

            numGenericNames = genericNames?.Count ?? 0;
            numTypeNames = typeNames?.Count ?? 0;
            numNamespaceNames = namespaceNames?.Count ?? 0;

            // If no wordlists found, fall back to default char logic
            if (numGenericNames == 0)
            {
                string chars;
                if (!string.IsNullOrWhiteSpace(settings.CustomChars))
                    chars = settings.CustomChars;
                else if (settings.UseUnicodeNames)
                    chars = unicodeChars;
                else if (settings.UseKoreanNames)
                    chars = koreanChars;
                else
                    chars = defaultChars;

                genericNames = new List<string>();
                foreach (char c in chars)
                    genericNames.Add(c.ToString());

                ValidateUnique(genericNames);
                numGenericNames = genericNames.Count;
            }

            // fallback if type/namespace names missing
            if (numTypeNames == 0)
            {
                typeNames = genericNames;
                numTypeNames = numGenericNames;
            }

            if (numNamespaceNames == 0)
            {
                namespaceNames = genericNames;
                numNamespaceNames = numGenericNames;
            }
        }

        private static List<string> LoadWordList(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var lines = File.ReadAllLines(path);
            var words = new List<string>();

            foreach (var line in lines)
            {
                var word = line.Trim();
                if (!string.IsNullOrEmpty(word))
                    words.Add(word);
            }

            if (words.Count == 0)
                return null;

            ValidateUnique(words);
            return words;
        }

        private static void ValidateUnique(List<string> list)
        {
            var set = new HashSet<string>(list);
            if (set.Count != list.Count)
                throw new InvalidOperationException("Duplicate entries found in name list.");
        }
    }
}
