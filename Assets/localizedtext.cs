using UnityEngine;
using TMPro;

// Put on any TextMeshPro text (button labels, titles, checklist lines...).
// Type the English and Filipino versions; it switches automatically
// when the language is changed in Settings.
// Leave English empty to use whatever text is already there.
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [TextArea] public string english;
    [TextArea] public string filipino;

    private TMP_Text label;

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        if (string.IsNullOrEmpty(english)) english = label.text;
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        label.text = LanguageManager.Pick(english, filipino);
    }
}