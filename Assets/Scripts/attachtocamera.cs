using UnityEngine;

// Attach this to ChecklistEasy (and later ChecklistMed / ChecklistHard).
// Keep the checklist GameObject INACTIVE by default in the Hierarchy.
// The moment something calls checklistEasy.SetActive(true) (e.g. when the
// player starts Easy mode), this script runs and snaps the panel to sit
// in front of the camera at the given offset, then follows it from there.
public class AttachToCamera : MonoBehaviour
{
    [Header("Position relative to camera (local space)")]
    public Vector3 localOffset = new Vector3(-0.4f, 0.2f, 1f);
    public Vector3 localEulerRotation = Vector3.zero;

    private void OnEnable()
    {
        if (Camera.main == null) return;

        transform.SetParent(Camera.main.transform, false);
        transform.localPosition = localOffset;
        transform.localEulerAngles = localEulerRotation;
    }
}