using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Attach this to the Guidelines panel (or any manager object).
// Detects the three tutorial actions and checks them off on a
// ChecklistManager (the static list version, not SequentialChecklist)
// since all three guidelines are shown together at once.
//
// IMPORTANT: tracking no longer starts automatically in Start(). Call
// BeginTracking() once the player has actually spawned in the room --
// either from your teleport script directly, or via RoomEntryDetector.cs
// on a trigger volume at the room entrance.
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

    // Call this once the player has actually spawned/entered the room --
    // not on scene load. This is what used to happen automatically in
    // Start(), which caused the "greened while still in the main menu" bug.
    public void BeginTracking()
    {
        if (tracking) return; // don't reset progress if called twice
        tracking = true;

        if (xrOrigin != null)
        {
            startPosition = xrOrigin.position;
            startYRotation = xrOrigin.eulerAngles.y;
        }

        // Hook into every interactable in the scene so grabbing ANYTHING
        // counts as completing "How to interact"
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
}