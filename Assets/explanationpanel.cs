using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// ONE in the whole scene (like ObjectiveWarning), e.g. on an empty
// "Explanation Panel Manager". It owns the single shared explanation panel.
// Each level only sends its text with LevelExplanations.
//
// The panel shows in front of the player and stays until the player CLICKS
// (presses the trigger anywhere), then quickly fades out.
// Needs TriggerClicker on the controllers' Ray Interactors.
public class ExplanationPanel : MonoBehaviour
{
    public static ExplanationPanel Instance { get; private set; }

    [Header("UI")]
    [Tooltip("The world-space panel (starts hidden)")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public string whyHeading = "Why did this happen?";
    public string fixHeading = "How to fix it";
    public string closeHint = "Click anywhere to continue";
    [Tooltip("Filipino versions of the headings above")]
    public string whyHeadingFilipino = "Bakit ito nangyari?";
    public string fixHeadingFilipino = "Paano ito ayusin";
    public string closeHintFilipino = "Pindutin kahit saan para magpatuloy";

    [Header("Placement (in front of the player)")]
    [Tooltip("Main Camera. Empty = finds it automatically")]
    public Transform playerHead;
    public float distanceFromPlayer = 1.2f;
    public float heightOffset = -0.05f;

    [Header("Closing")]
    [Tooltip("Optional: CanvasGroup on the panel, used for the quick fade out")]
    public CanvasGroup canvasGroup;
    [Tooltip("Ignore clicks for this long, so a leftover press doesn't close it instantly")]
    public float ignoreClicksFor = 0.5f;
    public float fadeDuration = 0.25f;

    [Header("Audio (optional)")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip showSound;

    public bool IsShowing => panel != null && panel.activeSelf;

    private struct Entry { public string title, why, fix; }
    private readonly Queue<Entry> waiting = new Queue<Entry>();
    private float shownAt;
    private bool closing;

    private void Awake()
    {
        Instance = this;
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable() { TriggerClicker.OnAnyTriggerPressed += OnAnyClick; }
    private void OnDisable() { TriggerClicker.OnAnyTriggerPressed -= OnAnyClick; }

    private void Update()
    {
        // If two explanations come at once, show them one after another
        if (!IsShowing && !closing && waiting.Count > 0)
            Display(waiting.Dequeue());
    }

    // Levels call this
    public void Show(string title, string why, string howToFix)
    {
        waiting.Enqueue(new Entry { title = title, why = why, fix = howToFix });
    }

    private void Display(Entry e)
    {
        if (panel == null) return;

        if (titleText != null) titleText.text = e.title;

        if (bodyText != null)
        {
            string body = "";
            if (!string.IsNullOrEmpty(e.why))
                body += "<b>" + LanguageManager.Pick(whyHeading, whyHeadingFilipino) + "</b>\n" + e.why + "\n\n";
            if (!string.IsNullOrEmpty(e.fix))
                body += "<b>" + LanguageManager.Pick(fixHeading, fixHeadingFilipino) + "</b>\n" + e.fix;
            string hint = LanguageManager.Pick(closeHint, closeHintFilipino);
            if (!string.IsNullOrEmpty(hint))
                body += "\n\n<size=70%><i>" + hint + "</i></size>";
            bodyText.text = body;
        }

        PlaceInFrontOfPlayer();
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        panel.SetActive(true);
        shownAt = Time.time;

        if (audioSource != null && showSound != null)
            audioSource.PlayOneShot(showSound);
    }

    // Any trigger click closes it (same as the warning panel)
    private void OnAnyClick()
    {
        if (!IsShowing || closing) return;
        if (Time.time - shownAt < ignoreClicksFor) return;
        Hide();
    }

    // Can also be called from a UI Button's OnClick
    public void Hide()
    {
        if (!IsShowing || closing) return;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        closing = true;

        if (canvasGroup != null && fadeDuration > 0f)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }
        }

        panel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        closing = false;
    }

    private void PlaceInFrontOfPlayer()
    {
        if (playerHead == null) return;

        Vector3 forward = playerHead.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = playerHead.up;
        forward.Normalize();

        Vector3 pos = playerHead.position + forward * distanceFromPlayer;
        pos.y = playerHead.position.y + heightOffset;

        panel.transform.position = pos;
        panel.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}