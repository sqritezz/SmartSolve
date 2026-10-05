using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

// Attach to the spline CABLE object (the one with Spline Container + Spline Extrude).
// Keeps one end of the cable attached to the 24-pin connector, so when the player
// pulls the connector out, the cable bends and follows instead of staying behind.
// Spline Extrude's "Rebuild On Spline Change" must be checked.
public class CableEndFollow : MonoBehaviour
{
    [Tooltip("This cable's Spline Container (leave empty to use the one on this object)")]
    public SplineContainer cable;

    [Tooltip("Empty object on the connector where the cable goes in (child of the connector)")]
    public Transform cableEndAnchor;

    [Tooltip("Which point of the spline sits at the connector: 0 = the first point you drew, -1 = the last")]
    public int knotIndex = 0;

    private Vector3 lastAnchorPos;

    private void Awake()
    {
        if (cable == null) cable = GetComponent<SplineContainer>();
    }

    private void LateUpdate()
    {
        if (cable == null || cableEndAnchor == null) return;

        // Only update when the connector actually moved
        Vector3 pos = cableEndAnchor.position;
        if ((pos - lastAnchorPos).sqrMagnitude < 1e-8f) return;
        lastAnchorPos = pos;

        Spline spline = cable.Spline;
        if (spline == null || spline.Count == 0) return;

        int i = knotIndex < 0 ? spline.Count - 1 : Mathf.Clamp(knotIndex, 0, spline.Count - 1);

        BezierKnot knot = spline[i];
        knot.Position = (float3)cable.transform.InverseTransformPoint(pos);
        spline.SetKnot(i, knot);
    }
}