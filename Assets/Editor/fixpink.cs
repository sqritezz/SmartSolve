// Put this file in Assets/Editor/ (create the "Editor" folder if needed)
// Then click: Tools > Fix ALL Pink Materials
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixPinkMaterials
{
    [MenuItem("Tools/Fix ALL Pink Materials")]
    static void Fix()
    {
        Shader standard = Shader.Find("Standard");
        if (standard == null) { Debug.LogError("Standard shader not found."); return; }

        // Fallback material for empty slots
        Material fallback = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

        var fixedMats = new HashSet<Material>();
        int emptySlots = 0;

        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var mats = r.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                // Case 1: missing material
                if (mats[i] == null)
                {
                    mats[i] = fallback;
                    changed = true;
                    emptySlots++;
                    continue;
                }

                // Case 2: broken / unsupported shader (URP, HDRP, error shader)
                Material m = mats[i];
                if (fixedMats.Contains(m) || !IsBroken(m.shader)) continue;

                Undo.RecordObject(m, "Fix Pink Material");

                // Save URP-style texture/color before switching shader
                Texture baseMap = m.HasProperty("_BaseMap") ? m.GetTexture("_BaseMap") : null;
                Color baseColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white;
                Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;

                m.shader = standard;

                if (baseMap != null) m.SetTexture("_MainTex", baseMap);
                m.SetColor("_Color", baseColor);
                if (normal != null) m.SetTexture("_BumpMap", normal);

                EditorUtility.SetDirty(m);
                fixedMats.Add(m);
            }

            if (changed)
            {
                Undo.RecordObject(r, "Fix Missing Material");
                r.sharedMaterials = mats;
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"Fixed {fixedMats.Count} broken materials and filled {emptySlots} empty material slots.");
    }

    static bool IsBroken(Shader s)
    {
        if (s == null || !s.isSupported) return true;
        string n = s.name;
        return n.Contains("InternalErrorShader")
            || n.StartsWith("Universal Render Pipeline")
            || n.StartsWith("HDRP")
            || n.StartsWith("Shader Graphs");
    }
}