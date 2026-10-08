using System.Collections;
using UnityEngine;
using TMPro;

// ONE in the scene (e.g. on an empty "Objective Warning" object).
// Shows a small warning panel in front of the player when they use something
// that isn't part of the current objective.
public class ObjectiveWarning : MonoBehaviour
{
    public static ObjectiveWarning Instance { get; private set; }

    [Header("UI")]
    [Tooltip("World-space canvas/panel for the warning (starts hidden)")]
    public GameObject panel;
    public TMP_Text messageText;
    [TextArea(1, 3)]
    public string defaultMessage = "Not yet! Follow the current objective on your checklist.";

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

    public void Show(string message)
    {
        if (panel == null) return;

        if (messageText != null)
            messageText.text = string.IsNullOrEmpty(message) ? defaultMessage : message;

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

    private IEnumerator ShowRoutine()
    {
        panel.SetActive(true);
        yield return new WaitForSeconds(showTime);
        panel.SetActive(false);
        routine = null;
    }
}
