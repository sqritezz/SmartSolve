using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Put this on anything you GRAB that acts weird (Old PSU, New PSU, 24-pin, screwdriver...).
//  1. Grab point: the hand holds the MIDDLE of the object, not its far-away pivot.
//  2. No throwing: it won't fly away when you let go (fixes "Cannot throw a kinematic Rigidbody").
//  3. Rest spot: if it falls through the floor or ends up far away, it goes back to its rest spot.
//
// Nothing to fill in. Optional: drag an empty into Rest Spot to choose where it returns
// (empty = where the object was when the level started / when it first appeared).
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabFix : MonoBehaviour
{
    [Header("Grab point")]
    [Tooltip("Makes a GrabPoint at the middle of the object (only if Attach Transform is empty)")]
    public bool autoGrabPoint = true;

    [Header("Rest spot")]
    [Tooltip("Where it goes back to. Empty = its starting place.")]
    public Transform restSpot;
    [Tooltip("Goes back if it drops this far (meters) below the rest spot")]
    public float fallLimit = 1.5f;
    [Tooltip("Goes back if it ends up this far (meters) from the rest spot")]
    public float maxDistance = 6f;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private Vector3 restPos;
    private Quaternion restRot;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        grab.throwOnDetach = false;
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        if (autoGrabPoint && grab.attachTransform == null)
        {
            Bounds b = new Bounds();
            bool any = false;
            foreach (var c in GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger) continue;
                if (!any) { b = c.bounds; any = true; } else b.Encapsulate(c.bounds);
            }
            if (!any)
                foreach (var r in GetComponentsInChildren<Renderer>(true))
                {
                    if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
                }

            var gp = new GameObject("GrabPoint").transform;
            gp.SetParent(transform, false);
            gp.position = any ? b.center : transform.position;
            gp.rotation = transform.rotation;
            grab.attachTransform = gp;
        }
    }

    private void Start()
    {
        restPos = restSpot != null ? restSpot.position : transform.position;
        restRot = restSpot != null ? restSpot.rotation : transform.rotation;
    }

    private void LateUpdate()
    {
        if (grab.isSelected) return;

        if (restSpot != null) { restPos = restSpot.position; restRot = restSpot.rotation; }

        bool fell = transform.position.y < restPos.y - fallLimit;
        bool lost = (transform.position - restPos).sqrMagnitude > maxDistance * maxDistance;
        if (fell || lost) ReturnToRest();
    }

    public void ReturnToRest()
    {
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(restPos, restRot);
    }
}