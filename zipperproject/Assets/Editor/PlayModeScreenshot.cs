using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 在 Play 模式里实拍：能看到落水后的鲨鱼、小虾米和鲨鱼视线锥（这些只在运行时存在，静态场景截不到）。
    /// 输出到 &lt;工程&gt;/Logs/play-*.png。
    /// </summary>
    public static class PlayModeScreenshot
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const int Width = 1280;
        const int Height = 720;

        static Level1Director director;
        static OceanPlayerController player;
        static GameObject shotCameraObject;
        static Camera shotCamera;
        static string outputDirectory;
        static bool started;
        static int phase;
        static float phaseStart;
        static bool done;

        [MenuItem("Tools/Zipper/进 Play 模式实拍")]
        public static void CaptureFromMenu()
        {
            Capture();
        }

        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);

            director = UnityEngine.Object.FindObjectOfType<Level1Director>();
            if (director == null)
            {
                Debug.LogError("[PlayShot] 找不到 Level1Director");
                EditorApplication.Exit(1);
                return;
            }

            // 进 Play 后场景会重载，这里先清空，交由 Tick 重新绑定
            director = null;
            player = null;
            outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            done = false;
            started = false;
            phase = 0;
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        static void Tick()
        {
            if (done) return;

            if (!EditorApplication.isPlaying) return;

            // Play 模式会重载场景，引用必须在这里重新取（不能用进 Play 之前抓到的对象）
            if (director == null)
            {
                director = UnityEngine.Object.FindObjectOfType<Level1Director>();
                if (director == null) return;
                player = null;
                started = false;
            }

            if (player == null)
            {
                player = director.Player;
                if (player == null) return;
            }

            if (!started)
            {
                started = true;
                phaseStart = Time.time;
                Debug.Log("[PlayShot] 已绑定场景，开始实拍");
                return;
            }

            if (shotCamera == null) CreateShotCamera();

            switch (phase)
            {
                case 0:
                    director.StartRun();
                    phase = 1;
                    phaseStart = Time.time;
                    break;

                case 1:
                    if (director.CurrentState == Level1Director.State.Swimming)
                    {
                        phase = 2;
                        phaseStart = Time.time;
                    }
                    else if (Time.time - phaseStart > 15f) Finish("没有落水");
                    break;

                case 2:
                    // 水花四散的瞬间：从高处往下拍，能看到落点圆环
                    if (Time.time - phaseStart > 0.75f)
                    {
                        Vector3 center = player.transform.position;
                        shotCamera.transform.position = center + new Vector3(0f, 22f, -18f);
                        shotCamera.transform.LookAt(new Vector3(center.x, 0f, center.z));
                        Save(shotCamera, Path.Combine(outputDirectory, "play-splash.png"));
                        phase = 3;
                        phaseStart = Time.time;
                    }
                    break;

                case 3:
                    // 等水花落地、鲨鱼游起来
                    if (Time.time - phaseStart > 1.8f)
                    {
                        ShootGameplay("play-swim.png");
                        Vector3 spot = FindAlertSpot();
                        if (spot != Vector3.zero) player.Teleport(spot);
                        phase = 4;
                        phaseStart = Time.time;
                    }
                    break;

                case 4:
                    if (Time.time - phaseStart > 0.45f)
                    {
                        ShootShark("play-vision.png");
                        Finish(null);
                    }
                    break;
            }
        }

        static Vector3 FindAlertSpot()
        {
            SharkController shark = director.GetActiveShark(0);
            if (shark == null) return Vector3.zero;
            return shark.transform.position + shark.transform.forward * 5f;
        }

        static void CreateShotCamera()
        {
            shotCameraObject = new GameObject("~PlayShotCamera");
            shotCamera = shotCameraObject.AddComponent<Camera>();
            shotCamera.fieldOfView = 55f;
            shotCamera.nearClipPlane = 0.05f;
            shotCamera.farClipPlane = 400f;
            shotCamera.clearFlags = CameraClearFlags.SolidColor;
            shotCamera.backgroundColor = new Color(0.03f, 0.24f, 0.38f);
        }

        static void ShootGameplay(string fileName)
        {
            Vector3 target = player.transform.position;
            shotCamera.transform.position = target + new Vector3(0f, 2.2f, -6.5f);
            shotCamera.transform.LookAt(target);
            Save(shotCamera, Path.Combine(outputDirectory, fileName));
        }

        static void ShootShark(string fileName)
        {
            SharkController shark = director.GetActiveShark(0);
            if (shark == null) return;

            Vector3 sharkPosition = shark.transform.position;
            // 斜侧上方 3/4 视角，能同时看到鲨鱼、视线锥和锥里的鱼
            shotCamera.transform.position = sharkPosition
                - shark.transform.forward * 5f
                + shark.transform.right * 10f
                + Vector3.up * 5.5f;
            shotCamera.transform.LookAt(sharkPosition + shark.transform.forward * 6f);
            Save(shotCamera, Path.Combine(outputDirectory, fileName));
        }

        static void Save(Camera camera, string path)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 4;

            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());

            camera.targetTexture = null;
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);

            Debug.Log("[PlayShot] 已写入 " + path);
        }

        static void Finish(string error)
        {
            if (done) return;
            done = true;

            if (!string.IsNullOrEmpty(error)) Debug.LogError("[PlayShot] " + error);

            EditorApplication.update -= Tick;
            if (shotCameraObject != null) UnityEngine.Object.DestroyImmediate(shotCameraObject);
            EditorApplication.isPlaying = false;

            Debug.Log("[PlayShot] 完成 " + (error == null ? "OK" : error));
            EditorApplication.Exit(string.IsNullOrEmpty(error) ? 0 : 1);
        }
    }
}
