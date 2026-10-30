using UnityEngine;
using System.Text.RegularExpressions;

// Saves each level's BEST time and BEST quiz score on the device (kept after closing the game).
// Nothing to put in the scene - other scripts use it by the level's name,
// e.g. "RAM Easy", "RAM Medium", "PSU Hard", "GPU Easy".
// The name must be written EXACTLY the same everywhere.
public static class LevelResults
{
    public static event System.Action OnChanged;

    // Results are only saved in the real game (the built APK on the Quest),
    // NOT while testing in the Unity Editor.
    public static bool CanSave => !Application.isEditor;

    // Every level name the menu uses (for resetting)
    public static readonly string[] AllLevels =
    {
        "RAM Easy", "RAM Medium", "RAM Hard",
        "PSU Easy", "PSU Medium", "PSU Hard",
        "GPU Easy", "GPU Medium", "GPU Hard",
        "Final"
    };

    private static string TimeKey(string level) => "SS_Time_" + level;
    private static string QuizKey(string level) => "SS_Quiz_" + level;
    private static string QuizScoreKey(string level) => "SS_QuizScore_" + level;

    // ---- Time ----
    public static void SaveTime(string level, float seconds)
    {
        if (!CanSave || string.IsNullOrEmpty(level) || seconds <= 0f) return;
        float best = PlayerPrefs.GetFloat(TimeKey(level), -1f);
        if (best < 0f || seconds < best)          // keep the fastest
        {
            PlayerPrefs.SetFloat(TimeKey(level), seconds);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool TryGetTime(string level, out float seconds)
    {
        seconds = PlayerPrefs.GetFloat(TimeKey(level), -1f);
        return seconds > 0f;
    }

    public static string FormatTime(float seconds)
    {
        int m = (int)(seconds / 60f);
        int s = (int)(seconds % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }

    // ---- Quiz ----
    // Pass the quiz's result text (e.g. "Score: 4/5"); "4/5" is taken from it.
    public static void SaveQuizFromText(string level, string resultText)
    {
        if (string.IsNullOrEmpty(level) || string.IsNullOrEmpty(resultText)) return;
        string clean = Regex.Replace(resultText, "<.*?>", "");   // remove color tags
        Match m = Regex.Match(clean, @"(\d+)\s*(/|out of|sa)\s*(\d+)", RegexOptions.IgnoreCase);
        if (m.Success)
            SaveQuiz(level, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[3].Value));
        else
            Debug.LogWarning("[LevelResults] Couldn't find a score like '4/5' in: " + clean);
    }

    public static void SaveQuiz(string level, int correct, int total)
    {
        if (!CanSave || string.IsNullOrEmpty(level) || total <= 0) return;
        float score = (float)correct / total;
        float best = PlayerPrefs.GetFloat(QuizScoreKey(level), -1f);
        if (score >= best)                         // keep the best score
        {
            PlayerPrefs.SetFloat(QuizScoreKey(level), score);
            PlayerPrefs.SetString(QuizKey(level), correct + "/" + total);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }

    public static bool TryGetQuiz(string level, out string result)
    {
        result = PlayerPrefs.GetString(QuizKey(level), "");
        return !string.IsNullOrEmpty(result);
    }

    // Wipe every saved time and quiz score
    // (used by Tools > SmartSolve > Reset Saved Times And Quiz Scores)
    public static void ResetAll()
    {
        ClearAll(AllLevels);
        Debug.Log("[LevelResults] All saved times and quiz scores were cleared.");
    }

    public static void ClearAll() { ResetAll(); }

    // Wipe only the given levels
    public static void ClearAll(string[] levels)
    {
        foreach (var l in levels)
        {
            PlayerPrefs.DeleteKey(TimeKey(l));
            PlayerPrefs.DeleteKey(QuizKey(l));
            PlayerPrefs.DeleteKey(QuizScoreKey(l));
        }
        PlayerPrefs.Save();
        OnChanged?.Invoke();
    }
}