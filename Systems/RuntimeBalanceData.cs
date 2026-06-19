using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Terraria;

namespace CalamityLegendsComeBack.Systems
{
    internal static class RuntimeBalanceData
    {
        private const string ModFolderName = "CalamityLegendsComeBack";
        private static readonly Dictionary<string, SourceFileCache> SourceCaches = new();

        public static int[] GetSourceIntArray(string sourceRelativePath, string fieldName, int[] defaults)
        {
            string initializer = GetSourceInitializer(sourceRelativePath, fieldName);
            if (initializer == null)
                return defaults;

            int[] values = ParseIntValues(initializer);
            return values.Length > 0 ? values : defaults;
        }

        public static float[] GetSourceFloatArray(string sourceRelativePath, string fieldName, float[] defaults)
        {
            string initializer = GetSourceInitializer(sourceRelativePath, fieldName);
            if (initializer == null)
                return defaults;

            float[] values = ParseFloatValues(initializer);
            return values.Length > 0 ? values : defaults;
        }

        public static int[] GetSourceIntTableRow(string sourceRelativePath, string fieldName, int rowIndex, int[] defaults)
        {
            string row = GetSourceTableRow(sourceRelativePath, fieldName, rowIndex);
            if (row == null)
                return defaults;

            int[] values = ParseIntValues(row);
            return values.Length > 0 ? values : defaults;
        }

        public static int GetSourceTableInt(string sourceRelativePath, string fieldName, int rowIndex, int columnIndex, int fallback)
        {
            string row = GetSourceTableRow(sourceRelativePath, fieldName, rowIndex);
            if (row == null)
                return fallback;

            List<string> cells = SplitTopLevelCells(row);
            if (columnIndex < 0 || columnIndex >= cells.Count)
                return fallback;

            return TryParseIntCell(cells[columnIndex], out int value) ? value : fallback;
        }

        public static float GetSourceTableFloat(string sourceRelativePath, string fieldName, int rowIndex, int columnIndex, float fallback)
        {
            string row = GetSourceTableRow(sourceRelativePath, fieldName, rowIndex);
            if (row == null)
                return fallback;

            List<string> cells = SplitTopLevelCells(row);
            if (columnIndex < 0 || columnIndex >= cells.Count)
                return fallback;

            return TryParseFloatCell(cells[columnIndex], out float value) ? value : fallback;
        }

        public static float GetSourceFloatTableColumn(string sourceRelativePath, string fieldName, int rowIndex, int columnIndex, float fallback)
        {
            return GetSourceTableFloat(sourceRelativePath, fieldName, rowIndex, columnIndex, fallback);
        }

        public static float GetSourceFloatReturn(string sourceRelativePath, string memberName, float fallback)
        {
            string path = GetSourcePath(sourceRelativePath);
            if (path == null)
                return fallback;

            SourceFileCache source = GetSource(path);
            if (source == null)
                return fallback;

            Match match = Regex.Match(
                source.Text,
                $@"\b{Regex.Escape(memberName)}\b\s*\([^)]*\)\s*=>\s*(?<value>[-+]?(?:\d+\.\d+|\d+|\.\d+)f?)",
                RegexOptions.CultureInvariant);

            return match.Success && TryParseFloatCell(match.Groups["value"].Value, out float value) ? value : fallback;
        }

        public static int GetSourceSwitchInt(string sourceRelativePath, string caseName, int fallback)
        {
            string path = GetSourcePath(sourceRelativePath);
            if (path == null)
                return fallback;

            SourceFileCache source = GetSource(path);
            if (source == null)
                return fallback;

            Match match = Regex.Match(
                source.Text,
                $@"\b{Regex.Escape(caseName)}\b\s*=>\s*(?<value>[-+]?\d+)",
                RegexOptions.CultureInvariant);

            return match.Success &&
                int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : fallback;
        }

        private static string GetSourceTableRow(string sourceRelativePath, string fieldName, int rowIndex)
        {
            string initializer = GetSourceInitializer(sourceRelativePath, fieldName);
            if (initializer == null)
                return null;

            List<string> rows = ExtractTopLevelRows(initializer);
            return rowIndex >= 0 && rowIndex < rows.Count ? rows[rowIndex] : null;
        }

        private static string GetSourceInitializer(string sourceRelativePath, string fieldName)
        {
            string path = GetSourcePath(sourceRelativePath);
            if (path == null)
                return null;

            SourceFileCache source = GetSource(path);
            if (source == null)
                return null;

            return ExtractFieldInitializer(source.Text, fieldName);
        }

        private static SourceFileCache GetSource(string path)
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(path);
            if (SourceCaches.TryGetValue(path, out SourceFileCache cache) && cache.LastWriteUtc == lastWrite)
                return cache;

            try
            {
                string text = File.ReadAllText(path);
                SourceFileCache source = new(path, lastWrite, text);
                SourceCaches[path] = source;
                return source;
            }
            catch
            {
                return null;
            }
        }

        private static string GetSourcePath(string relativePath)
        {
            foreach (string path in GetSourceCandidatePaths(relativePath))
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private static IEnumerable<string> GetSourceCandidatePaths(string relativePath)
        {
            string savePath = Main.SavePath;
            if (!string.IsNullOrWhiteSpace(savePath))
                yield return Path.Combine(savePath, "ModSources", ModFolderName, relativePath);

            string current = Directory.GetCurrentDirectory();
            if (!string.IsNullOrWhiteSpace(current))
            {
                yield return Path.Combine(current, relativePath);
                yield return Path.Combine(current, "ModSources", ModFolderName, relativePath);
            }
        }

        private static string ExtractFieldInitializer(string text, string fieldName)
        {
            Match match = Regex.Match(text, $@"\b{Regex.Escape(fieldName)}\b\s*=", RegexOptions.CultureInvariant);
            if (!match.Success)
                return null;

            int braceStart = text.IndexOf('{', match.Index + match.Length);
            if (braceStart < 0)
                return null;

            int depth = 0;
            bool inString = false;
            for (int i = braceStart; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '"' && (i == 0 || text[i - 1] != '\\'))
                    inString = !inString;

                if (inString)
                    continue;

                if (c == '{')
                    depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                        return text.Substring(braceStart + 1, i - braceStart - 1);
                }
            }

            return null;
        }

        private static List<string> ExtractTopLevelRows(string initializer)
        {
            List<string> rows = new();
            int depth = 0;
            int rowStart = -1;
            bool inString = false;

            for (int i = 0; i < initializer.Length; i++)
            {
                char c = initializer[i];
                if (c == '"' && (i == 0 || initializer[i - 1] != '\\'))
                    inString = !inString;

                if (inString)
                    continue;

                if (c == '{')
                {
                    if (depth == 0)
                        rowStart = i + 1;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && rowStart >= 0)
                    {
                        rows.Add(initializer.Substring(rowStart, i - rowStart));
                        rowStart = -1;
                    }
                }
            }

            return rows;
        }

        private static List<string> SplitTopLevelCells(string row)
        {
            List<string> cells = new();
            int start = 0;
            bool inString = false;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                if (c == '"' && (i == 0 || row[i - 1] != '\\'))
                    inString = !inString;

                if (!inString && c == ',')
                {
                    cells.Add(row.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            if (start < row.Length)
                cells.Add(row.Substring(start).Trim());

            return cells;
        }

        private static int[] ParseIntValues(string text)
        {
            List<int> values = new();
            foreach (Match match in Regex.Matches(StripComments(text), @"[-+]?\d+"))
            {
                if (int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    values.Add(value);
            }

            return values.ToArray();
        }

        private static float[] ParseFloatValues(string text)
        {
            List<float> values = new();
            foreach (Match match in Regex.Matches(StripComments(text), @"[-+]?(?:\d+\.\d+|\d+|\.\d+)f?"))
            {
                string token = match.Value.TrimEnd('f', 'F');
                if (float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    values.Add(value);
            }

            return values.ToArray();
        }

        private static bool TryParseIntCell(string cell, out int value)
        {
            Match match = Regex.Match(StripComments(cell), @"[-+]?\d+");
            if (match.Success)
                return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

            value = 0;
            return false;
        }

        private static bool TryParseFloatCell(string cell, out float value)
        {
            Match match = Regex.Match(StripComments(cell), @"[-+]?(?:\d+\.\d+|\d+|\.\d+)f?");
            if (match.Success)
                return float.TryParse(match.Value.TrimEnd('f', 'F'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);

            value = 0f;
            return false;
        }

        private static string StripComments(string text)
        {
            text = Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
            return Regex.Replace(text, @"//.*", string.Empty);
        }

        private sealed class SourceFileCache
        {
            public SourceFileCache(string path, DateTime lastWriteUtc, string text)
            {
                Path = path;
                LastWriteUtc = lastWriteUtc;
                Text = text;
            }

            public string Path { get; }
            public DateTime LastWriteUtc { get; }
            public string Text { get; }
        }
    }
}
