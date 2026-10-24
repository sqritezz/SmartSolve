using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// ONE per tutorial (or per level). Watches the checklist and highlights the
// objects the player must use for the CURRENT step:
//   - the object pulses a glow color
//   - a bouncing arrow floats above it (hidden while the player holds it)
// When the step changes, the old highlight turns off and the next one turns on.
// Nothing else to change in other scripts.
public class TutorialHighlighter : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("Just a note for yourself, e.g. 'Turn on the TV'")]
        public string note;
        [Tooltip("Checklist group (step) number. Group 0 is usually 'talk to the NPC'.")]
        public int group;
        [Tooltip("What to highlight during this step (button, plug, port, NPC...)")]
        public GameObject[] targets;
    }

    [Header("Checklist to follow")]
    public SequentialChecklist checklist;

    [Header("Steps (press + for each)")]
    public List<Step> steps = new List<Step>();

    [Header("Glow")]
    public Color highlightColor = new Color(1f, 0.85f, 0.1f);
    [Range(0f, 1f)] public float strength = 0.65f;
    public float pulseSpeed = 3f;

    [Header("Arrow above the object")]
    public bool showArrow = true;
    [Tooltip("Arrow size in meters")]
    public float arrowSize = 0.06f;
    [Tooltip("Gap above the object (meters)")]
    public float arrowGap = 0.05f;
    public float bobHeight = 0.03f;

    private class Lit
    {
        public GameObject go;
        public Renderer[] renderers;
        public Color[][] baseColors;
        public XRBaseInteractable interactable;
        public Transform arrow;
    }

    private readonly Dictionary<GameObject, Lit> cache = new Dictionary<GameObject, Lit>();
    private readonly HashSet<GameObject> activeNow = new HashSet<GameObject>();
    private MaterialPropertyBlock mpb;
    private Material arrowMat;
    private int lastGroup = -2;

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();
    }

    private void OnDisable()
    {
        foreach (var go in activeNow) TurnOff(go);
        activeNow.Clear();
        lastGroup = -2;
    }

    private void Update()
    {
        int group = CurrentGroup();
        if (group != lastGroup)
        {
            lastGroup = group;
            var wanted = new HashSet<GameObject>();
            foreach (var s in steps)
                if (s != null && s.group == group && s.targets != null)
                    foreach (var t in s.targets) if (t != null) wanted.Add(t);

            foreach (var go in new List<GameObject>(activeNow))
                if (!wanted.Contains(go)) { TurnOff(go); activeNow.Remove(go); }
            foreach (var go in wanted) activeNow.Add(go);
        }

        float k = (Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f) * strength;
        foreach (var go in activeNow) Glow(go, k);
    }

    // ---------- Glow ----------
    private Lit Get(GameObject go)
    {
        if (cache.TryGetValue(go, out var lit)) return lit;
        lit = new Lit { go = go };
        lit.renderers = go.GetComponentsInChildren<Renderer>(true);
        lit.baseColors = new Color[lit.renderers.Length][];
        for (int r = 0; r < lit.renderers.Length; r++)
        {
            var mats = lit.renderers[r].sharedMaterials;
            lit.baseColors[r] = new Color[mats.Length];
            for (int m = 0; m < mats.Length; m++)
            {
                Color c = Color.white;
                if (mats[m] != null)
                {
                    if (mats[m].HasProperty(BaseColorID)) c = mats[m].GetColor(BaseColorID);
                    else if (mats[m].HasProperty(ColorID)) c = mats[m].GetColor(ColorID);
                }
                lit.baseColors[r][m] = c;
            }
        }
        lit.interactable = go.GetComponentInParent<XRBaseInteractable>();
        if (lit.interactable == null) lit.interactable = go.GetComponentInChildren<XRBaseInteractable>();
        cache[go] = lit;
        return lit;
    }

    private void Glow(GameObject go, float k)
    {
        var lit = Get(go);
        bool visible = go.activeInHierarchy;

        for (int r = 0; r < lit.renderers.Length; r++)
        {
            var rend = lit.renderers[r];
            if (rend == null || rend.transform.IsChildOf(transform)) continue;
            for (int m = 0; m < lit.baseColors[r].Length; m++)
            {
                Color c = Color.Lerp(lit.baseColors[r][m], highlightColor, k);
                rend.GetPropertyBlock(mpb, m);
                mpb.SetColor(BaseColorID, c);
                mpb.SetColor(ColorID, c);
                rend.SetPropertyBlock(mpb, m);
            }
        }

        if (!showArrow) return;
        bool held = lit.interactable != null && lit.interactable.isSelected;
        if (lit.arrow == null) lit.arrow = MakeArrow();
        lit.arrow.gameObject.SetActive(visible && !held);
        if (!visible || held) return;

        Bounds b = Bounds(lit);
        float bob = Mathf.Sin(Time.time * 4f) * bobHeight;
        lit.arrow.position = new Vector3(b.center.x, b.max.y + arrowGap + arrowSize + bob, b.center.z);
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 look = cam.transform.position - lit.arrow.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) lit.arrow.rotation = Quaternion.LookRotation(-look);
        }
    }

    private void TurnOff(GameObject go)
    {
        if (go == null || !cache.TryGetValue(go, out var lit)) return;
        for (int r = 0; r < lit.renderers.Length; r++)
        {
            var rend = lit.renderers[r];
            if (rend == null) continue;
            for (int m = 0; m < lit.baseColors[r].Length; m++)
            {
                rend.GetPropertyBlock(mpb, m);
                mpb.SetColor(BaseColorID, lit.baseColors[r][m]);
                mpb.SetColor(ColorID, lit.baseColors[r][m]);
                rend.SetPropertyBlock(mpb, m);
            }
        }
        if (lit.arrow != null) lit.arrow.gameObject.SetActive(false);
    }

    private static Bounds Bounds(Lit lit)
    {
        bool any = false;
        Bounds b = new Bounds(lit.go.transform.position, Vector3.zero);
        foreach (var r in lit.renderers)
        {
            if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
            if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // ---------- Arrow (a "V" made of two thin bars, pointing down) ----------
    private Transform MakeArrow()
    {
        if (arrowMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            arrowMat = new Material(s);
            arrowMat.SetColor(BaseColorID, highlightColor);
            arrowMat.SetColor(ColorID, highlightColor);
        }

        var root = new GameObject("Highlight Arrow").transform;
        root.SetParent(transform, true);
        for (int i = -1; i <= 1; i += 2)
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(bar.GetComponent<Collider>());     // so the VR ray ignores it
            bar.transform.SetParent(root, false);
            bar.transform.localScale = new Vector3(arrowSize * 0.22f, arrowSize, arrowSize * 0.22f);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, -35f * i);
            bar.transform.localPosition = new Vector3(arrowSize * 0.28f * i, 0f, 0f);
            bar.GetComponent<Renderer>().sharedMaterial = arrowMat;
        }
        root.gameObject.SetActive(false);
        return root;
    }

    // ---------- Which checklist group is showing ----------
    private int CurrentGroup()
    {
        if (checklist == null || !checklist.isActiveAndEnabled) return -1;
        for (int g = 0; g < 40; g++)
            for (int o = 0; o < 10; o++)
                if (checklist.IsCurrentStep(g, o)) return g;
        return -1;
    }
}