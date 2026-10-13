using UnityEngine;

// Attach this to any part (RAM, PSU, CPU, etc.) alongside its XR Grab Interactable.
// It just holds the label/description data that the hover panel will display.
//
// Language: fill the Filipino fields. When the language is switched in Settings,
// partName/description switch by themselves, so the hover panel script
// doesn't need any changes. Empty Filipino fields = shows English.
public class PartInfo : MonoBehaviour
{
    [Header("Info shown on the hover panel")]
    public string partName = "RAM";

    [TextArea(2, 4)]
    public string description = "Random Access Memory - stores temporary data for active processes.";

    [Header("Filipino (leave empty to show English)")]
    public string partNameFilipino;

    [TextArea(2, 4)]
    public string descriptionFilipino;

    private string englishName;
    private string englishDescription;

    private void Awake()
    {
        // Remember the English text typed in the Inspector
        englishName = partName;
        englishDescription = description;
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += ApplyLanguage;
        ApplyLanguage();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= ApplyLanguage;
    }

    private void ApplyLanguage()
    {
        if (englishName == null) { englishName = partName; englishDescription = description; }

        partName = LanguageManager.Pick(englishName, partNameFilipino);
        description = LanguageManager.Pick(englishDescription, descriptionFilipino);
    }
}