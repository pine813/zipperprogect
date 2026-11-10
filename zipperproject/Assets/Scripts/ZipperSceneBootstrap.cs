using UnityEngine;

/// <summary>
/// 进入 Zipper 场景时自愈：清掉上一个场景遗留下来的相机、恢复时间缩放、保证只有一个 AudioListener。
/// 用 RuntimeInitializeOnLoadMethod(AfterSceneLoad) 实现，不需要往场景里放对象。
/// </summary>
public static class ZipperSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoad()
    {
        OceanPlayerController player = Object.FindObjectOfType<OceanPlayerController>();
        if (player == null) return; // 不是 Zipper 场景，什么都不做

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        Camera ours = null;
        int disabled = 0;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam == null) continue;

            if (cam.GetComponent<ThirdPersonCamera>() != null)
            {
                ours = cam;
                continue;
            }

            if (cam.gameObject.activeSelf)
            {
                cam.gameObject.SetActive(false);
                disabled++;
            }
        }

        if (ours != null) ours.gameObject.SetActive(true);

        // AudioListener 只保留一个
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        bool kept = false;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null) continue;
            if (!kept && listeners[i].gameObject.activeInHierarchy) { kept = true; continue; }
            listeners[i].gameObject.SetActive(false);
        }

        Debug.Log("[ZipperBootstrap] 进入 Zipper 场景：相机 " + cameras.Length + " 台，关掉 " + disabled
                  + " 台，保留 " + (ours != null ? ours.name : "无")
                  + "，玩家位置 " + player.transform.position.ToString("F1"));
    }
}
