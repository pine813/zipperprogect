using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 生成第三关场景：正交相机 + BlueRunnerLevel 预制体 + BlueFlightEntry，并加入 Build Settings。
/// 用法：-batchmode -executeMethod BlueFlightSceneBuilder.Build
/// </summary>
public static class BlueFlightSceneBuilder
{
    const string PrefabPath = "Assets/BlueRunner/Prefabs/BlueRunnerLevel.prefab";
    const string ScenePath = "Assets/BlueRunner/Scenes/BlueFlightLevel.unity";

    public static void Build()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 相机（正交，Size 5，带 AudioListener）
        GameObject cameraObject = new GameObject("BlueFlight Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.031f, 0.090f, 0.207f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<AudioListener>();

        // 关卡预制体
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError("[BlueFlight] 找不到预制体 " + PrefabPath);
            EditorApplication.Exit(2);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        BlueFlightEntry entry = instance.GetComponent<BlueFlightEntry>();
        if (entry == null) entry = instance.AddComponent<BlueFlightEntry>();

        SerializedObject serialized = new SerializedObject(entry);
        SerializedProperty cameraProperty = serialized.FindProperty("flightCamera");
        if (cameraProperty != null) cameraProperty.objectReferenceValue = camera;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        AddToBuildSettings(ScenePath);
        Debug.Log("[BlueFlight] 第三关场景已生成：" + ScenePath);
        EditorApplication.Exit(0);
    }

    static void AddToBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].path == path)
            {
                list[i] = new EditorBuildSettingsScene(path, true);
                EditorBuildSettings.scenes = list.ToArray();
                return;
            }
        }

        list.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
