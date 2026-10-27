using System.Collections;
using UnityEngine;

// Attach to EACH of the PSU's screws on the back of the case (needs a Collider).
// Touch it with the screwdriver and hold for a moment:
//  - FASTENED -> spins and backs out -> REMOVED (a copy falls to the ground)
//  - REMOVED  -> (only once the new PSU is installed) shows sticking out,
//                touch it again -> spins in -> FASTENED
// Unscrewing while the PSU switch is ON is a safety mistake.
//
// Audio: ONE Audio Source (leave empty = uses the one on this object),
// and the sounds are clips you drag in. The screwing clip loops while turning.
public class PSUScrew : MonoBehaviour
{
    public enum ScrewState { Fastened, Removed }

    [Header("Tool")]
    [Tooltip("Tag on the screwdriver. Create this tag in Unity first.")]
    public string toolTag = "Screwdriver";
    [Tooltip("Seconds the screwdriver must touch the screw")]
    public float workTime = 1.2f;
    [Tooltip("The screwdriver counts as touching when any part of it is this close (meters) to the screw. Bigger = easier in VR.")]
    public float touchRadius = 0.05f;

    [Header("Motion")]
    [Tooltip("Local direction pointing OUT of the case (the way the screw comes out)")]
    public Vector3 screwAxis = Vector3.forward;
    [Tooltip("ON = ignore Screw Axis. The screw always comes out toward the player (found automatically).")]
    public bool autoTowardPlayer = true;
    [Tooltip("How far (local units) it backs out before coming off")]
    public float backOutDistance = 0.02f;
    public float spinSpeed = 720f;
    [Tooltip("Spin the screw while turning. OFF = it only slides in/out and always stays straight.")]
    public bool spinVisual = false;

    [Header("Drop when removed")]
    [Tooltip("ON = the screw falls to the ground when it comes out (a copy with physics)")]
    public bool dropToGround = true;
    [Tooltip("Small push outward when it pops off")]
    public float dropPush = 0.4f;
    [Tooltip("Remove the dropped copy when the screw is needed again (new PSU installed)")]
    public bool clearDroppedWhenNeeded = true;

    [Header("Safety")]
    public PSUSwitch1 psuSwitch;
    public PerformanceTracker performanceTracker;
    [Tooltip("e.g. \"Turn off the PSU switch first!\"")]
    public GameObject powerOnWarning;
    public float messageShowTime = 2.5f;

    [Header("Audio")]
    [Tooltip("Leave empty to use the Audio Source on this object")]
    public AudioSource audioSource;
    public AudioClip screwingSound;   // loops while turning
    public AudioClip doneSound;       // plays when off / tight
    public AudioClip warningSound;

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
    private bool waitForRelease = false; // after a screw comes out, move the tool away before it can go back in
    private float lastWarnTime = -999f;
    private float spinAngle = 0f;
    private GameObject droppedCopy;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

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

    private readonly Collider[] nearby = new Collider[16];

    // Distance check every frame - works even when physics touch events don't fire
    private void CheckNearbyTool()
    {
        int n = Physics.OverlapSphereNonAlloc(transform.position, touchRadius, nearby, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
            if (nearby[i] != null) CheckTool(nearby[i]);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, touchRadius);
    }

    private void Update()
    {
        CheckNearbyTool();
        bool touching = Time.time - lastTouchTime < 0.1f;
        bool working = false;

        if (!touching) waitForRelease = false;

        if (state == ScrewState.Fastened && touching && !waitForRelease)
        {
            if (psuSwitch != null && !psuSwitch.IsOff)
            {
                // Once per touch, not every frame
                if (Time.time - lastWarnTime > messageShowTime)
                {
                    lastWarnTime = Time.time;
                    Play(warningSound);
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
                    waitForRelease = true;
                    spinAngle = 0f;
                    if (dropToGround) DropCopy();
                    StopLoop(); Play(doneSound);
                }
            }
        }
        if (state == ScrewState.Removed && canFasten && touching && !waitForRelease)
        {
            working = true;
            progress += Time.deltaTime / workTime;
            if (progress >= 1f)
            {
                progress = 0f;
                state = ScrewState.Fastened;
                waitForRelease = true;
                spinAngle = 0f;
                StopLoop(); Play(doneSound);
            }
        }

        // Screw is needed again -> the one on the floor goes away (looks like it was picked up)
        if (canFasten && droppedCopy != null && clearDroppedWhenNeeded)
        {
            Destroy(droppedCopy);
            droppedCopy = null;
        }

        if (working) spinAngle += spinSpeed * Time.deltaTime;

        if (working) StartLoop();
        else StopLoop();

        ApplyPose();
    }

    private bool axisFound = false;
    private Vector3 outParent = Vector3.forward;   // "out" direction in the parent's space (for position)
    private Vector3 outLocal = Vector3.forward;    // same direction in the screw's own space (for spinning)

    // Uses the screw's OWN shaft direction (one of its local axes), picking the side that faces
    // the player. So it backs out along its shaft and spins around it without tipping over.
    private void FindAxis()
    {
        Vector3 shaftLocal = screwAxis.sqrMagnitude > 0.0001f ? screwAxis.normalized : Vector3.forward;

        Camera cam = Camera.main;
        if (autoTowardPlayer && cam != null)
        {
            // player direction in the screw's own space (at its fastened pose)
            Vector3 toPlayer = cam.transform.position - transform.position;
            Vector3 toPlayerParent = transform.parent != null ? transform.parent.InverseTransformDirection(toPlayer) : toPlayer;
            Vector3 p = Quaternion.Inverse(fastenedLocalRot) * toPlayerParent;
            Vector3 a = new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));
            if (a.x >= a.y && a.x >= a.z) shaftLocal = new Vector3(Mathf.Sign(p.x), 0, 0);
            else if (a.y >= a.z) shaftLocal = new Vector3(0, Mathf.Sign(p.y), 0);
            else shaftLocal = new Vector3(0, 0, Mathf.Sign(p.z));
        }

        outLocal = shaftLocal;
        outParent = fastenedLocalRot * shaftLocal;
        axisFound = true;
    }

    private Vector3 WorldOut()
    {
        if (!axisFound) FindAxis();
        return transform.parent != null ? transform.parent.TransformDirection(outParent) : outParent;
    }

    private void ApplyPose()
    {
        if (!axisFound && progress > 0f) FindAxis();   // decide when the player starts unscrewing (they're right there)
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

        transform.localPosition = fastenedLocalPos + outParent * backOutDistance * outAmount;
        transform.localRotation = spinVisual
            ? fastenedLocalRot * Quaternion.AngleAxis(spinAngle, outLocal)
            : fastenedLocalRot;

        foreach (var r in renderers)
            if (r != null) r.enabled = visible;
    }

    private bool looping = false;

    private void StartLoop()
    {
        if (looping || audioSource == null || screwingSound == null) return;
        audioSource.clip = screwingSound;
        audioSource.loop = true;
        audioSource.Play();
        looping = true;
    }

    private void StopLoop()
    {
        if (!looping || audioSource == null) return;
        audioSource.Stop();
        audioSource.loop = false;
        looping = false;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        try { audioSource.PlayOneShot(clip); }
        catch (System.Exception)
        {
            Debug.LogWarning("[PSUScrew] A sound slot is broken. Set it to None and drag the clip in again.", this);
        }
    }

    private IEnumerator ShowMessage(GameObject message)
    {
        message.SetActive(true);
        yield return new WaitForSeconds(messageShowTime);
        message.SetActive(false);
    }

    // Makes a physics copy of the screw's visuals that falls to the ground
    private void DropCopy()
    {
        if (droppedCopy != null) Destroy(droppedCopy);

        droppedCopy = new GameObject(name + " (dropped)");
        droppedCopy.transform.SetPositionAndRotation(transform.position, transform.rotation);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var piece = new GameObject(r.name);
            piece.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
            piece.transform.localScale = r.transform.lossyScale;
            piece.transform.SetParent(droppedCopy.transform, true);

            piece.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            piece.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
            var box = piece.AddComponent<BoxCollider>();
            box.center = mf.sharedMesh.bounds.center;
            box.size = mf.sharedMesh.bounds.size;
        }

        var rb = droppedCopy.AddComponent<Rigidbody>();
        rb.mass = 0.02f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        Vector3 outDir = WorldOut().normalized;
        rb.linearVelocity = outDir * dropPush;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }
}