using System;
using System.Text;
using System.Web;

public class Connect
{
    private static string _cachedConnectionString;

    // Gets the database connection string for Microsoft Access database
    // Automatically detects the best available provider (ACE 16.0 or 12.0)
    public static string GetConnectionString()
    {
        if (!string.IsNullOrEmpty(_cachedConnectionString))
            return _cachedConnectionString;

        try
        {
            LoggingService.Log("CONNECT", "GetConnectionString called - detecting provider");

            // Verify HttpContext is available (required for Server.MapPath)
            if (HttpContext.Current == null)
            {
                LoggingService.Log("CONNECT", "HttpContext is null", new InvalidOperationException("HttpContext is not available"));
                throw new InvalidOperationException("HttpContext is not available");
            }

            // Resolve database file path
            string path = HttpContext.Current.Server.MapPath("~/App_Data/calnder.db1.accdb.mdb");
            LoggingService.Log("CONNECT", string.Format("Database path resolved: {0}", path ?? "null"));

            if (string.IsNullOrEmpty(path))
            {
                LoggingService.Log("CONNECT", "Database path is empty", new InvalidOperationException("Database path is empty"));
                throw new InvalidOperationException("Database path is empty");
            }

            if (!System.IO.File.Exists(path))
            {
                LoggingService.Log("CONNECT", string.Format("Database file does not exist: {0}", path));
            }

            // Try providers in order of preference: 16.0 (Newer Office/Access), then 12.0 (Legacy)
            string[] providers = new[] { "Microsoft.ACE.OLEDB.16.0", "Microsoft.ACE.OLEDB.12.0" };
            
            foreach (var provider in providers)
            {
                string connectionString = @"Provider=" + provider + ";Data Source=" + path + ";Persist Security Info=False;";
                try
                {
                    // Test connection
                    using (var conn = new System.Data.OleDb.OleDbConnection(connectionString))
                    {
                        conn.Open();
                        // If we are here, it worked
                        LoggingService.Log("CONNECT", "Successfully connected using provider: " + provider);
                    }
                    
                    _cachedConnectionString = connectionString;
                    return _cachedConnectionString;
                }
                catch (Exception ex)
                {
                   LoggingService.Log("CONNECT", "Failed to connect with provider: " + provider + " - " + ex.Message);
                   // Continue to next provider
                }
            }

            // If we get here, no provider worked. Default to 12.0 and let the caller handle the error.
            LoggingService.Log("CONNECT", "All providers failed. Defaulting to 12.0");
            _cachedConnectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + path + ";Persist Security Info=False;";
            return _cachedConnectionString;
        }
        catch (Exception ex)
        {
            LoggingService.Log("CONNECT", "Exception in GetConnectionString", ex);
            throw;
        }
    }

    // Fixes Hebrew text encoding / classic UTF-8↔Windows-1255 mojibake
    public static string FixEncoding(object value)
    {
        if (value == null || value == DBNull.Value)
            return "";

        string text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return "";

        text = SanitizeCorruptedSeparators(text);

        // Already clean Hebrew/ASCII — leave alone (do NOT run strategies that can corrupt good text)
        if (IsCleanText(text))
            return text;

        try
        {
            string best = text;
            int bestScore = ScoreText(text);

            Encoding hebrew1255 = Encoding.GetEncoding("Windows-1255");
            Encoding utf8 = Encoding.UTF8;
            Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");

            // Strategy 1: UTF-8 bytes misread as Windows-1255 / Latin-1 (׳×׳–׳›׳•׳¨׳× → תזכורת)
            TryImprove(ref best, ref bestScore, () => utf8.GetString(hebrew1255.GetBytes(text)));
            TryImprove(ref best, ref bestScore, () => utf8.GetString(latin1.GetBytes(text)));

            // Strategy 2: Windows-1255 bytes misread as UTF-8 — only if source has no real Hebrew letters
            if (!HasHebrewLetters(text))
            {
                TryImprove(ref best, ref bestScore, () => hebrew1255.GetString(utf8.GetBytes(text)));
            }

            // Strategy 3: Default ANSI ↔ UTF-8 / 1255
            TryImprove(ref best, ref bestScore, () => utf8.GetString(Encoding.Default.GetBytes(text)));
            TryImprove(ref best, ref bestScore, () => hebrew1255.GetString(Encoding.Default.GetBytes(text)));

            // Mixed strings: repair token-by-token when whole-string conversion isn't enough
            if (LooksLikeMojibake(text) || LooksLikeMojibake(best))
            {
                string mixed = FixMixedTokens(text);
                TryImprove(ref best, ref bestScore, () => mixed);
                string mixedBest = FixMixedTokens(best);
                TryImprove(ref best, ref bestScore, () => mixedBest);
            }

            best = SanitizeCorruptedSeparators(best);
            // Cleanup leftovers from partial historical repairs (ת + junk + כורת)
            best = System.Text.RegularExpressions.Regex.Replace(
                best, @"ת[^\u05D0-\u05EA]{0,3}כורת", "תזכורת");
            return best;
        }
        catch (Exception ex)
        {
            LoggingService.Log("CONNECT", "Error in FixEncoding", ex);
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
        // Split on spaces/punctuation while keeping delimiters
        // Do NOT split on en-dash/× — they appear inside classic Hebrew mojibake sequences
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

    // Higher = better. Rewards Hebrew letters, penalizes mojibake markers / replacement chars.
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
            // Classic UTF-8→1255 mojibake uses geresh (׳) + Latin symbols
            if (c == '׳' || c == '״') mojibakeHits++;
            if (c == '×' || c == '–' || c == '›' || c == '«' || c == '»') mojibakeHits++;
            if (c == '\uFFFD' || c == '¿' || c == 'Â' || c == 'Ã') mojibakeHits += 2;
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
        // ASCII / digits / punctuation only
        if (!HasHebrewLetters(text) && !HasHighAnsi(text)) return true;
        // Real Hebrew letters and no mojibake markers
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

    // Detects classic Hebrew UTF-8 misdecoded as Windows-1255/Latin-1 (e.g. ׳×׳–׳›׳•׳¨׳×)
    private static bool LooksLikeMojibake(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (text.IndexOf("׳×", StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("×׳", StringComparison.Ordinal) >= 0) return true;
        // geresh/gershayim mixed with Latin-1 symbols typical of mojibake
        bool hasGeresh = text.IndexOf('׳') >= 0 || text.IndexOf('״') >= 0;
        bool hasLatinJunk = text.IndexOf('×') >= 0 || text.IndexOf('›') >= 0
            || text.IndexOf('«') >= 0 || text.IndexOf('Â') >= 0 || text.IndexOf('Ã') >= 0;
        if (hasGeresh && hasLatinJunk) return true;
        // Corrupted middle-dot separator seen in notifications
        if (text.IndexOf('\u05B2') >= 0) return true;
        return false;
    }

    private static string SanitizeCorruptedSeparators(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Access/ANSI sometimes mangles middle-dot into Hebrew point + dot
        text = text.Replace("\u05B2·", " - ");
        text = text.Replace("\u05B2", "");
        text = text.Replace(" · ", " - ");
        text = text.Replace("·", " - ");
        return text;
    }
}

