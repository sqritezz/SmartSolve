using System.Collections;
using UnityEngine;
using TMPro;

// ONE in the scene (e.g. on an empty "Objective Warning" object).
// Shows a small warning panel in front of the player when they use something
// that isn't part of the current objective.
// Filipino: shown automatically when the game language is Filipino.
public class ObjectiveWarning : MonoBehaviour
{
    public static ObjectiveWarning Instance { get; private set; }

    [Header("UI")]
    [Tooltip("World-space canvas/panel for the warning (starts hidden)")]
    public GameObject panel;
    public TMP_Text messageText;
    [TextArea(1, 3)]
    public string defaultMessage = "Not yet! Follow the current objective on your checklist.";
    [TextArea(1, 3)]
    public string defaultMessageFilipino = "Hindi pa! Sundin muna ang kasalukuyang objective sa iyong checklist.";

    [Header("Title (optional)")]
    [Tooltip("The 'Warning' heading text")]
    public TMP_Text titleText;
    public string title = "Warning";
    public string titleFilipino = "Babala";

    [System.Serializable]
    public class Translation
    {
        [TextArea(1, 3)] public string english;
        [TextArea(1, 3)] public string filipino;
    }

    [Header("Filipino for messages sent by other scripts")]
    [Tooltip("If another script shows an English message listed here, the Filipino version is shown instead")]
    public Translation[] translations = new Translation[]
    {
        new Translation { english = "Talk to the NPC first before touching the PSU switch.",
                          filipino = "Kausapin muna ang NPC bago galawin ang PSU switch." },
        new Translation { english = "Turn off the PSU switch first!",
                          filipino = "Patayin muna ang PSU switch!" },
    };

    [Header("Placement")]
    public float distanceFromPlayer = 0.8f;
    public float heightOffset = -0.15f;
    public float showTime = 2.5f;

    [Header("Audio")]
    public AudioSource warningSound;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    // English + Filipino given directly
    public void Show(string english, string filipino)
    {
        Show(LanguageManager.Pick(english, filipino), true);
    }

    public void Show(string message)
    {
        Show(message, false);
    }

    private void Show(string message, bool alreadyTranslated)
    {
        if (panel == null) return;

        if (string.IsNullOrEmpty(message))
            message = LanguageManager.Pick(defaultMessage, defaultMessageFilipino);
        else if (!alreadyTranslated)
            message = Translate(message);

        if (messageText != null) messageText.text = message;
        if (titleText != null) titleText.text = LanguageManager.Pick(title, titleFilipino);

        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Vector3 pos = cam.position + cam.forward * distanceFromPlayer + Vector3.up * heightOffset;
            panel.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - cam.position, Vector3.up));
        }

        if (warningSound != null) warningSound.Play();

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine());
    }

    private string Translate(string english)
    {
        if (english == defaultMessage) return LanguageManager.Pick(defaultMessage, defaultMessageFilipino);
        if (translations != null)
            foreach (var t in translations)
                if (t != null && !string.IsNullOrEmpty(t.english) &&
                    string.Equals(t.english.Trim(), english.Trim(), System.StringComparison.OrdinalIgnoreCase))
                    return LanguageManager.Pick(t.english, t.filipino);
        return english;
    }

    private IEnumerator ShowRoutine()
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(showTime);
        panel.SetActive(false);
        routine = null;
    }
}