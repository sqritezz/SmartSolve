using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// TEMPORARY test. Put on Left Ray Grab and Right Ray Grab.
// Press Play and point at a wall: the Console shows WHAT the ray is hitting
// (object name + layer). Click the log to highlight that object in the Hierarchy.
// Remove this script when done.
public class RayHitDebug : MonoBehaviour
{
    private XRRayInteractor ray;
    private GameObject last;

    private void Awake() { ray = GetComponent<XRRayInteractor>(); }

    private void Update()
    {
        if (ray == null) return;
        if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            GameObject g = hit.collider.gameObject;
            if (g != last)
            {
                last = g;
                Debug.Log("[RayHitDebug] " + name + " hits: " + GetPath(g.transform) +
                          "  | layer: " + LayerMask.LayerToName(g.layer), g);
            }
        }
        else last = null;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}