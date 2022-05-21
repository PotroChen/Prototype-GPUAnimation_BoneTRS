using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using GPUSkinning;

public class GPUSkinningSamplerWindow : EditorWindow
{
    [MenuItem("GPUSkinningSampler/Bake")]
    public static void Bake()
    {
        GameObject[] selectedGameObjects = Selection.gameObjects;

        if (selectedGameObjects != null && selectedGameObjects.Length > 1)
        {
            ShowDialog("不支持选择多个目标");
            return;
        }

        if (selectedGameObjects == null || selectedGameObjects.Length <= 0)
        {
            ShowDialog("请选择采样目标");
            return;
        }

        GameObject gameObject = selectedGameObjects[0];
        if (gameObject == null)
        {
            ShowDialog("无法采样该文件");
            return;
        }

        GPUSkinningSampler sampler = new GPUSkinningSampler();
        if (!sampler.GenerateRawData(gameObject))
            return;

        GPUSkinningAnimation animation = sampler.Sample();
        Texture2D animationMap = sampler.CreateAnimationMap(animation);
        Mesh gpuSkinningMesh = sampler.CreateMesh(animation);


        string folderPath = SelectSavePath();
        if (string.IsNullOrEmpty(folderPath))
            return;

        string animationPath = string.Format("{0}/Animation_{1}.asset", folderPath, animation.name);
        string mapPath = string.Format("{0}/AnimationMap_{1}.asset", folderPath, animation.name);
        string meshPath = string.Format("{0}/GPUSkinning_{1}.asset", folderPath, animation.name);

        AssetDatabase.CreateAsset(animation, animationPath);//保存ScriptableObject
        AssetDatabase.CreateAsset(animationMap, mapPath);//保存贴图
        AssetDatabase.CreateAsset(gpuSkinningMesh, meshPath);//保存Mesh

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string SelectSavePath()
    {
        string prefsKey = "GPUSkinning_SavePath";
        string title = "GPUSkinning:选择储存的文件夹";

        string cachedPath = UnityEditor.EditorPrefs.GetString(prefsKey, Application.dataPath);

        string path = UnityEditor.EditorUtility.SaveFolderPanel(title, cachedPath, "GPUSkinning");

        UnityEditor.EditorPrefs.SetString(prefsKey, path);

        if (!string.IsNullOrEmpty(path))
            path = "Assets" + path.Substring(Application.dataPath.Length);

        return path;
    }

    private static void ShowDialog(string msg)
    {
        UnityEditor.EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
    }

}
