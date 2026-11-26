using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamMachine.Util
{
    /// <summary>
    /// A small JSON reader and writer so the core stays free of extra packages.
    /// Unity 2022 and plain .NET both compile it. Numbers come back as doubles.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            int i = 0;
            object value = ParseValue(json, ref i);
            Skip(json, ref i);
            if (i != json.Length)
            {
                throw new FormatException("Unexpected trailing JSON at index " + i);
            }
            return value;
        }

        public static string Stringify(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        public static Dictionary<string, object> AsObject(object value)
        {
            return (Dictionary<string, object>)value;
        }

        public static List<object> AsArray(object value)
        {
            return value as List<object> ?? new List<object>();
        }

        public static string AsString(object value)
        {
            return value as string ?? string.Empty;
        }

        public static double AsDouble(object value)
        {
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int n) return n;
            if (value is long l) return l;
            if (value is string s) return double.Parse(s, CultureInfo.InvariantCulture);
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        }

        public static float AsFloat(object value)
        {
            return (float)AsDouble(value);
        }

        public static int AsInt(object value)
        {
            return (int)AsDouble(value);
        }

        public static bool AsBool(object value)
        {
            if (value is bool b) return b;
            return AsString(value) == "true";
        }

        static void Write(StringBuilder sb, object value)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }
            if (value is bool b)
            {
                sb.Append(b ? "true" : "false");
                return;
            }
            if (value is string s)
            {
                WriteString(sb, s);
                return;
            }
            if (value is int n)
            {
                sb.Append(n.ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (value is long l)
            {
                sb.Append(l.ToString(CultureInfo.InvariantCulture));
                return;
            }
            if (value is float f)
            {
                WriteNumber(sb, f);
                return;
            }
            if (value is double d)
            {
                WriteNumber(sb, d);
                return;
            }
            if (value is IDictionary<string, object> dict)
            {
                sb.Append('{');
                bool first = true;
                foreach (var pair in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, pair.Key);
                    sb.Append(':');
                    Write(sb, pair.Value);
                }
                sb.Append('}');
                return;
            }
            if (value is IList list)
            {
                sb.Append('[');
                for (int i = 0; i < list.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    Write(sb, list[i]);
                }
                sb.Append(']');
                return;
            }
            throw new InvalidOperationException("MiniJson cannot write " + value.GetType().Name);
        }

        static void WriteNumber(StringBuilder sb, double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                sb.Append("0");
                return;
            }
            sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append("\\u");
                            sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        static object ParseValue(string json, ref int i)
        {
            Skip(json, ref i);
            if (i >= json.Length) throw new FormatException("Unexpected end of JSON");
            char c = json[i];
            if (c == '{') return ParseObject(json, ref i);
            if (c == '[') return ParseArray(json, ref i);
            if (c == '"') return ParseString(json, ref i);
            if (c == 't') return ParseLiteral(json, ref i, "true", true);
            if (c == 'f') return ParseLiteral(json, ref i, "false", false);
            if (c == 'n') return ParseLiteral(json, ref i, "null", null);
            return ParseNumber(json, ref i);
        }

        static Dictionary<string, object> ParseObject(string json, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++;
            Skip(json, ref i);
            if (i < json.Length && json[i] == '}')
            {
                i++;
                return dict;
            }
            while (i < json.Length)
            {
                Skip(json, ref i);
                string key = ParseString(json, ref i);
                Skip(json, ref i);
                if (i >= json.Length || json[i] != ':') throw new FormatException("Expected ':'");
                i++;
                dict[key] = ParseValue(json, ref i);
                Skip(json, ref i);
                if (i >= json.Length) throw new FormatException("Unclosed object");
                if (json[i] == ',')
                {
                    i++;
                    continue;
                }
                if (json[i] == '}')
                {
                    i++;
                    break;
                }
                throw new FormatException("Expected ',' or '}'");
            }
            return dict;
        }

        static List<object> ParseArray(string json, ref int i)
        {
            var list = new List<object>();
            i++;
            Skip(json, ref i);
            if (i < json.Length && json[i] == ']')
            {
                i++;
                return list;
            }
            while (i < json.Length)
            {
                list.Add(ParseValue(json, ref i));
                Skip(json, ref i);
                if (i >= json.Length) throw new FormatException("Unclosed array");
                if (json[i] == ',')
                {
                    i++;
                    continue;
                }
                if (json[i] == ']')
                {
                    i++;
                    break;
                }
                throw new FormatException("Expected ',' or ']'");
            }
            return list;
        }

        static string ParseString(string json, ref int i)
        {
            if (json[i] != '"') throw new FormatException("Expected string");
            i++;
            var sb = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= json.Length) throw new FormatException("Bad escape");
                char e = json[i++];
                switch (e)
                {
                    case '"':
                    case '\\':
                    case '/': sb.Append(e); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > json.Length) throw new FormatException("Bad unicode escape");
                        int code = int.Parse(json.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default: throw new FormatException("Unknown escape");
                }
            }
            throw new FormatException("Unclosed string");
        }

        static object ParseLiteral(string json, ref int i, string literal, object value)
        {
            if (i + literal.Length > json.Length || json.Substring(i, literal.Length) != literal)
            {
                throw new FormatException("Expected " + literal);
            }
            i += literal.Length;
            return value;
        }

        static object ParseNumber(string json, ref int i)
        {
            int start = i;
            if (json[i] == '-') i++;
            while (i < json.Length && char.IsDigit(json[i])) i++;
            if (i < json.Length && json[i] == '.')
            {
                i++;
                while (i < json.Length && char.IsDigit(json[i])) i++;
            }
            if (i < json.Length && (json[i] == 'e' || json[i] == 'E'))
            {
                i++;
                if (i < json.Length && (json[i] == '+' || json[i] == '-')) i++;
                while (i < json.Length && char.IsDigit(json[i])) i++;
            }
            string token = json.Substring(start, i - start);
            return double.Parse(token, CultureInfo.InvariantCulture);
        }

        static void Skip(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }
    }
}
