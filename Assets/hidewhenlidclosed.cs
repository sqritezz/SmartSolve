using UnityEngine;

// Put this on the System Unit (or anywhere) of a level.
// While the LID is closed, the parts in "Hide These" are made invisible -
// but ONLY while they are inside the case (so a plug lying on the table never vanishes).
// Open the lid and they show again. Scripts on them keep running (only the look is hidden).
//
// Use it for things that poke through the closed lid, e.g. the PCIe plug.
public class HideWhenLidClosed : MonoBehaviour
{
    [Tooltip("The system unit's lid/cover (the moving part). Must be CLOSED when the game starts.")]
    public Transform lid;
    [Tooltip("Parts to hide while the lid is closed, e.g. PCIe_Plug")]
    public GameObject[] hideThese;
    [Tooltip("The case body (any object with a collider or mesh covering the whole case). " +
             "Parts are only hidden while they are inside it. Empty = always hide while the lid is closed.")]
    public GameObject caseBody;

    [Header("When is the lid 'closed'?")]
    [Tooltip("Lid counts as closed within this distance (meters) of where it started")]
    public float closedDistance = 0.02f;
    [Tooltip("...and within this angle (degrees)")]
    public float closedAngle = 5f;

    private Vector3 closedPos;
    private Quaternion closedRot;

    private void Start()
    {
        if (lid != null) { closedPos = lid.position; closedRot = lid.rotation; }
    }

    private void LateUpdate()
    {
        bool lidClosed = lid != null &&
                         Vector3.Distance(lid.position, closedPos) <= closedDistance &&
                         Quaternion.Angle(lid.rotation, closedRot) <= closedAngle;

        Bounds caseB = GetBounds(caseBody);

        if (hideThese == null) return;
        foreach (var g in hideThese)
        {
            if (g == null) continue;
            bool inside = caseBody == null || caseB.Contains(GetBounds(g).center);
            SetVisible(g, !(lidClosed && inside));
        }
    }

    private static void SetVisible(GameObject g, bool visible)
    {
        foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            if (r.enabled != visible) r.enabled = visible;
    }

    private static Bounds GetBounds(GameObject g)
    {
        if (g == null) return new Bounds();
        var col = g.GetComponent<Collider>();
        if (col != null) return col.bounds;
        bool found = false; Bounds b = new Bounds(g.transform.position, Vector3.zero);
        foreach (var r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (!found) { b = r.bounds; found = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }
}