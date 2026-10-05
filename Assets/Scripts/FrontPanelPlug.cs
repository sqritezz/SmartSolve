using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to PowerSwitchPlug (needs Rigidbody + XR Grab Interactable + Collider).
// It snaps between "pin spots" (empty objects on the header, one per pin pair).
// Release it near a spot -> snaps there. Released nowhere near -> goes back.
// Snapping onto a WRONG pair counts as a wrong attempt.
// Moving it while the PSU is ON is a safety mistake.
public class FrontPanelPlug : MonoBehaviour
{
    [System.Serializable]
    public class PinSpot
    {
        [Tooltip("e.g. \"Pins 6-8 (PWR SW)\"")]
        public string label;
        public Transform spot;
        public bool isCorrect;
    }

    [Header("Pin Spots")]
    public PinSpot[] spots;
    [Tooltip("Which spot it starts on (the WRONG one, pins 5-7)")]
    public int startSpotIndex = 0;
    [Tooltip("How close (meters) it must be released to a spot to snap - about half the gap between two pins")]
    public float snapDistance = 0.04f;

    [Header("Safety")]
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;
    [Tooltip("e.g. \"Turn off the PSU switch before touching the cables!\"")]
    public GameObject powerOnWarning;

    [Header("Feedback")]
    [Tooltip("e.g. \"Those aren't the power switch pins. Check the manual.\"")]
    public GameObject wrongPinsMessage;
    public float messageShowTime = 2.5f;
    public AudioSource snapSound;
    public AudioSource wrongSound;

    [Header("State (read-only)")]
    public int currentSpot = 0;

    public bool IsCorrect =>
        spots != null && currentSpot >= 0 && currentSpot < spots.Length && spots[currentSpot].isCorrect;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private bool forceDropping = false;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnGrabbed);
            grab.selectExited.AddListener(OnReleased);
        }
    }

    private void Start()
    {
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
        if (wrongPinsMessage != null) wrongPinsMessage.SetActive(false);

        currentSpot = Mathf.Clamp(startSpotIndex, 0, spots.Length - 1);
        PlaceAt(currentSpot);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (psuSwitch != null && !psuSwitch.IsOff)
        {
            if (wrongSound != null) wrongSound.Play();
            ShowMessage(powerOnWarning);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
            StartCoroutine(ForceDrop());
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (forceDropping) return;
        StartCoroutine(SettleNextFrame());
    }

    private IEnumerator SettleNextFrame()
    {
        yield return null;

        int nearest = -1;
        float best = snapDistance;
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i].spot == null) continue;
            float d = Vector3.Distance(transform.position, spots[i].spot.position);
            if (d <= best) { best = d; nearest = i; }
        }

        if (nearest < 0 || nearest == currentSpot)
        {
            PlaceAt(currentSpot); // dropped nowhere useful - back where it was
            yield break;
        }

        currentSpot = nearest;
        PlaceAt(currentSpot);

        if (spots[currentSpot].isCorrect)
        {
            if (snapSound != null) snapSound.Play();
        }
        else
        {
            if (wrongSound != null) wrongSound.Play();
            ShowMessage(wrongPinsMessage);
            if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
        }
    }

    private IEnumerator ForceDrop()
    {
        forceDropping = true;
        grab.enabled = false;
        yield return null;
        grab.enabled = true;
        PlaceAt(currentSpot);
        forceDropping = false;
    }

    private void PlaceAt(int index)
    {
        if (spots == null || index < 0 || index >= spots.Length || spots[index].spot == null) return;
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        transform.SetPositionAndRotation(spots[index].spot.position, spots[index].spot.rotation);
    }

    private void ShowMessage(GameObject message)
    {
        if (message != null) StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }
}
