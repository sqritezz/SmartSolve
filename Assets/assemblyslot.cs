using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to an empty GameObject positioned exactly where a piece should
// end up (e.g. "MonitorSlot" on the desk, "ChairSlot" on the floor).
// Add a trigger Collider (isTrigger = true) sized generously around that spot
// so the piece can be released nearby, not pixel-perfect.
[RequireComponent(typeof(Collider))]
public class AssemblySlot : MonoBehaviour
{
    [Tooltip("Must exactly match the AssemblyPiece.partName this slot accepts")]
    public string expectedPartName;

    [Tooltip("Exact position/rotation the piece snaps to. Defaults to this GameObject's transform if left empty.")]
    public Transform snapPoint;

    [Header("Events")]
    public UnityEvent onPieceSnapped;

    private bool isFilled = false;

    private void Awake()
    {
        if (snapPoint == null)
            snapPoint = transform;
    }

    private void OnTriggerStay(Collider other)
    {
        if (isFilled) return;

        AssemblyPiece piece = other.GetComponentInParent<AssemblyPiece>();
        if (piece == null || piece.partName != expectedPartName || piece.isSnapped) return;

        // Only snap once the player has let go of it (not mid-grab)
        XRGrabInteractable grab = piece.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected) return;

        SnapPiece(piece, grab);
    }

    private void SnapPiece(AssemblyPiece piece, XRGrabInteractable grab)
    {
        isFilled = true;

        piece.transform.position = snapPoint.position;
        piece.transform.rotation = snapPoint.rotation;

        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Lock it in place -- no more picking it up once it's correctly assembled
        if (grab != null)
            grab.enabled = false;

        piece.OnSnapped();
        onPieceSnapped?.Invoke();
    }
}