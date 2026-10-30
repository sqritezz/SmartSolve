using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// TEMPORARY test. Put on Left Ray Grab and Right Ray Grab.
// Press Play and point at a wall: the Console shows WHAT the ray is hitting.
//   "hits 3D:" = a collider (object name + layer)
//   "hits UI:" = a Canvas (UI raycasts ignore layers like Decor!)
// Click the log to highlight that object in the Hierarchy.
// Remove this script when done.
public class RayHitDebug : MonoBehaviour
{
    private XRRayInteractor ray;
    private GameObject last;

    private void Awake() { ray = GetComponent<XRRayInteractor>(); }

    private void Update()
    {
        if (ray == null) return;

        GameObject g = null;
        string kind = "";

        if (ray.TryGetCurrentUIRaycastResult(out UnityEngine.EventSystems.RaycastResult ui) && ui.gameObject != null)
        {
            g = ui.gameObject;
            Canvas c = g.GetComponentInParent<Canvas>();
            kind = "UI (canvas: " + (c != null ? GetPath(c.transform) : "?") + ")";
        }
        else if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            g = hit.collider.gameObject;
            kind = "3D";
        }

        if (g != null && g != last)
        {
            Debug.Log("[RayHitDebug] " + name + " hits " + kind + ": " + GetPath(g.transform) +
                      "  | layer: " + LayerMask.LayerToName(g.layer), g);
        }
        last = g;
    }

    private static string GetPath(Transform t)
    {
        string p = t.name;
        while (t.parent != null) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}