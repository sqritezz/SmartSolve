using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the Guidelines panel (or any manager object).
// Detects the tutorial actions and checks them off on a
// ChecklistManager (the static list version, not SequentialChecklist).
//
// IMPORTANT: tracking does not start automatically in Start(). Call
// BeginTracking() once the player has actually spawned in the room
// (RoomEntryDetector / roomentry.cs does this).
public class GuidelineTracker : MonoBehaviour
{
    [Header("References")]
    public ChecklistManager checklist;
    [Tooltip("Drag the XR Origin (VR) here -- used to detect movement and rotation")]
    public Transform xrOrigin;

    [Header("Objective Indices (matching ChecklistManager's Objectives array)")]
    public int moveObjectiveIndex = 0;
    public int interactObjectiveIndex = 1;
    public int rotateObjectiveIndex = 2;
    [Tooltip("Add more guideline indices here (press + to add)")]
    public int[] moreObjectiveIndices = { };

    [Header("Thresholds")]
    [Tooltip("How far the player must move (meters) before 'How to move' checks off")]
    public float moveDistanceThreshold = 0.5f;
    [Tooltip("How many degrees the player must turn before 'How to rotate' checks off")]
    public float rotateAngleThreshold = 30f;

    private Vector3 startPosition;
    private float startYRotation;
    private bool moveDone = false;
    private bool interactDone = false;
    private bool rotateDone = false;
    private bool tracking = false;

    // Call this once the player has actually spawned/entered the room.
    public void BeginTracking()
    {
        if (tracking) return; // don't reset progress if called twice
        tracking = true;

        if (xrOrigin != null)
        {
            startPosition = xrOrigin.position;
            startYRotation = xrOrigin.eulerAngles.y;
        }

        // Grabbing ANYTHING counts as completing "How to interact"
        var interactables = FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
        foreach (var interactable in interactables)
        {
            interactable.selectEntered.AddListener(OnAnyInteract);
        }
    }

    private void Update()
    {
        if (!tracking || xrOrigin == null) return;

        if (!moveDone)
        {
            float dist = Vector3.Distance(xrOrigin.position, startPosition);
            if (dist >= moveDistanceThreshold)
            {
                moveDone = true;
                if (checklist != null)
                    checklist.CompleteObjective(moveObjectiveIndex);
            }
        }

        if (!rotateDone)
        {
            float angleDelta = Mathf.Abs(Mathf.DeltaAngle(startYRotation, xrOrigin.eulerAngles.y));
            if (angleDelta >= rotateAngleThreshold)
            {
                rotateDone = true;
                if (checklist != null)
                    checklist.CompleteObjective(rotateObjectiveIndex);
            }
        }
    }

    private void OnAnyInteract(SelectEnterEventArgs args)
    {
        if (!tracking || interactDone) return;

        interactDone = true;
        if (checklist != null)
            checklist.CompleteObjective(interactObjectiveIndex);
    }

    // Checks off one of the "More Objective Indices".
    // slot 0 = first one in the list, slot 1 = second, etc.
    // Call it from any event (button OnClick, NPC dialogue finished, etc.)
    public void CompleteMoreObjective(int slot)
    {
        if (checklist == null || slot < 0 || slot >= moreObjectiveIndices.Length) return;
        checklist.CompleteObjective(moreObjectiveIndices[slot]);
    }
}