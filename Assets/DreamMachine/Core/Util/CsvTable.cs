using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DreamMachine.Util
{
    public static class CsvTable
    {
        public static string Escape(string value)
        {
            if (value == null) return "";
            bool quote = value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            string cleaned = value.Replace("\"", "\"\"");
            return quote ? "\"" + cleaned + "\"" : cleaned;
        }

        public static string Format(float value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        public static void Write(string path, IList<string> header, IList<IList<string>> rows)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", header));
            for (int r = 0; r < rows.Count; r++)
            {
                sb.AppendLine(string.Join(",", rows[r]));
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
    }
}
