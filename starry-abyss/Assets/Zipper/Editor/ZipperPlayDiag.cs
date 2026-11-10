using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>在合并工程里播放 ZipperLevel，强行 BeginLevel2，然后打印相机/球/女孩坐标。</summary>
public static class ZipperPlayDiag
{
    static int step;
    static float startTime;

    public static void Run()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

        EditorSceneManager.OpenScene("Assets/Zipper/Scenes/ZipperLevel.unity");
        EditorApplication.isPlaying = true;
        startTime = -1f;
        step = 0;
        EditorApplication.update += Tick;
        Debug.Log("[PD] play started");
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (startTime < 0f) startTime = Time.realtimeSinceStartup;

        float elapsed = Time.realtimeSinceStartup - startTime;

        if (step == 0 && elapsed > 1.5f)
        {
            step = 1;
            Dump("场景开始 1.5 秒");
        }
        else if (step == 1 && elapsed > 2.0f)
        {
            step = 2;
            Level2Director l2 = Object.FindObjectOfType<Level2Director>();
            if (l2 != null)
            {
                l2.BeginLevel2();
                Debug.Log("[PD] 已调用 BeginLevel2");
            }
            else
            {
                Debug.Log("[PD] 找不到 Level2Director");
            }
        }
        else if (step == 2 && elapsed > 3.0f)
        {
            step = 3;
            Dump("BeginLevel2 后 1 秒");
        }
        else if (step == 3 && elapsed > 3.6f)
        {
            step = 4;
            Debug.Log("[PD] ---- end ----");
            EditorApplication.update -= Tick;
            EditorApplication.Exit(0);
        }
    }

    static void Dump(string tag)
    {
        Debug.Log("[PD] ===== " + tag + " =====");

        OceanPlayerController player = Object.FindObjectOfType<OceanPlayerController>();
        GirlNPC girl = Object.FindObjectOfType<GirlNPC>(true);

        Debug.Log("[PD] player=" + (player != null ? player.transform.position.ToString("F2") : "null")
                  + " mode=" + (player != null ? player.Mode.ToString() : "-")
                  + " visual=" + (player != null ? player.CurrentVisual.ToString() : "-"));

        Debug.Log("[PD] girl=" + (girl != null ? girl.transform.position.ToString("F2") : "null")
                  + " active=" + (girl != null && girl.gameObject.activeSelf));

        if (player != null && girl != null)
            Debug.Log("[PD] distance player-girl=" + Vector3.Distance(player.transform.position, girl.transform.position).ToString("F2"));

        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            ThirdPersonCamera tpc = cam.GetComponent<ThirdPersonCamera>();
            Debug.Log("[PD] camera[" + i + "] " + cam.name + " pos=" + cam.transform.position.ToString("F2")
                      + " enabled=" + cam.enabled + " active=" + cam.gameObject.activeSelf
                      + " depth=" + cam.depth + " tpc=" + (tpc != null));
        }

        Camera main = Camera.main;
        Debug.Log("[PD] Camera.main=" + (main != null ? main.name + " pos=" + main.transform.position.ToString("F2") : "null"));
    }
}
