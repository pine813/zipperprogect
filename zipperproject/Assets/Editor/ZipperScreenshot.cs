using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 在 batchmode 里把第一关渲成 PNG：开场（空中球体）、水下（鲨鱼）、沙滩。
    /// 输出到 &lt;工程&gt;/Logs/shot-*.png。
    /// </summary>
    public static class ZipperScreenshot
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const int Width = 1280;
        const int Height = 720;

        [MenuItem("Tools/Zipper/渲染关卡截图")]
        public static void CaptureFromMenu()
        {
            Capture();
        }

        public static void Capture()
        {
            EditorSceneManager.OpenScene(ScenePath);

            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

            Color previousAmbient = RenderSettings.ambientLight;
            bool previousFog = RenderSettings.fog;
            GameObject shotCameraObject = null;

            try
            {
                shotCameraObject = new GameObject("~ShotCamera");
                Camera shotCamera = shotCameraObject.AddComponent<Camera>();
                shotCamera.fieldOfView = 58f;
                shotCamera.clearFlags = CameraClearFlags.SolidColor;
                shotCamera.nearClipPlane = 0.05f;
                shotCamera.farClipPlane = 600f;

                // 1) 俯瞰：整个海洋碗 + 沙滩
                RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f);
                RenderSettings.fog = false;
                shotCamera.clearFlags = CameraClearFlags.Skybox;
                shotCamera.transform.position = new Vector3(48f, 30f, -52f);
                shotCamera.transform.LookAt(new Vector3(0f, -5f, 0f));
                Save(shotCamera, Path.Combine(directory, "shot-ocean-overview.png"));

                // 2) 水下：鲨鱼 + 海床 + 水面
                RenderSettings.ambientLight = new Color(0.12f, 0.32f, 0.44f);
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.032f;
                RenderSettings.fogColor = new Color(0.04f, 0.22f, 0.34f);
                shotCamera.backgroundColor = new Color(0.04f, 0.22f, 0.34f);
                shotCamera.transform.position = new Vector3(5f, -6f, 32f);
                shotCamera.transform.LookAt(new Vector3(0f, -5f, 21f));
                Save(shotCamera, Path.Combine(directory, "shot-underwater.png"));

                // 3) 沙滩视角：从岸上看海
                RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f);
                RenderSettings.fog = false;
                shotCamera.clearFlags = CameraClearFlags.Skybox;
                shotCamera.transform.position = new Vector3(0f, 5.5f, 56f);
                shotCamera.transform.LookAt(new Vector3(0f, -3f, 8f));
                Save(shotCamera, Path.Combine(directory, "shot-beach.png"));

                // 4) 第二关：女孩特写
                RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f);
                RenderSettings.fog = false;
                shotCamera.clearFlags = CameraClearFlags.Skybox;

                GameObject girlObject = GameObject.Find("Girl");
                if (girlObject != null)
                {
                    Vector3 girlPosition = girlObject.transform.position;

                    // gril.fbx 正面朝 -Z：从 -Z 方向拍才是正脸
                    shotCamera.transform.position = new Vector3(girlPosition.x, girlPosition.y + 2.2f, girlPosition.z - 4.5f);
                    shotCamera.transform.LookAt(girlPosition + Vector3.up * 0.95f);
                    Save(shotCamera, Path.Combine(directory, "shot-girl-front.png"));

                    shotCamera.transform.position = new Vector3(girlPosition.x, girlPosition.y + 2.2f, girlPosition.z + 4.5f);
                    shotCamera.transform.LookAt(girlPosition + Vector3.up * 0.95f);
                    Save(shotCamera, Path.Combine(directory, "shot-girl-back.png"));

                    shotCamera.transform.position = new Vector3(girlPosition.x + 4.5f, 3.0f, girlPosition.z + 4.5f);
                    shotCamera.transform.LookAt(girlPosition + Vector3.up * 0.95f);
                    Save(shotCamera, Path.Combine(directory, "shot-girl.png"));
                }

                // 5) 第二关：沙滩上的贝壳群
                shotCamera.transform.position = new Vector3(30f, 14f, 62f);
                shotCamera.transform.LookAt(new Vector3(44f, 1f, 30f));
                Save(shotCamera, Path.Combine(directory, "shot-beach-level2.png"));

                // 6) 球体特写：检查卡通条纹是否能看岀滚动
                GameObject playerObject = GameObject.Find("Player");
                if (playerObject != null)
                {
                    Vector3 playerPosition = playerObject.transform.position;
                    shotCamera.transform.position = new Vector3(playerPosition.x + 1.6f, playerPosition.y + 1.0f, playerPosition.z - 2.2f);
                    shotCamera.transform.LookAt(playerPosition);
                    Save(shotCamera, Path.Combine(directory, "shot-ball.png"));
                }

                // 7) 贝壳特写：检查是否把珍珠完全包住
                GameObject clamObject = GameObject.Find("Clam_0");
                if (clamObject != null)
                {
                    Vector3 clamPosition = clamObject.transform.position;
                    shotCamera.transform.position = new Vector3(clamPosition.x + 2.6f, clamPosition.y + 2.0f, clamPosition.z - 2.6f);
                    shotCamera.transform.LookAt(clamPosition + Vector3.up * 0.35f);
                    Save(shotCamera, Path.Combine(directory, "shot-clam.png"));
                }

                // 8) 开场：场景里配好的相机（球体悬在空中，海在下方）
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Save(mainCamera, Path.Combine(directory, "shot-start.png"));
                }

                Debug.Log("[Shot] 截图完成: " + directory);
            }
            catch (Exception e)
            {
                Debug.LogError("[Shot] 截图失败: " + e);
            }
            finally
            {
                RenderSettings.ambientLight = previousAmbient;
                RenderSettings.fog = previousFog;
                if (shotCameraObject != null) UnityEngine.Object.DestroyImmediate(shotCameraObject);
                EditorApplication.Exit(0);
            }
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

            Debug.Log("[Shot] 已写入 " + path);
        }
    }
}
