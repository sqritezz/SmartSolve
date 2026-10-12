using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Put this on a TRIGGER zone inside the trash bin's opening.
// When the player lets go of an accepted object (e.g. the old RAM) inside it,
// the object drops into the bin, a sound plays, it disappears,
// and the checklist step is ticked.
public class TrashBin : MonoBehaviour
{
    [Header("What can be thrown away")]
    [Tooltip("Tags that can be tossed in, e.g. RAM (the old/broken RAM)")]
    public string[] acceptedTags = { "RAM" };

    [Header("Drop animation")]
    [Tooltip("Optional: point at the bottom of the bin. Empty = this zone's center.")]
    public Transform dropPoint;
    public float dropDuration = 0.4f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip trashSound;

    [Header("Checklist Integration (optional)")]
    public SequentialChecklist checklist;
    public int groupIndex;
    public int objectiveIndex;
    [Tooltip("Only accept the object while this step is the current one. Otherwise it's ignored (the RAM goes back).")]
    public bool onlyDuringThisStep = true;

    [Header("Retry (in case the checklist is mid-transition)")]
    public float retryWindow = 2f;
    public float retryInterval = 0.1f;

    [Header("Events (optional)")]
    public UnityEvent onTrashed;

    private bool isTrashing = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            Debug.LogWarning("[TrashBin] The collider on " + name + " should have Is Trigger ticked.");
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTrashing) return;

        GameObject obj = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

        if (!IsAccepted(obj)) return;

        // Still in the player's hand -> wait until they let go
        XRGrabInteractable grab = obj.GetComponent<XRGrabInteractable>();
        if (grab != null && (grab.isSelected || !grab.enabled)) return;

        // Not the right moment in the checklist -> ignore
        if (onlyDuringThisStep && checklist != null && !checklist.IsCurrentStep(groupIndex, objectiveIndex))
        {
            Debug.Log("[TrashBin] " + obj.name + " dropped in, but checklist isn't on Group "
                + groupIndex + "/Objective " + objectiveIndex + " yet - ignoring for now.");
            return;
        }

        isTrashing = true;
        StartCoroutine(Trash(obj, grab));
    }

    private bool IsAccepted(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;
        foreach (string t in acceptedTags)
            if (!string.IsNullOrEmpty(t) && obj.CompareTag(t)) return true;
        return false;
    }

    private IEnumerator Trash(GameObject obj, XRGrabInteractable grab)
    {
        Debug.Log("[TrashBin] Trashing " + obj.name);

        // Lock it so it can't be grabbed or trashed twice
        if (grab != null) grab.enabled = false;
        obj.tag = "Untagged";

        // Stop any "glide back" script on the RAM
        foreach (var mb in obj.GetComponents<MonoBehaviour>())
            if (mb != null && mb != this) mb.StopAllCoroutines();

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (audioSource != null && trashSound != null)
            audioSource.PlayOneShot(trashSound);

        // Drop into the bin
        Vector3 start = obj.transform.position;
        Vector3 end = dropPoint != null ? dropPoint.position : transform.position;
        float t = 0f;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            obj.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / dropDuration));
            yield return null;
        }

        obj.SetActive(false);

        yield return StartCoroutine(TryCompleteWithRetry());

        onTrashed?.Invoke();
        isTrashing = false;
    }

    private IEnumerator TryCompleteWithRetry()
    {
        if (checklist == null) yield break;

        float elapsed = 0f;
        while (elapsed < retryWindow)
        {
            if (checklist.IsCurrentStep(groupIndex, objectiveIndex))
            {
                checklist.CompleteObjective(groupIndex, objectiveIndex);
                Debug.Log("[TrashBin] Checklist step completed.");
                yield break;
            }
            yield return new WaitForSeconds(retryInterval);
            elapsed += retryInterval;
        }

        Debug.LogWarning("[TrashBin] Gave up waiting for checklist Group " + groupIndex
            + "/Objective " + objectiveIndex + " to become current.");
    }
}