using UnityEngine;

/// <summary>
/// 进入 Zipper 场景时自愈：
/// 1. 清掉上一个场景遗留的相机；
/// 2. 清掉女孩模型（gril.fbx）自带摄像机——它会抢走画面，导致"视角卡在女孩身上"；
/// 3. 强制启用跟着球的那台主相机，并保证只有一个 AudioListener。
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

        // 1) 女孩模型里的摄像机和音频监听器必须清掉
        GirlNPC girl = Object.FindObjectOfType<GirlNPC>(true);
        int removedFromGirl = 0;
        if (girl != null)
        {
            Camera[] girlCameras = girl.GetComponentsInChildren<Camera>(true);
            for (int i = 0; i < girlCameras.Length; i++)
            {
                Camera cam = girlCameras[i];
                if (cam == null) continue;
                if (cam.transform == girl.transform) cam.enabled = false;
                else Object.Destroy(cam.gameObject);
                removedFromGirl++;
            }

            AudioListener[] girlListeners = girl.GetComponentsInChildren<AudioListener>(true);
            for (int i = 0; i < girlListeners.Length; i++)
            {
                AudioListener listener = girlListeners[i];
                if (listener == null) continue;
                if (listener.transform == girl.transform) listener.enabled = false;
                else Object.Destroy(listener.gameObject);
                removedFromGirl++;
            }
        }

        // 2) 只保留跟着球的那台，并强制启用
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

        if (ours != null)
        {
            ours.gameObject.SetActive(true);
            ours.enabled = true;
            ours.tag = "MainCamera";
        }

        // 3) AudioListener 只留一个
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        bool kept = false;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] == null) continue;
            if (!kept && listeners[i].gameObject.activeInHierarchy)
            {
                kept = true;
                continue;
            }
            listeners[i].gameObject.SetActive(false);
        }

        Debug.Log("[ZipperBootstrap] 相机 " + cameras.Length + " 台，关掉 " + disabled
                  + " 台，清掉女孩身上 " + removedFromGirl + " 个相机/监听器，保留 "
                  + (ours != null ? ours.name : "无") + "，玩家位置 " + player.transform.position.ToString("F1"));
    }
}
