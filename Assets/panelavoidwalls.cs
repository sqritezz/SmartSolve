using UnityEngine;

// Put this on the NPC's DIALOGUE PANEL (the world-space Canvas).
// If the panel ends up inside a wall / door / box collider, it slides
// toward the player until it is free, then goes back when there is room.
// Nothing else to set up - it remembers where you placed the panel.
public class PanelAvoidWalls : MonoBehaviour
{
    [Tooltip("Main Camera. Empty = finds it automatically")]
    public Transform playerHead;
    [Tooltip("The NPC (its own colliders are ignored). Empty = this panel's parent")]
    public Transform ignoreRoot;
    [Tooltip("Which layers count as walls. Default = everything")]
    public LayerMask wallLayers = ~0;

    [Header("Tuning")]
    [Tooltip("Extra space (meters) kept between the panel and the wall")]
    public float padding = 0.03f;
    [Tooltip("Max distance (meters) the panel may move toward the player")]
    public float maxPull = 1.5f;
    [Tooltip("Never come closer to the player than this (meters)")]
    public float minDistanceToPlayer = 0.4f;
    public float step = 0.03f;
    public float smooth = 12f;

    private Transform parentT;
    private Vector3 localStart;
    private RectTransform rt;
    private readonly Collider[] hits = new Collider[16];

    private void Awake()
    {
        parentT = transform.parent;
        localStart = transform.localPosition;
        rt = GetComponent<RectTransform>();
        if (ignoreRoot == null) ignoreRoot = parentT;
    }

    private void LateUpdate()
    {
        if (playerHead == null && Camera.main != null) playerHead = Camera.main.transform;
        if (playerHead == null) return;

        Vector3 basePos = parentT != null ? parentT.TransformPoint(localStart) : localStart;
        Vector3 toPlayer = playerHead.position - basePos;
        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;
        Vector3 dir = toPlayer / dist;

        float limit = Mathf.Min(maxPull, dist - minDistanceToPlayer);
        Vector3 target = basePos;
        float pulled = 0f;
        while (pulled < limit && Blocked(target))
        {
            pulled += step;
            target = basePos + dir * pulled;
        }

        transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
    }

    private bool Blocked(Vector3 pos)
    {
        Vector3 half;
        if (rt != null)
        {
            Vector3 s = transform.lossyScale;
            half = new Vector3(rt.rect.width * s.x * 0.5f, rt.rect.height * s.y * 0.5f, 0.01f)
                   + new Vector3(padding, padding, padding);
        }
        else half = Vector3.one * (0.2f + padding);

        int n = Physics.OverlapBoxNonAlloc(pos, half, hits, transform.rotation, wallLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i];
            if (c == null) continue;
            if (c.transform.IsChildOf(transform)) continue;                          // the panel itself
            if (ignoreRoot != null && c.transform.IsChildOf(ignoreRoot)) continue;   // the NPC
            if (c.transform.IsChildOf(playerHead.root)) continue;                    // the player / hands
            if (c is CharacterController) continue;
            return true;
        }
        return false;
    }
}