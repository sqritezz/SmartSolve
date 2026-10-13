using System;
using UnityEngine;
using TMPro;

// ONE in the scene (e.g. on SettingsManager).
// Remembers the chosen language (even after closing the game) and tells
// every LocalizedText to update.
//
// Settings button: On Click () -> LanguageManager.NextLanguage
// (or two buttons: SetEnglish / SetFilipino)
public class LanguageManager : MonoBehaviour
{
    public enum Language { English = 0, Filipino = 1 }

    public static Language Current { get; private set; } = Language.English;
    public static event Action OnLanguageChanged;

    [Tooltip("Optional: text on the settings button that shows the current language")]
    public TMP_Text currentLanguageLabel;
    public string englishLabel = "Language: English";
    public string filipinoLabel = "Wika: Filipino";

    private const string SaveKey = "Language";

    private void Awake()
    {
        Current = (Language)PlayerPrefs.GetInt(SaveKey, 0);
    }

    private void Start()
    {
        Apply();
    }

    public void SetEnglish() => SetLanguage(Language.English);
    public void SetFilipino() => SetLanguage(Language.Filipino);

    // One button that switches back and forth
    public void NextLanguage()
    {
        SetLanguage(Current == Language.English ? Language.Filipino : Language.English);
    }

    public void SetLanguage(Language lang)
    {
        Current = lang;
        PlayerPrefs.SetInt(SaveKey, (int)lang);
        PlayerPrefs.Save();
        Apply();
    }

    private void Apply()
    {
        if (currentLanguageLabel != null)
            currentLanguageLabel.text = Current == Language.English ? englishLabel : filipinoLabel;
        OnLanguageChanged?.Invoke();
    }

    // For other scripts (dialogue, explanations): pick the right string
    public static string Pick(string english, string filipino)
    {
        if (Current == Language.Filipino && !string.IsNullOrEmpty(filipino)) return filipino;
        return english;
    }
}