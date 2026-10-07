using System.Collections;
using UnityEngine;

// Attach to EACH of the PSU's screws on the back of the case (needs a Collider).
// Touch it with the screwdriver and hold for a moment:
//  - FASTENED -> spins and backs out -> REMOVED (hidden)
//  - REMOVED  -> (only once the new PSU is installed) shows sticking out,
//                touch it again -> spins in -> FASTENED
// Unscrewing while the PSU switch is ON is a safety mistake.
public class PSUScrew : MonoBehaviour
{
    public enum ScrewState { Fastened, Removed }

    [Header("Tool")]
    [Tooltip("Tag on the screwdriver. Create this tag in Unity first.")]
    public string toolTag = "Screwdriver";
    [Tooltip("Seconds the screwdriver must touch the screw")]
    public float workTime = 1.2f;

    [Header("Motion")]
    [Tooltip("Local direction pointing OUT of the case (the way the screw comes out)")]
    public Vector3 screwAxis = Vector3.forward;
    [Tooltip("How far (local units) it backs out before coming off")]
    public float backOutDistance = 0.02f;
    public float spinSpeed = 720f;

    [Header("Safety")]
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;
    [Tooltip("e.g. \"Turn off the PSU switch first!\"")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Audio")]
    public AudioSource screwingSound;   // loop while turning
    public AudioSource doneSound;       // plays when off / tight
    public AudioSource warningSound;

    [Header("State (read-only)")]
    public ScrewState state = ScrewState.Fastened;
    [Tooltip("Set by PSUHardManager once the new PSU is installed")]
    public bool canFasten = false;
    [Range(0f, 1f)] public float progress = 0f;

    public bool IsFastened => state == ScrewState.Fastened;
    public bool IsRemoved => state == ScrewState.Removed;

    private Vector3 fastenedLocalPos;
    private Quaternion fastenedLocalRot;
    private Renderer[] renderers;
    private float lastTouchTime = -999f;
    private float lastWarnTime = -999f;
    private float spinAngle = 0f;

    private void Awake()
    {
        fastenedLocalPos = transform.localPosition;
        fastenedLocalRot = transform.localRotation;
        renderers = GetComponentsInChildren<Renderer>();
    }

    private void Start()
    {
        if (powerOnWarning != null) powerOnWarning.SetActive(false);
        ApplyPose();
    }

    private void OnTriggerStay(Collider other) { CheckTool(other); }
    private void OnCollisionStay(Collision collision) { CheckTool(collision.collider); }

    private void CheckTool(Collider other)
    {
        bool isTool = other.CompareTag(toolTag) ||
                      (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(toolTag));
        if (isTool) lastTouchTime = Time.time;
    }

    private void Update()
    {
        bool touching = Time.time - lastTouchTime < 0.1f;
        bool working = false;

        if (state == ScrewState.Fastened && touching)
        {
            if (psuSwitch != null && !psuSwitch.IsOff)
            {
                // Once per touch, not every frame
                if (Time.time - lastWarnTime > messageShowTime)
                {
                    lastWarnTime = Time.time;
                    if (warningSound != null) warningSound.Play();
                    if (performanceTracker != null) performanceTracker.RegisterWrongAttempt();
                    if (powerOnWarning != null) StartCoroutine(ShowMessage(powerOnWarning));
                }
            }
            else
            {
                working = true;
                progress += Time.deltaTime / workTime;
                if (progress >= 1f)
                {
                    progress = 0f;
                    state = ScrewState.Removed;
                    if (doneSound != null) doneSound.Play();
                }
            }
        }
        else if (state == ScrewState.Removed && canFasten && touching)
        {
            working = true;
            progress += Time.deltaTime / workTime;
            if (progress >= 1f)
            {
                progress = 0f;
                state = ScrewState.Fastened;
                if (doneSound != null) doneSound.Play();
            }
        }

        if (working) spinAngle += spinSpeed * Time.deltaTime;

        if (screwingSound != null)
        {
            if (working && !screwingSound.isPlaying) screwingSound.Play();
            else if (!working && screwingSound.isPlaying) screwingSound.Stop();
        }

        ApplyPose();
    }

    private void ApplyPose()
    {
        Vector3 axis = screwAxis.normalized;
        float outAmount;
        bool visible;

        if (state == ScrewState.Fastened)
        {
            outAmount = progress;          // backing out while unscrewing
            visible = true;
        }
        else
        {
            outAmount = 1f - progress;     // going back in while fastening
            visible = canFasten;           // hidden until the new PSU is in
        }

        transform.localPosition = fastenedLocalPos + axis * backOutDistance * outAmount;
        transform.localRotation = fastenedLocalRot * Quaternion.AngleAxis(spinAngle, axis);

        foreach (var r in renderers)
            if (r != null) r.enabled = visible;
    }

    private IEnumerator ShowMessage(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }
}
