using System;
using System.Text;
using System.Web;

/// <summary>
/// General helpers that are not database-connection related (Hebrew encoding, theme, etc.).
/// </summary>
public static class Utilities
{
    public const string ThemeCookieName = "OptiSchedTheme";
    public const string ThemeDark = "dark";
    public const string ThemeLight = "light";

    public static string GetTheme()
    {
        try
        {
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                HttpCookie cookie = HttpContext.Current.Request.Cookies[ThemeCookieName];
                if (cookie != null && string.Equals(cookie.Value, ThemeLight, StringComparison.OrdinalIgnoreCase))
                    return ThemeLight;
            }
        }
        catch { }
        return ThemeDark;
    }

    public static void SetTheme(string theme)
    {
        if (HttpContext.Current == null || HttpContext.Current.Response == null)
            return;

        string value = string.Equals(theme, ThemeLight, StringComparison.OrdinalIgnoreCase) ? ThemeLight : ThemeDark;
        HttpCookie cookie = new HttpCookie(ThemeCookieName, value);
        cookie.Expires = DateTime.Now.AddYears(1);
        cookie.Path = "/";
        cookie.HttpOnly = false;
        HttpContext.Current.Response.Cookies.Set(cookie);
    }

    public static string ToggleTheme()
    {
        string next = GetTheme() == ThemeLight ? ThemeDark : ThemeLight;
        SetTheme(next);
        return next;
    }

    // Fixes Hebrew text encoding / classic UTF-8 <-> Windows-1255 mojibake
    public static string FixEncoding(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";

        string text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return "";

        text = SanitizeCorruptedSeparators(text);

        if (IsCleanText(text))
            return text;

        try
        {
            string best = text;
            int bestScore = ScoreText(text);

            Encoding hebrew1255 = Encoding.GetEncoding("Windows-1255");
            Encoding utf8 = Encoding.UTF8;
            Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");

            TryImprove(ref best, ref bestScore, () => utf8.GetString(hebrew1255.GetBytes(text)));
            TryImprove(ref best, ref bestScore, () => utf8.GetString(latin1.GetBytes(text)));

            if (!HasHebrewLetters(text))
            {
                TryImprove(ref best, ref bestScore, () => hebrew1255.GetString(utf8.GetBytes(text)));
            }

            TryImprove(ref best, ref bestScore, () => utf8.GetString(Encoding.Default.GetBytes(text)));
            TryImprove(ref best, ref bestScore, () => hebrew1255.GetString(Encoding.Default.GetBytes(text)));

            if (LooksLikeMojibake(text) || LooksLikeMojibake(best))
            {
                string mixed = FixMixedTokens(text);
                TryImprove(ref best, ref bestScore, () => mixed);
                string mixedBest = FixMixedTokens(best);
                TryImprove(ref best, ref bestScore, () => mixedBest);
            }

            best = SanitizeCorruptedSeparators(best);
            // Cleanup leftovers from partial historical repairs
            best = System.Text.RegularExpressions.Regex.Replace(
                best, @"\u05EA[^\u05D0-\u05EA]{0,3}\u05DB\u05D5\u05E8\u05EA", "\u05EA\u05D6\u05DB\u05D5\u05E8\u05EA");
            return best;
        }
        catch (Exception ex)
        {
            LoggingService.Log("Utilities", "Error in FixEncoding", ex);
            return text;
        }
    }

    private static void TryImprove(ref string best, ref int bestScore, Func<string> candidateFactory)
    {
        try
        {
            string candidate = candidateFactory();
            if (string.IsNullOrEmpty(candidate)) return;
            candidate = SanitizeCorruptedSeparators(candidate);
            int score = ScoreText(candidate);
            if (score > bestScore)
            {
                best = candidate;
                bestScore = score;
            }
        }
        catch { }
    }

    private static string FixMixedTokens(string text)
    {
        var parts = System.Text.RegularExpressions.Regex.Split(text, @"(\s+|[:|/]+)");
        for (int i = 0; i < parts.Length; i++)
        {
            string p = parts[i];
            if (string.IsNullOrEmpty(p) || !LooksLikeMojibake(p))
                continue;

            string fixedPart = p;
            int score = ScoreText(p);
            Encoding hebrew1255 = Encoding.GetEncoding("Windows-1255");
            Encoding utf8 = Encoding.UTF8;
            Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");
            try
            {
                string c1 = utf8.GetString(hebrew1255.GetBytes(p));
                if (ScoreText(c1) > score) { fixedPart = c1; score = ScoreText(c1); }
            }
            catch { }
            try
            {
                string c2 = utf8.GetString(latin1.GetBytes(p));
                if (ScoreText(c2) > score) { fixedPart = c2; score = ScoreText(c2); }
            }
            catch { }
            parts[i] = fixedPart;
        }
        return string.Concat(parts);
    }

    private static int ScoreText(string text)
    {
        if (string.IsNullOrEmpty(text)) return -1000;
        int score = 0;
        int hebrewLetters = 0;
        int mojibakeHits = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c >= 0x05D0 && c <= 0x05EA) hebrewLetters++;
            // geresh / gershayim
            if (c == '\u05F3' || c == '\u05F4') mojibakeHits++;
            // multiplication / en-dash / quotes
            if (c == '\u00D7' || c == '\u2013' || c == '\u203A' || c == '\u00AB' || c == '\u00BB') mojibakeHits++;
            if (c == '\uFFFD' || c == '\u00BF' || c == '\u00C2' || c == '\u00C3') mojibakeHits += 2;
            if (c >= 0x0080 && c <= 0x009F) mojibakeHits += 2;
        }
        score += hebrewLetters * 4;
        score -= mojibakeHits * 6;
        if (LooksLikeMojibake(text)) score -= 20;
        return score;
    }

    private static bool IsCleanText(string text)
    {
        if (LooksLikeMojibake(text)) return false;
        if (!HasHebrewLetters(text) && !HasHighAnsi(text)) return true;
        return HasHebrewLetters(text) && !LooksLikeMojibake(text);
    }

    private static bool HasHebrewLetters(string text)
    {
        foreach (char c in text)
        {
            if (c >= 0x05D0 && c <= 0x05EA) return true;
        }
        return false;
    }

    private static bool HasHighAnsi(string text)
    {
        foreach (char c in text)
        {
            if (c > 127) return true;
        }
        return false;
    }

    private static bool LooksLikeMojibake(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        // geresh + multiply  (classic UTF-8 Hebrew misread as 1255)
        if (text.IndexOf("\u05F3\u00D7", StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("\u00D7\u05F3", StringComparison.Ordinal) >= 0) return true;
        bool hasGeresh = text.IndexOf('\u05F3') >= 0 || text.IndexOf('\u05F4') >= 0;
        bool hasLatinJunk = text.IndexOf('\u00D7') >= 0 || text.IndexOf('\u203A') >= 0
            || text.IndexOf('\u00AB') >= 0 || text.IndexOf('\u00C2') >= 0 || text.IndexOf('\u00C3') >= 0;
        if (hasGeresh && hasLatinJunk) return true;
        if (text.IndexOf('\u05B2') >= 0) return true;
        return false;
    }

    private static string SanitizeCorruptedSeparators(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = text.Replace("\u05B2\u00B7", " - ");
        text = text.Replace("\u05B2", "");
        text = text.Replace(" \u00B7 ", " - ");
        text = text.Replace("\u00B7", " - ");
        return text;
    }
}
