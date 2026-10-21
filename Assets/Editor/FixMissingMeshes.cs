// Put this file inside a folder named "Editor" in your Assets (e.g. Assets/Editor/FixMissingMeshes.cs)
// Then use the menu: Tools > Fix Missing Meshes
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FixMissingMeshes
{
    [MenuItem("Tools/Fix Missing Meshes")]
    static void Fix()
    {
        int fixedCount = 0, notFound = 0;

        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mf.sharedMesh != null) continue;

            // Use the object name without " (12)" style suffixes, e.g. "wallHalf (3)" -> "wallHalf"
            string meshName = mf.gameObject.name;
            int paren = meshName.IndexOf(" (");
            if (paren > 0) meshName = meshName.Substring(0, paren);

            Mesh mesh = FindMesh(meshName);
            if (mesh != null)
            {
                Undo.RecordObject(mf, "Fix Missing Mesh");
                mf.sharedMesh = mesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(mf);
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"No mesh named '{meshName}' found for {mf.gameObject.name}", mf);
                notFound++;
            }
        }

        EditorSceneManager_MarkDirty();
        Debug.Log($"Fix Missing Meshes: fixed {fixedCount}, not found {notFound}");
    }

    static Mesh FindMesh(string name)
    {
        foreach (string guid in AssetDatabase.FindAssets(name))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Mesh m && m.name == name) return m;
        }
        return null;
    }

    static void EditorSceneManager_MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }
}
