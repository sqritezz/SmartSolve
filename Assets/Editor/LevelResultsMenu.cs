using UnityEditor;

// Menu: Tools > SmartSolve > Reset Saved Times And Quiz Scores
// Clears every saved best time and quiz score (works in or out of Play mode).
public static class LevelResultsMenu
{
    [MenuItem("Tools/SmartSolve/Reset Saved Times And Quiz Scores")]
    private static void ResetAll()
    {
        LevelResults.ResetAll();
    }
}
