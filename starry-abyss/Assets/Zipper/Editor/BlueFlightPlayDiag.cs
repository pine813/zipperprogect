using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>播放 BlueFlightLevel，检查关卡构建、相机与通关回调。</summary>
public static class BlueFlightPlayDiag
{
    static int step;
    static float startTime = -1f;

    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        EditorSceneManager.OpenScene("Assets/BlueRunner/Scenes/BlueFlightLevel.unity");
        EditorApplication.isPlaying = true;
        step = 0;
        startTime = -1f;
        EditorApplication.update += Tick;
        Debug.Log("[BF] play started");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (startTime < 0f) startTime = Time.realtimeSinceStartup;
        float elapsed = Time.realtimeSinceStartup - startTime;

        if (step == 0 && elapsed > 1.6f)
        {
            step = 1;
            BlueRunner.GameRoot root = Object.FindObjectOfType<BlueRunner.GameRoot>();
            BlueFlightEntry entry = Object.FindObjectOfType<BlueFlightEntry>();
            Camera main = Camera.main;

            Debug.Log("[BF] root=" + (root != null)
                      + " state=" + (root != null ? root.State.ToString() : "-")
                      + " player=" + (root != null && root.Player != null)
                      + " entry=" + (entry != null));
            Debug.Log("[BF] camera=" + (main != null ? main.name : "null")
                      + " ortho=" + (main != null && main.orthographic)
                      + " size=" + (main != null ? main.orthographicSize.ToString("F1") : "-"));

            if (root != null) root.Win();
        }
        else if (step == 1 && elapsed > 3.0f)
        {
            step = 2;
            BlueRunner.GameRoot root = Object.FindObjectOfType<BlueRunner.GameRoot>();
            Debug.Log("[BF] after win state=" + (root != null ? root.State.ToString() : "-"));
            Debug.Log("[BF] ---- end ----");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
    }
}
