using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Zipper.EditorTools
{
    /// <summary>
    /// 一键构建第一关「海洋」：程序化海床碗状地形 + 水面 + 沙滩 + 蓝色球体主角 + 落水变鱼 + 鲨鱼巡逻。
    /// 用法：菜单 Tools/Zipper/重建第一关（海洋），或 -executeMethod Zipper.EditorTools.OceanLevelBuilder.Build
    /// </summary>
    public static class OceanLevelBuilder
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string MeshFolder = "Assets/Meshes";
        const string MaterialFolder = "Assets/Materials";
        const string PrefabFolder = "Assets/Prefabs";

        const float WaterLevel = 0f;
        const float ShoreRadius = 46.5f;
        const float BowlOuterRadius = 68f;

        static readonly Vector3 PlayerStart = new Vector3(0f, 19f, 0f);

        [MenuItem("Tools/Zipper/重建第一关（海洋）", priority = 0)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder("Assets/Shaders");
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Art");

            Materials materials = CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.60f, 0.67f);
            RenderSettings.fog = false;

            CreateSun();
            CreateTerrain(materials);
            CreateWater(materials);

            GameObject player = CreatePlayer(materials);
            CreateScenery(materials);
            CreateCamera(player);

            GameObject creatures = new GameObject("Creatures");

            GameObject audioObject = new GameObject("GameAudio");
            audioObject.AddComponent<GameAudio>();

            GameObject mobileObject = new GameObject("MobileControls");
            mobileObject.AddComponent<MobileControls>();

            GameObject sharkPrefab = CreateSharkPrefab(materials);
            GameObject shrimpPrefab = CreateShrimpPrefab(materials);
            GameObject dropletPrefab = CreateDropletPrefab(materials);
            GameObject bubblePrefab = CreateBubblePrefab(materials);

            Level1Director level1Director = CreateDirector(player, creatures.transform,
                sharkPrefab, shrimpPrefab, dropletPrefab, bubblePrefab);
            CreateLevel2(materials, player, level1Director);

            PrefabUtility.SaveAsPrefabAsset(player, PrefabFolder + "/PlayerFish.prefab");

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Ocean] 第一关构建完成：" + ScenePath);
        }

        struct Materials
        {
            public Material Sand;
            public Material Seabed;
            public Material Ocean;
            public Material Ball;
            public Material Fish;
            public Material Shark;
            public Material SharkBelly;
            public Material Eye;
            public Material Rock;
            public Material Kelp;
            public Material Coral;
            public Material Shell;
            public Material Pearl;
            public Material Girl;
            public Material Shrimp;
            public Material Water;
        }

        static Materials CreateMaterials()
        {
            Materials materials = new Materials();

            materials.Sand = CreateStandard("M_Sand", new Color(0.90f, 0.82f, 0.64f), 0.04f, 0f);
            materials.Sand.mainTexture = CreateNoiseTexture("T_SandGrain", 256, 0.10f);
            materials.Sand.mainTextureScale = new Vector2(16f, 16f);
            EditorUtility.SetDirty(materials.Sand);

            materials.Seabed = CreateStandard("M_Seabed", new Color(0.60f, 0.62f, 0.52f), 0.03f, 0f);
            materials.Seabed.mainTexture = CreateNoiseTexture("T_SeabedGrain", 256, 0.14f);
            materials.Seabed.mainTextureScale = new Vector2(10f, 10f);
            EditorUtility.SetDirty(materials.Seabed);

            // 卡通沙滩球：纯色球体转动看不出来，条纹才看得出滚动
            materials.Ball = CreateStandard("M_Ball", Color.white, 0.45f, 0f);
            materials.Ball.mainTexture = CreateBeachBallTexture("T_BeachBall", 256);
            EditorUtility.SetDirty(materials.Ball);
            materials.Shark = CreateStandard("M_Shark", new Color(0.34f, 0.42f, 0.50f), 0.30f, 0f);
            materials.SharkBelly = CreateStandard("M_SharkBelly", new Color(0.88f, 0.90f, 0.92f), 0.20f, 0f);
            materials.Eye = CreateStandard("M_Eye", new Color(0.02f, 0.02f, 0.03f), 0.75f, 0f);
            materials.Rock = CreateStandard("M_Rock", new Color(0.44f, 0.46f, 0.48f), 0.10f, 0f);
            materials.Kelp = CreateStandard("M_Kelp", new Color(0.32f, 0.64f, 0.36f), 0.22f, 0f);
            materials.Coral = CreateStandard("M_Coral", new Color(0.94f, 0.52f, 0.44f), 0.18f, 0f);
            materials.Shell = CreateStandard("M_Shell", new Color(0.99f, 0.88f, 0.86f), 0.60f, 0f);
            materials.Pearl = CreateStandard("M_Pearl", new Color(0.97f, 0.97f, 0.99f), 0.95f, 0.15f);
            materials.Pearl.mainTexture = CreatePearlTexture("T_Pearl", 256);
            materials.Pearl.mainTextureScale = new Vector2(2f, 1f);
            EditorUtility.SetDirty(materials.Pearl);
            materials.Girl = CreateStandard("M_Girl", new Color(0.88f, 0.76f, 0.66f), 0.25f, 0f);
            materials.Shrimp = CreateStandard("M_Shrimp", new Color(1f, 0.55f, 0.45f), 0.45f, 0f);
            materials.Water = CreateSoftMaterial("M_Water", new Color(0.86f, 0.96f, 1f, 0.55f));

            materials.Fish = CreateStandard("M_Fish", new Color(1f, 0.52f, 0.12f), 0.35f, 0f);
            materials.Fish.mainTexture = CreateStripeTexture("T_FishStripes", 128,
                new Color(1f, 0.96f, 0.90f), new Color(1f, 0.48f, 0.08f));
            materials.Fish.mainTextureScale = new Vector2(1f, 3f);
            EditorUtility.SetDirty(materials.Fish);

            materials.Ocean = CreateOceanMaterial();

            return materials;
        }

        static Material CreateOceanMaterial()
        {
            string path = MaterialFolder + "/M_Ocean.mat";
            AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find("Zipper/OceanSurface");
            if (shader == null)
            {
                Debug.LogError("[Ocean] 找不到 Zipper/OceanSurface 着色器，回退到透明 Standard");
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader);
            material.name = "M_Ocean";
            if (shader.name == "Standard")
            {
                material.color = new Color(0.10f, 0.45f, 0.65f, 0.75f);
                material.SetFloat("_Mode", 3f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateSun()
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.86f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
            lightObject.transform.rotation = Quaternion.Euler(52f, -38f, 0f);

            RenderSettings.sun = light;
            RenderSettings.skybox = CreateSkybox();
        }

        static Material CreateSkybox()
        {
            string path = MaterialFolder + "/M_Sky.mat";
            AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find("Zipper/CartoonSky");
            if (shader == null)
            {
                Debug.LogWarning("[Ocean] 找不到 Zipper/CartoonSky，天空将回退为纯色");
                return null;
            }

            Material material = new Material(shader);
            material.name = "M_Sky";
            material.SetColor("_TopColor", new Color(0.34f, 0.65f, 0.95f));
            material.SetColor("_HorizonColor", new Color(0.86f, 0.94f, 1.00f));
            material.SetColor("_BottomColor", new Color(0.72f, 0.87f, 0.88f));

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void CreateTerrain(Materials materials)
        {
            // 海床与沙滩分开成两块网格：水下用暗色材质，岸上用亮沙，避免整片沙漠感
            CreateBowlPart("Seabed", 0f, 40.5f, materials.Seabed, MeshFolder + "/OceanSeabed.asset");
            CreateBowlPart("Beach", 40.5f, BowlOuterRadius, materials.Sand, MeshFolder + "/OceanBeach.asset");
        }

        static void CreateBowlPart(string name, float minRadius, float maxRadius, Material material, string meshPath)
        {
            GameObject part = new GameObject(name);
            Mesh mesh = BuildBowlMesh(minRadius, maxRadius);
            SaveMesh(mesh, meshPath);

            MeshFilter filter = part.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;

            part.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void CreateWater(Materials materials)
        {
            GameObject water = new GameObject("OceanSurface");
            Mesh mesh = BuildDiscMesh(200f, 12, 96);
            SaveMesh(mesh, MeshFolder + "/OceanSurface.asset");

            MeshFilter filter = water.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = water.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.Ocean;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            water.transform.position = new Vector3(0f, WaterLevel, 0f);
        }

        // ---------------- 主角 ----------------

        static GameObject CreatePlayer(Materials materials)
        {
            GameObject player = new GameObject("Player");
            player.transform.position = PlayerStart;

            CharacterController body = player.AddComponent<CharacterController>();
            body.radius = 0.4f;
            body.height = 0.8f;
            body.center = Vector3.zero;
            body.slopeLimit = 55f;
            body.stepOffset = 0.35f;
            body.skinWidth = 0.03f;
            body.minMoveDistance = 0f;

            GameObject ball = CreatePrimitive("BallVisual", PrimitiveType.Sphere, player.transform,
                Vector3.zero, Vector3.one * 0.8f, Quaternion.identity, materials.Ball);

            GameObject fish = new GameObject("FishVisual");
            fish.transform.SetParent(player.transform, false);
            Transform tail = BuildFish(fish.transform, materials);
            fish.SetActive(false);

            // 第二关：撞到珍珠后变成珍珠形态
            GameObject pearl = CreatePrimitive("PearlVisual", PrimitiveType.Sphere, player.transform,
                Vector3.zero, Vector3.one * 0.72f, Quaternion.identity, materials.Pearl);
            pearl.SetActive(false);

            OceanPlayerController controller = player.AddComponent<OceanPlayerController>();
            controller.ConfigureVisuals(ball.transform, fish.transform, tail, pearl.transform);
            controller.ConfigureWater(WaterLevel, ShoreRadius);
            controller.SetMode(OceanPlayerMode.Air);

            return player;
        }

        static Transform BuildFish(Transform parent, Materials materials)
        {
            CreatePrimitive("Body", PrimitiveType.Sphere, parent,
                Vector3.zero, new Vector3(0.34f, 0.32f, 0.68f), Quaternion.identity, materials.Fish);

            GameObject tailPivot = new GameObject("TailPivot");
            tailPivot.transform.SetParent(parent, false);
            tailPivot.transform.localPosition = new Vector3(0f, 0f, -0.33f);

            CreatePrimitive("TailFin", PrimitiveType.Cube, tailPivot.transform,
                new Vector3(0f, 0f, -0.09f), new Vector3(0.04f, 0.26f, 0.2f), Quaternion.identity, materials.Fish);

            CreatePrimitive("DorsalFin", PrimitiveType.Cube, parent,
                new Vector3(0f, 0.16f, -0.02f), new Vector3(0.03f, 0.14f, 0.26f), Quaternion.Euler(-10f, 0f, 0f), materials.Fish);

            CreatePrimitive("PectoralFinL", PrimitiveType.Cube, parent,
                new Vector3(-0.15f, -0.03f, 0.12f), new Vector3(0.18f, 0.03f, 0.12f), Quaternion.Euler(0f, 0f, 20f), materials.Fish);
            CreatePrimitive("PectoralFinR", PrimitiveType.Cube, parent,
                new Vector3(0.15f, -0.03f, 0.12f), new Vector3(0.18f, 0.03f, 0.12f), Quaternion.Euler(0f, 0f, -20f), materials.Fish);

            CreatePrimitive("EyeL", PrimitiveType.Sphere, parent,
                new Vector3(-0.11f, 0.07f, 0.26f), Vector3.one * 0.07f, Quaternion.identity, materials.Eye);
            CreatePrimitive("EyeR", PrimitiveType.Sphere, parent,
                new Vector3(0.11f, 0.07f, 0.26f), Vector3.one * 0.07f, Quaternion.identity, materials.Eye);

            return tailPivot.transform;
        }

        // ---------------- 生物预制体 ----------------

        static GameObject CreateSharkPrefab(Materials materials)
        {
            GameObject shark = new GameObject("Shark");
            shark.transform.localScale = Vector3.one * 2f;

            CreatePrimitive("Body", PrimitiveType.Sphere, shark.transform,
                Vector3.zero, new Vector3(0.56f, 0.5f, 1.7f), Quaternion.identity, materials.Shark);

            CreatePrimitive("Belly", PrimitiveType.Sphere, shark.transform,
                new Vector3(0f, -0.14f, 0.05f), new Vector3(0.46f, 0.26f, 1.35f), Quaternion.identity, materials.SharkBelly);

            CreatePrimitive("Snout", PrimitiveType.Sphere, shark.transform,
                new Vector3(0f, 0f, 0.86f), new Vector3(0.3f, 0.26f, 0.5f), Quaternion.identity, materials.Shark);

            CreatePrimitive("DorsalFin", PrimitiveType.Cube, shark.transform,
                new Vector3(0f, 0.38f, 0.1f), new Vector3(0.05f, 0.42f, 0.5f), Quaternion.Euler(12f, 0f, 0f), materials.Shark);

            GameObject tailPivot = new GameObject("TailPivot");
            tailPivot.transform.SetParent(shark.transform, false);
            tailPivot.transform.localPosition = new Vector3(0f, 0f, -0.85f);

            CreatePrimitive("TailFin", PrimitiveType.Cube, tailPivot.transform,
                new Vector3(0f, 0f, -0.15f), new Vector3(0.05f, 0.55f, 0.45f), Quaternion.Euler(15f, 0f, 0f), materials.Shark);

            CreatePrimitive("PectoralFinL", PrimitiveType.Cube, shark.transform,
                new Vector3(-0.36f, -0.12f, 0.45f), new Vector3(0.36f, 0.04f, 0.2f), Quaternion.Euler(0f, 0f, 24f), materials.Shark);
            CreatePrimitive("PectoralFinR", PrimitiveType.Cube, shark.transform,
                new Vector3(0.36f, -0.12f, 0.45f), new Vector3(0.36f, 0.04f, 0.2f), Quaternion.Euler(0f, 0f, -24f), materials.Shark);

            CreatePrimitive("EyeL", PrimitiveType.Sphere, shark.transform,
                new Vector3(-0.21f, 0.12f, 0.76f), Vector3.one * 0.11f, Quaternion.identity, materials.Eye);
            CreatePrimitive("EyeR", PrimitiveType.Sphere, shark.transform,
                new Vector3(0.21f, 0.12f, 0.76f), Vector3.one * 0.11f, Quaternion.identity, materials.Eye);

            SharkController controller = shark.AddComponent<SharkController>();
            controller.ConfigureVisual(tailPivot.transform);

            string path = PrefabFolder + "/Shark.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(shark, path);
            UnityEngine.Object.DestroyImmediate(shark);
            return prefab;
        }

        static GameObject CreateShrimpPrefab(Materials materials)
        {
            GameObject shrimp = new GameObject("Shrimp");

            CreatePrimitive("Body", PrimitiveType.Sphere, shrimp.transform,
                Vector3.zero, new Vector3(0.26f, 0.24f, 0.62f), Quaternion.identity, materials.Shrimp);

            GameObject tailPivot = new GameObject("TailPivot");
            tailPivot.transform.SetParent(shrimp.transform, false);
            tailPivot.transform.localPosition = new Vector3(0f, 0f, -0.32f);

            CreatePrimitive("TailFan", PrimitiveType.Cube, tailPivot.transform,
                new Vector3(0f, 0f, -0.12f), new Vector3(0.2f, 0.03f, 0.24f), Quaternion.Euler(0f, 0f, 0f), materials.Shrimp);

            CreatePrimitive("FinTop", PrimitiveType.Cube, shrimp.transform,
                new Vector3(0f, 0.14f, -0.02f), new Vector3(0.02f, 0.1f, 0.22f), Quaternion.identity, materials.Shrimp);

            for (int i = 0; i < 3; i++)
            {
                CreatePrimitive("Leg", PrimitiveType.Cube, shrimp.transform,
                    new Vector3(0f, -0.14f, 0.06f - i * 0.12f), new Vector3(0.03f, 0.14f, 0.03f), Quaternion.Euler(0f, 0f, 18f - i * 18f), materials.Shrimp);
            }

            CreatePrimitive("AntennaL", PrimitiveType.Cube, shrimp.transform,
                new Vector3(-0.07f, 0.03f, 0.42f), new Vector3(0.015f, 0.015f, 0.3f), Quaternion.Euler(-12f, -18f, 0f), materials.Shrimp);
            CreatePrimitive("AntennaR", PrimitiveType.Cube, shrimp.transform,
                new Vector3(0.07f, 0.03f, 0.42f), new Vector3(0.015f, 0.015f, 0.3f), Quaternion.Euler(-12f, 18f, 0f), materials.Shrimp);

            CreatePrimitive("EyeL", PrimitiveType.Sphere, shrimp.transform,
                new Vector3(-0.09f, 0.09f, 0.26f), Vector3.one * 0.07f, Quaternion.identity, materials.Eye);
            CreatePrimitive("EyeR", PrimitiveType.Sphere, shrimp.transform,
                new Vector3(0.09f, 0.09f, 0.26f), Vector3.one * 0.07f, Quaternion.identity, materials.Eye);

            ShrimpController controller = shrimp.AddComponent<ShrimpController>();
            controller.ConfigureVisual(tailPivot.transform);

            string path = PrefabFolder + "/Shrimp.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(shrimp, path);
            UnityEngine.Object.DestroyImmediate(shrimp);
            return prefab;
        }

        static GameObject CreateDropletPrefab(Materials materials)
        {
            GameObject droplet = new GameObject("SplashDroplet");
            CreatePrimitive("Drop", PrimitiveType.Sphere, droplet.transform,
                Vector3.zero, Vector3.one * 0.3f, Quaternion.identity, materials.Water);

            droplet.AddComponent<SplashDroplet>();

            string path = PrefabFolder + "/SplashDroplet.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(droplet, path);
            UnityEngine.Object.DestroyImmediate(droplet);
            return prefab;
        }

        static GameObject CreateBubblePrefab(Materials materials)
        {
            GameObject bubble = new GameObject("Bubble");
            CreatePrimitive("Sphere", PrimitiveType.Sphere, bubble.transform,
                Vector3.zero, Vector3.one * 0.34f, Quaternion.identity, materials.Water);

            bubble.AddComponent<Bubble>();

            string path = PrefabFolder + "/Bubble.prefab";
            AssetDatabase.DeleteAsset(path);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(bubble, path);
            UnityEngine.Object.DestroyImmediate(bubble);
            return prefab;
        }

        // ---------------- 环境装饰 ----------------

        static void CreateScenery(Materials materials)
        {
            GameObject scenery = new GameObject("Scenery");

            Random.State previous = Random.state;
            Random.InitState(20261109);

            for (int i = 0; i < 26; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(12f, 40f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius) + 0.2f, Mathf.Sin(angle) * radius);
                float scale = Random.Range(0.9f, 2.6f);
                CreatePrimitive("Rock_" + i, PrimitiveType.Sphere, scenery.transform,
                    position, new Vector3(scale, scale * Random.Range(0.5f, 0.9f), scale),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), materials.Rock);
            }

            // 高大的海草丛：可以躲鲨鱼视线（带碰撞体，能挡住射线），并且会随波摇摆
            for (int i = 0; i < 30; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(13f, 36f);
                float height = Random.Range(6f, 11f);
                Vector3 root = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius), Mathf.Sin(angle) * radius);

                GameObject cluster = new GameObject("Kelp_" + i);
                cluster.transform.parent = scenery.transform;
                cluster.transform.position = root;
                cluster.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                int blades = Random.Range(5, 8);
                Transform[] bladeTransforms = new Transform[blades];
                for (int b = 0; b < blades; b++)
                {
                    float bladeAngle = (360f / blades) * b + Random.Range(-14f, 14f);
                    float tilt = Random.Range(3f, 11f);
                    float bladeHeight = height * Random.Range(0.72f, 1.05f);
                    GameObject blade = CreatePrimitive("Blade", PrimitiveType.Cube, cluster.transform,
                        Quaternion.Euler(0f, bladeAngle, 0f) * new Vector3(0f, bladeHeight * 0.5f, 0.35f),
                        new Vector3(0.75f, bladeHeight, 0.16f),
                        Quaternion.Euler(0f, bladeAngle, tilt), materials.Kelp);
                    bladeTransforms[b] = blade.transform;
                }

                SeaweedSway sway = cluster.AddComponent<SeaweedSway>();
                sway.Configure(Random.Range(5f, 10f), Random.Range(0.7f, 1.3f), bladeTransforms);

                BoxCollider blocker = cluster.AddComponent<BoxCollider>();
                blocker.center = new Vector3(0f, height * 0.5f, 0f);
                blocker.size = new Vector3(1.8f, height, 1.8f);
            }

            // 海扇：扁平扇形，也是遮挡物
            for (int i = 0; i < 10; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(15f, 34f);
                float height = Random.Range(2.5f, 4.5f);
                Vector3 root = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius), Mathf.Sin(angle) * radius);

                GameObject fan = new GameObject("SeaFan_" + i);
                fan.transform.parent = scenery.transform;
                fan.transform.position = root;
                fan.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                int ribs = 5;
                Transform[] ribTransforms = new Transform[ribs];
                for (int r = 0; r < ribs; r++)
                {
                    float spread = (r - (ribs - 1) * 0.5f) * 13f;
                    GameObject rib = CreatePrimitive("Rib", PrimitiveType.Cube, fan.transform,
                        Quaternion.Euler(0f, 0f, spread) * new Vector3(0f, height * 0.5f, 0f),
                        new Vector3(0.55f, height, 0.1f),
                        Quaternion.Euler(0f, 0f, spread), materials.Coral);
                    ribTransforms[r] = rib.transform;
                }

                SeaweedSway fanSway = fan.AddComponent<SeaweedSway>();
                fanSway.Configure(Random.Range(3f, 6f), Random.Range(0.6f, 1.1f), ribTransforms);

                BoxCollider blocker = fan.AddComponent<BoxCollider>();
                blocker.center = new Vector3(0f, height * 0.5f, 0f);
                blocker.size = new Vector3(2.4f, height, 0.5f);
            }

            // 管状海绵：圆柱堆，增加植物种类
            for (int i = 0; i < 12; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(10f, 30f);
                Vector3 root = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius), Mathf.Sin(angle) * radius);

                GameObject sponge = new GameObject("Sponge_" + i);
                sponge.transform.parent = scenery.transform;
                sponge.transform.position = root;

                int tubes = Random.Range(2, 4);
                for (int t = 0; t < tubes; t++)
                {
                    float tubeHeight = Random.Range(1.6f, 3.4f);
                    Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), tubeHeight * 0.5f, Random.Range(-0.5f, 0.5f));
                    CreatePrimitive("Tube", PrimitiveType.Cylinder, sponge.transform,
                        offset, new Vector3(Random.Range(0.3f, 0.5f), tubeHeight * 0.5f, Random.Range(0.3f, 0.5f)),
                        Quaternion.identity, materials.Coral);
                }
            }

            // 珊瑚团：给海床加点颜色层次
            for (int i = 0; i < 18; i++)
            {
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(10f, 36f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius) + 0.25f, Mathf.Sin(angle) * radius);
                float scale = Random.Range(0.7f, 1.8f);
                CreatePrimitive("Coral_" + i, PrimitiveType.Sphere, scenery.transform,
                    position, new Vector3(scale, scale * 0.55f, scale),
                    Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), materials.Coral);
            }

            Random.state = previous;
        }

        // ---------------- 相机与流程 ----------------

        static void CreateCamera(GameObject player)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 29f, -9f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.55f, 0.72f, 0.86f);
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 400f;
            camera.fieldOfView = 54f;

            cameraObject.AddComponent<AudioListener>();

            ThirdPersonCamera orbit = cameraObject.AddComponent<ThirdPersonCamera>();
            orbit.Configure(player.transform, 4.8f, new Vector3(0f, 0.35f, 0f), -55f, 80f, 24f);
            orbit.SnapBehindTarget();

            WaterEnvironment environment = cameraObject.AddComponent<WaterEnvironment>();
            environment.Configure(WaterLevel);
        }

        static Level1Director CreateDirector(GameObject player, Transform creatures,
            GameObject sharkPrefab, GameObject shrimpPrefab, GameObject dropletPrefab, GameObject bubblePrefab)
        {
            GameObject directorObject = new GameObject("Level1Director");
            Level1Director director = directorObject.AddComponent<Level1Director>();

            director.Configure(player.GetComponent<OceanPlayerController>(), sharkPrefab, shrimpPrefab,
                dropletPrefab, bubblePrefab, creatures, PlayerStart, WaterLevel, ShoreRadius);

            return director;
        }

        // ---------------- 第二关：沙滩 ----------------

        static void CreateLevel2(Materials materials, GameObject player, Level1Director level1)
        {
            GameObject level2Root = new GameObject("Level2");

            ClamController[] clams = CreateClams(materials, level2Root.transform);
            GirlNPC girl = CreateGirl(materials, level2Root.transform);
            Texture2D finale = CreateFinaleImage();

            GameObject directorObject = new GameObject("Level2Director");
            Level2Director director = directorObject.AddComponent<Level2Director>();
            director.Configure(player.GetComponent<OceanPlayerController>(), level1, girl, clams, finale);

            Debug.Log("[Ocean] 第二关内容：贝壳 " + clams.Length + " 个，女孩 " + (girl != null ? "已放置" : "缺失"));
        }

        static ClamController[] CreateClams(Materials materials, Transform parent)
        {
            const int count = 14;
            ClamController[] clams = new ClamController[count];

            // 按参考图做的扇形扇贝：上下两瓣、放射状棱、波浪形边缘
            Mesh bowlMesh = BuildScallopMesh("ClamBowl", 1.05f, 0.26f, 9, 0.045f, 36, 14, true, 0.07f);
            Mesh lidMesh = BuildScallopMesh("ClamLid", 1.02f, 0.24f, 9, 0.045f, 36, 14, false, 0.07f);
            SaveMesh(bowlMesh, MeshFolder + "/ClamBowl.asset");
            SaveMesh(lidMesh, MeshFolder + "/ClamLid.asset");

            Random.State previous = Random.state;
            Random.InitState(20261110);

            for (int i = 0; i < count; i++)
            {
                // 体积大小不一，但最小值也远大于主控球体（球直径 0.8 米，贝壳最小约 2 米宽）
                float scale = Random.Range(1.05f, 1.85f);

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(49f, 63f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius), Mathf.Sin(angle) * radius);

                GameObject root = new GameObject("Clam_" + i);
                root.transform.SetParent(parent, false);
                root.transform.position = position;
                root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                root.transform.localScale = Vector3.one * scale;

                GameObject bowl = new GameObject("BottomShell");
                bowl.transform.SetParent(root.transform, false);
                bowl.AddComponent<MeshFilter>().sharedMesh = bowlMesh;
                MeshRenderer bowlRenderer = bowl.AddComponent<MeshRenderer>();
                bowlRenderer.sharedMaterial = materials.Shell;
                bowlRenderer.shadowCastingMode = ShadowCastingMode.Off;

                GameObject hinge = new GameObject("Hinge");
                hinge.transform.SetParent(root.transform, false);
                hinge.transform.localPosition = new Vector3(0f, 0.26f, -0.9f);

                GameObject lid = new GameObject("TopShell");
                lid.transform.SetParent(hinge.transform, false);
                lid.transform.localPosition = new Vector3(0f, 0.24f, 0.9f);
                lid.AddComponent<MeshFilter>().sharedMesh = lidMesh;
                MeshRenderer lidRenderer = lid.AddComponent<MeshRenderer>();
                lidRenderer.sharedMaterial = materials.Shell;
                lidRenderer.shadowCastingMode = ShadowCastingMode.Off;

                GameObject pearl = CreatePrimitive("Pearl", PrimitiveType.Sphere, root.transform,
                    new Vector3(0f, 0.35f, 0.1f), Vector3.one * 0.64f, Quaternion.identity, materials.Pearl);
                pearl.SetActive(false);

                ClamController clam = root.AddComponent<ClamController>();
                clam.Configure(hinge.transform, pearl, scale);
                clams[i] = clam;
            }

            Random.state = previous;
            return clams;
        }

        /// <summary>
        /// 程序化生成一片扇贝：扇形开口 168°、波浪边、放射状棱，并且有厚度（双面 + 边缘封口）。
        /// domeUp=true 是承托珍珠的下瓣（边缘上翘），false 是盖子（边缘下扣）。
        /// </summary>
        static Mesh BuildScallopMesh(string name, float radius, float domeHeight, int ribs,
            float rimWave, int angularSegments, int radialSegments, bool domeUp, float thickness)
        {
            float halfFan = 84f * Mathf.Deg2Rad;
            float sign = domeUp ? 1f : -1f;

            int cols = angularSegments + 1;
            int rows = radialSegments + 1;
            int surfaceCount = cols * rows;

            Vector3[] vertices = new Vector3[surfaceCount * 2];
            Vector2[] uvs = new Vector2[surfaceCount * 2];

            for (int i = 0; i < rows; i++)
            {
                float u = i / (float)radialSegments;
                for (int j = 0; j < cols; j++)
                {
                    float theta = Mathf.Lerp(-halfFan, halfFan, j / (float)angularSegments);

                    float wave = 1f + rimWave * Mathf.Sin(ribs * theta * 6f);
                    float r = radius * u * wave;

                    float height = sign * domeHeight * Mathf.Pow(u, 1.7f);
                    height += sign * domeHeight * 0.22f * Mathf.Pow(u, 1.2f)
                              * (0.5f + 0.5f * Mathf.Cos(ribs * theta * 6f));

                    int index = i * cols + j;
                    float x = Mathf.Sin(theta) * r;
                    float z = Mathf.Cos(theta) * r;

                    vertices[index] = new Vector3(x, height, z);
                    vertices[index + surfaceCount] = new Vector3(x, height - sign * thickness, z);
                    uvs[index] = new Vector2(j / (float)angularSegments, u);
                    uvs[index + surfaceCount] = uvs[index];
                }
            }

            List<int> triangles = new List<int>();

            for (int i = 0; i < radialSegments; i++)
            {
                for (int j = 0; j < angularSegments; j++)
                {
                    int a = i * cols + j;
                    int b = a + 1;
                    int c = a + cols;
                    int d = c + 1;

                    // 外表面
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);

                    // 内表面（反向）
                    int ai = a + surfaceCount;
                    int bi = b + surfaceCount;
                    int ci = c + surfaceCount;
                    int di = d + surfaceCount;
                    triangles.Add(ai);
                    triangles.Add(bi);
                    triangles.Add(ci);
                    triangles.Add(bi);
                    triangles.Add(di);
                    triangles.Add(ci);
                }
            }

            // 外弧边缘封口
            int outerRingStart = radialSegments * cols;
            for (int j = 0; j < angularSegments; j++)
            {
                int a = outerRingStart + j;
                int b = a + 1;
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(a + surfaceCount);
                triangles.Add(b);
                triangles.Add(b + surfaceCount);
                triangles.Add(a + surfaceCount);
            }

            // 两条直边封口
            for (int i = 0; i < radialSegments; i++)
            {
                int leftA = i * cols;
                int leftC = leftA + cols;
                triangles.Add(leftA);
                triangles.Add(leftA + surfaceCount);
                triangles.Add(leftC);
                triangles.Add(leftC);
                triangles.Add(leftA + surfaceCount);
                triangles.Add(leftC + surfaceCount);

                int rightA = i * cols + angularSegments;
                int rightC = rightA + cols;
                triangles.Add(rightA);
                triangles.Add(rightC);
                triangles.Add(rightA + surfaceCount);
                triangles.Add(rightC);
                triangles.Add(rightC + surfaceCount);
                triangles.Add(rightA + surfaceCount);
            }

            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static GirlNPC CreateGirl(Materials materials, Transform parent)
        {
            GameObject root = new GameObject("Girl");
            root.transform.SetParent(parent, false);

            GameObject model = null;
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/gril.fbx");
            if (asset != null)
            {
                model = (GameObject)PrefabUtility.InstantiatePrefab(asset, root.transform);
                model.name = "gril";
            }

            if (model != null)
            {
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                    float rawHeight = Mathf.Max(0.001f, bounds.size.y);
                    float scale = 1.75f / rawHeight;
                    model.transform.localScale = Vector3.one * scale;

                    float minY = bounds.min.y * scale;
                    model.transform.localPosition = new Vector3(0f, -minY, 0f);

                    Debug.Log(string.Format("[Ocean] girl.fbx 原始高度 {0:F2}m，缩放 {1:F3}，脚底修正 {2:F2}",
                        rawHeight, scale, -minY));
                }

                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
                {
                    if (renderer.sharedMaterial == null) renderer.sharedMaterial = materials.Girl;
                }
            }
            else
            {
                Debug.LogWarning("[Ocean] 找不到 Assets/gril.fbx，用胶囊人形占位");
                CreatePrimitive("PlaceholderBody", PrimitiveType.Capsule, root.transform,
                    new Vector3(0f, 0.85f, 0f), new Vector3(0.5f, 0.85f, 0.5f), Quaternion.identity, materials.Girl);
                CreatePrimitive("PlaceholderHead", PrimitiveType.Sphere, root.transform,
                    new Vector3(0f, 1.72f, 0f), Vector3.one * 0.35f, Quaternion.identity, materials.Girl);
            }

            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = 1.8f;
            capsule.radius = 0.45f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            GirlNPC npc = root.AddComponent<GirlNPC>();
            npc.ConfigureVisual(model != null ? model.transform : root.transform);

            // 初始位置放在沙滩上（运行时会随机刷新）
            float startAngle = 35f * Mathf.Deg2Rad;
            float startRadius = 56f;
            root.transform.position = new Vector3(Mathf.Cos(startAngle) * startRadius, ProfileY(startRadius), Mathf.Sin(startAngle) * startRadius);

            return npc;
        }

        static Texture2D CreateFinaleImage()
        {
            string assetPath = "Assets/Resources/GirlWithPearlEarring.png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (existing != null)
            {
                Debug.Log("[Ocean] 使用已有的通关图 " + assetPath);
                return existing;
            }

            const int width = 512;
            const int height = 640;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = x / (float)width;
                    float v = y / (float)height;

                    Color color = Color.Lerp(new Color(0.05f, 0.09f, 0.12f), new Color(0.15f, 0.18f, 0.17f), v);

                    float light = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.34f, 0.72f)) * 1.1f);
                    color += new Color(0.04f, 0.04f, 0.03f) * light;

                    // 蓝色头巾
                    float turban = Ellipse(u, v, 0.50f, 0.80f, 0.23f, 0.13f);
                    if (turban < 1f) color = Color.Lerp(color, new Color(0.24f, 0.37f, 0.52f), Edge(turban));

                    // 黄色披巾
                    float cloth = Ellipse(u, v, 0.47f, 0.32f, 0.31f, 0.27f);
                    if (cloth < 1f) color = Color.Lerp(color, new Color(0.64f, 0.52f, 0.26f), Edge(cloth) * 0.92f);

                    // 脸
                    float face = Ellipse(u, v, 0.50f, 0.62f, 0.155f, 0.20f);
                    if (face < 1f) color = Color.Lerp(color, new Color(0.87f, 0.69f, 0.56f), Edge(face));

                    // 珍珠耳环
                    float pearl = Vector2.Distance(new Vector2(u, v), new Vector2(0.605f, 0.545f)) / 0.030f;
                    if (pearl < 1f) color = Color.Lerp(color, new Color(0.98f, 0.98f, 0.99f), Edge(pearl));

                    // 暗角
                    float vignette = Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.5f)) * 1.45f);
                    color = Color.Lerp(color, Color.black, vignette * 0.42f);

                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            File.WriteAllBytes(Application.dataPath + "/Resources/GirlWithPearlEarring.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Ocean] 已生成通关图占位 " + assetPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static float Ellipse(float u, float v, float centerX, float centerY, float radiusX, float radiusY)
        {
            float dx = (u - centerX) / radiusX;
            float dy = (v - centerY) / radiusY;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>椭圆边缘抗锯齿：0=完全覆盖，1=不覆盖。</summary>
        static float Edge(float normalizedDistance)
        {
            return 1f - SmoothStep01(Mathf.Clamp01((normalizedDistance - 0.82f) / 0.18f));
        }

        // ---------------- 网格 ----------------

        static float ProfileY(float radius)
        {
            if (radius <= 14f) return -16f;
            if (radius <= 26f) return Mathf.Lerp(-16f, -12f, SmoothStep01((radius - 14f) / 12f));
            if (radius <= 38f) return Mathf.Lerp(-12f, -5f, SmoothStep01((radius - 26f) / 12f));
            if (radius <= 44f) return Mathf.Lerp(-5f, -1.2f, SmoothStep01((radius - 38f) / 6f));
            if (radius <= 47f) return Mathf.Lerp(-1.2f, 0.5f, SmoothStep01((radius - 44f) / 3f));
            if (radius <= 52f) return Mathf.Lerp(0.5f, 1.6f, SmoothStep01((radius - 47f) / 5f));
            if (radius <= 60f) return Mathf.Lerp(1.6f, 1.2f, SmoothStep01((radius - 52f) / 8f));
            return Mathf.Lerp(1.2f, -0.9f, SmoothStep01(Mathf.Clamp01((radius - 60f) / 8f)));
        }

        static float SmoothStep01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        static Mesh BuildBowlMesh(float minRadius, float maxRadius)
        {
            const int segments = 96;

            bool hasCenter = minRadius <= 0.01f;
            List<float> radii = new List<float>();

            if (hasCenter)
            {
                for (float r = 1.5f; r < 20f; r += 2.5f) radii.Add(r);
                for (float r = 20f; r < 38f; r += 2f) radii.Add(r);
                for (float r = 38f; r <= 40.5f; r += 0.5f) radii.Add(r);
            }
            else
            {
                for (float r = minRadius; r < 50f; r += 1f) radii.Add(r);
                for (float r = 50f; r < maxRadius; r += 4f) radii.Add(r);
                radii.Add(maxRadius);
            }

            int rings = radii.Count;

            List<Vector3> vertices = new List<Vector3>(1 + rings * segments);
            List<Vector2> uvs = new List<Vector2>(1 + rings * segments);
            List<int> triangles = new List<int>(segments * rings * 6);

            if (hasCenter)
            {
                vertices.Add(new Vector3(0f, ProfileY(0f), 0f));
                uvs.Add(new Vector2(0.5f, 0.5f));
            }

            int firstRingVertex = hasCenter ? 1 : 0;

            for (int i = 0; i < rings; i++)
            {
                float radius = radii[i];
                for (int j = 0; j < segments; j++)
                {
                    float angle = (j / (float)segments) * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, ProfileY(radius), Mathf.Sin(angle) * radius));
                    uvs.Add(new Vector2(radius / BowlOuterRadius, j / (float)segments));
                }
            }

            if (hasCenter)
            {
                for (int j = 0; j < segments; j++)
                {
                    int next = (j + 1) % segments;
                    triangles.Add(0);
                    triangles.Add(1 + next);
                    triangles.Add(1 + j);
                }
            }

            for (int i = 0; i < rings - 1; i++)
            {
                int ringA = firstRingVertex + i * segments;
                int ringB = firstRingVertex + (i + 1) * segments;
                for (int j = 0; j < segments; j++)
                {
                    int next = (j + 1) % segments;
                    int a = ringA + j;
                    int b = ringA + next;
                    int c = ringB + j;
                    int d = ringB + next;

                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);

                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "OceanBowl_" + minRadius.ToString("F0") + "_" + maxRadius.ToString("F0");
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildDiscMesh(float radius, int rings, int segments)
        {
            List<Vector3> vertices = new List<Vector3>(1 + rings * segments);
            List<Vector2> uvs = new List<Vector2>(1 + rings * segments);
            List<int> triangles = new List<int>(segments * rings * 6);

            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (int i = 1; i <= rings; i++)
            {
                float r = radius * (i / (float)rings);
                for (int j = 0; j < segments; j++)
                {
                    float angle = (j / (float)segments) * Mathf.PI * 2f;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
                    uvs.Add(new Vector2(Mathf.Cos(angle) * 0.5f + 0.5f, Mathf.Sin(angle) * 0.5f + 0.5f));
                }
            }

            for (int j = 0; j < segments; j++)
            {
                int next = (j + 1) % segments;
                triangles.Add(0);
                triangles.Add(1 + j);
                triangles.Add(1 + next);
            }

            for (int i = 0; i < rings - 1; i++)
            {
                int ringA = 1 + i * segments;
                int ringB = 1 + (i + 1) * segments;
                for (int j = 0; j < segments; j++)
                {
                    int next = (j + 1) % segments;
                    int a = ringA + j;
                    int b = ringA + next;
                    int c = ringB + j;
                    int d = ringB + next;

                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);

                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "OceanSurfaceDisc";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void SaveMesh(Mesh mesh, string path)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        // ---------------- 通用小工具 ----------------

        static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent,
            Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation;
            gameObject.transform.localScale = localScale;

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;

            return gameObject;
        }

        static Material CreateSoftMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find("Zipper/SoftSphere");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Material CreateStandard(string name, Color color, float smoothness, float metallic)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Texture2D CreateBeachBallTexture(string name, int size)
        {
            string assetPath = MaterialFolder + "/" + name + ".png";
            string absolutePath = Application.dataPath + "/Materials/" + name + ".png";

            Color[] stripes =
            {
                new Color(1.00f, 0.38f, 0.34f),
                new Color(1.00f, 0.98f, 0.94f),
                new Color(0.38f, 0.74f, 0.96f),
                new Color(1.00f, 0.98f, 0.94f),
                new Color(1.00f, 0.83f, 0.34f),
                new Color(1.00f, 0.98f, 0.94f),
                new Color(0.58f, 0.86f, 0.64f),
                new Color(1.00f, 0.98f, 0.94f)
            };

            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int band = Mathf.Clamp((x * stripes.Length) / size, 0, stripes.Length - 1);
                    generated.SetPixel(x, y, stripes[band]);
                }
            }
            generated.Apply();
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static Texture2D CreatePearlTexture(string name, int size)
        {
            string assetPath = MaterialFolder + "/" + name + ".png";
            string absolutePath = Application.dataPath + "/Materials/" + name + ".png";

            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    // 斜向柔光带，让珍珠旋转时能看出来
                    float band = 0.5f + 0.5f * Mathf.Sin((u * 3f + v * 5f) * Mathf.PI * 2f);
                    Color color = Color.Lerp(new Color(0.84f, 0.89f, 0.96f), new Color(1f, 0.98f, 0.95f), band);
                    generated.SetPixel(x, y, color);
                }
            }
            generated.Apply();
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static Texture2D CreateNoiseTexture(string name, int size, float variation)
        {
            string assetPath = MaterialFolder + "/" + name + ".png";
            string absolutePath = Application.dataPath + "/Materials/" + name + ".png";

            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            System.Random random = new System.Random(20261109);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float value = 1f - variation * (float)random.NextDouble();
                    generated.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }
            generated.Apply();
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static Texture2D CreateStripeTexture(string name, int size, Color light, Color dark)
        {
            string assetPath = MaterialFolder + "/" + name + ".png";
            string absolutePath = Application.dataPath + "/Materials/" + name + ".png";

            Texture2D generated = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool stripe = (x / Mathf.Max(1, size / 6)) % 2 == 0;
                    generated.SetPixel(x, y, stripe ? light : dark);
                }
            }
            generated.Apply();
            File.WriteAllBytes(absolutePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent)) parent = parent.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
