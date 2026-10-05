using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach to the compressed air can (needs Rigidbody + XR Grab Interactable).
// Grab it with the grip, hold the TRIGGER to spray.
// While spraying, anything with an AirCleanable component in front of the
// nozzle (within sprayRange) slowly gets cleaned.
// Grabbing it also completes the "Grab the air can" checklist step.
public class CompressedAirCan : MonoBehaviour
{
    [Header("Nozzle")]
    [Tooltip("Empty child at the nozzle tip. Its blue (Z) arrow must point where the air comes out.")]
    public Transform nozzle;

    [Tooltip("How far the air reaches (meters)")]
    public float sprayRange = 1.2f;

    [Tooltip("How wide the air stream is (meters) - bigger = easier to aim")]
    public float sprayRadius = 0.08f;

    [Header("Effects")]
    [Tooltip("Particle System at the nozzle (Looping on, Play On Awake off)")]
    public ParticleSystem sprayParticles;

    [Tooltip("Hiss sound (Loop on, Play On Awake off)")]
    public AudioSource sprayLoopSound;

    [Header("Checklist Integration (grabbing the can)")]
    public SequentialChecklist checklist;
    public int grabGroupIndex = 4;
    public int grabObjectiveIndex = 0;

    [Header("State (read-only)")]
    public bool isSpraying = false;

    private XRBaseInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnGrabbed);
            interactable.activated.AddListener(OnActivated);
            interactable.deactivated.AddListener(OnDeactivated);
            interactable.selectExited.AddListener(OnReleased);
        }
    }

    private void Start()
    {
        StopSpray();
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        // Only completes when it's the current step, so grabbing early or
        // re-grabbing later does nothing
        if (checklist != null && checklist.IsCurrentStep(grabGroupIndex, grabObjectiveIndex))
            checklist.CompleteObjective(grabGroupIndex, grabObjectiveIndex);
    }

    private void OnActivated(ActivateEventArgs args) { StartSpray(); }
    private void OnDeactivated(DeactivateEventArgs args) { StopSpray(); }
    private void OnReleased(SelectExitEventArgs args) { StopSpray(); }

    public void StartSpray()
    {
        isSpraying = true;
        if (sprayParticles != null) sprayParticles.Play();
        if (sprayLoopSound != null && !sprayLoopSound.isPlaying) sprayLoopSound.Play();
    }

    public void StopSpray()
    {
        isSpraying = false;
        if (sprayParticles != null) sprayParticles.Stop();
        if (sprayLoopSound != null) sprayLoopSound.Stop();
    }

    private void Update()
    {
        if (!isSpraying) return;

        AirCleanable target = FindTarget();
        if (target != null)
            target.ReceiveAir(Time.deltaTime);
    }

    private AirCleanable FindTarget()
    {
        Transform origin = nozzle != null ? nozzle : transform;

        // 1. Something right at the nozzle tip (very close spraying)
        Collider[] close = Physics.OverlapSphere(origin.position, sprayRadius, ~0, QueryTriggerInteraction.Collide);
        foreach (var col in close)
        {
            AirCleanable c = col.GetComponentInParent<AirCleanable>();
            if (c != null) return c;
        }

        // 2. Something further along the air stream
        RaycastHit[] hits = Physics.SphereCastAll(origin.position, sprayRadius, origin.forward,
                                                  sprayRange, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            AirCleanable c = hit.collider.GetComponentInParent<AirCleanable>();
            if (c != null) return c;
        }

        return null;
    }

    // Shows the spray reach in the Scene view when the can is selected
    private void OnDrawGizmosSelected()
    {
        Transform origin = nozzle != null ? nozzle : transform;
        Gizmos.color = Color.cyan;
        Vector3 end = origin.position + origin.forward * sprayRange;
        Gizmos.DrawLine(origin.position, end);
        Gizmos.DrawWireSphere(end, sprayRadius);
    }
}