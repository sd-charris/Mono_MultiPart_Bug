
using System;
using System.Globalization;
using System.Text;

public class Helpers
{
    public static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
}

public class StrUtils
{
    StrUtils() { }

    public static bool StartsWith(string str1, string str2)
    {
        return StartsWith(str1, str2, false);
    }

    public static bool StartsWith(string str1, string str2, bool ignore_case)
    {
        int l2 = str2.Length;
        if (l2 == 0)
            return true;

        int l1 = str1.Length;
        if (l2 > l1)
            return false;

        return (0 == String.Compare(str1, 0, str2, 0, l2, ignore_case, Helpers.InvariantCulture));
    }

    public static bool EndsWith(string str1, string str2)
    {
        return EndsWith(str1, str2, false);
    }

    public static bool EndsWith(string str1, string str2, bool ignore_case)
    {
        int l2 = str2.Length;
        if (l2 == 0)
            return true;

        int l1 = str1.Length;
        if (l2 > l1)
            return false;

        return (0 == String.Compare(str1, l1 - l2, str2, 0, l2, ignore_case, Helpers.InvariantCulture));
    }

    public static string EscapeQuotesAndBackslashes(string attributeValue)
    {
        StringBuilder sb = null;
        for (int i = 0; i < attributeValue.Length; i++)
        {
            char ch = attributeValue[i];
            if (ch == '\'' || ch == '"' || ch == '\\')
            {
                if (sb == null)
                {
                    sb = new StringBuilder();
                    sb.Append(attributeValue.Substring(0, i));
                }
                sb.Append('\\');
                sb.Append(ch);
            }
            else
            {
                if (sb != null)
                    sb.Append(ch);
            }
        }
        if (sb != null)
            return sb.ToString();
        return attributeValue;
    }

    public static bool IsNullOrEmpty(string value)
    {
        return String.IsNullOrEmpty(value);
    }

    public static string[] SplitRemoveEmptyEntries(string value, char[] separator)
    {
        return value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
    }
}