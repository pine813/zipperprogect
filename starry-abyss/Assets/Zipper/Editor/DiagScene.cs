using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>量一下 ZipperLevel 场景里女孩/球/相机的真实数据（不需要跑游戏）。</summary>
public static class DiagScene
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Zipper/Scenes/ZipperLevel.unity");

        Debug.Log("[DIAG] ---- ZipperLevel ----");

        GirlNPC girl = Object.FindObjectOfType<GirlNPC>(true);
        if (girl != null)
        {
            Transform t = girl.transform;
            Debug.Log("[DIAG] girl pos=" + t.position.ToString("F2") + " lossyScale=" + t.lossyScale.ToString("F3"));

            Renderer[] renderers = girl.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                Debug.Log("[DIAG] girl renderers=" + renderers.Length + " bounds size=" + b.size.ToString("F2")
                          + " center=" + b.center.ToString("F2"));
            }

            Collider[] colliders = girl.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Debug.Log("[DIAG] girl collider[" + i + "] " + colliders[i].GetType().Name
                          + " trigger=" + colliders[i].isTrigger + " bounds=" + colliders[i].bounds.size.ToString("F2"));
            }
        }
        else
        {
            Debug.Log("[DIAG] girl = null");
        }

        OceanPlayerController player = Object.FindObjectOfType<OceanPlayerController>(true);
        if (player != null)
            Debug.Log("[DIAG] player pos=" + player.transform.position.ToString("F2")
                      + " scale=" + player.transform.lossyScale.ToString("F2"));

        Camera cam = Camera.main;
        if (cam != null)
            Debug.Log("[DIAG] camera pos=" + cam.transform.position.ToString("F2") + " depth=" + cam.depth);

        ThirdPersonCamera tpc = Object.FindObjectOfType<ThirdPersonCamera>(true);
        if (tpc != null)
        {
            var f = typeof(ThirdPersonCamera).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Transform target = f != null ? f.GetValue(tpc) as Transform : null;
            Debug.Log("[DIAG] camera target=" + (target != null ? target.name : "null"));
        }

        Level1Director l1 = Object.FindObjectOfType<Level1Director>(true);
        Level2Director l2 = Object.FindObjectOfType<Level2Director>(true);
        Debug.Log("[DIAG] l1=" + (l1 != null) + " l2=" + (l2 != null));

        Debug.Log("[DIAG] ---- end ----");
        EditorApplication.Exit(0);
    }
}
