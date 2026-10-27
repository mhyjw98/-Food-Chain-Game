using System.IO;
using UnityEditor;
using UnityEngine;

// 지금 열려 있는 씬을 위에서 찍어 미니맵용 맵 그림을 만든다.
// 맵이 바뀌면 맵 씬을 연 상태에서 다시 실행하면 된다
public static class MinimapCaptureTool
{
    [MenuItem("Tools/Minimap/Capture Map Image")]
    private static void CaptureFromMenu()
    {
        MinimapConfig config = Selection.activeObject as MinimapConfig;
        if (config == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:MinimapConfig");
            if (guids.Length > 0)
                config = AssetDatabase.LoadAssetAtPath<MinimapConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        if (config == null)
        {
            Debug.LogWarning("[Minimap] MinimapConfig 에셋이 없습니다. Create > Minimap > Config 로 먼저 만들어 주세요.");
            return;
        }

        Debug.Log("[Minimap] 맵 그림 저장: " + Capture(config));
    }

    // 저장한 그림의 에셋 경로를 돌려준다
    public static string Capture(MinimapConfig config)
    {
        Vector2 worldSize = config.WorldSize;
        Vector2 center = config.WorldCenter;
        int ppu = Mathf.Max(1, config.capturePixelsPerUnit);
        int width = Mathf.Max(1, Mathf.RoundToInt(worldSize.x * ppu));
        int height = Mathf.Max(1, Mathf.RoundToInt(worldSize.y * ppu));

        var cameraObject = new GameObject("~MinimapCapture") { hideFlags = HideFlags.HideAndDontSave };
        var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;

        try
        {
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = worldSize.y * 0.5f;
            camera.aspect = (float)width / height;
            camera.transform.position = new Vector3(center.x, center.y, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = config.captureBackground;
            camera.cullingMask = config.captureLayers;
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply();
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(cameraObject);
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
        }

        // 설정 에셋 옆에 같은 이름으로 저장한다
        string configPath = AssetDatabase.GetAssetPath(config);
        string folder = string.IsNullOrEmpty(configPath) ? "Assets" : Path.GetDirectoryName(configPath).Replace('\\', '/');
        string assetPath = folder + "/" + config.name + "_Map.png";

        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        config.mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();

        return assetPath;
    }
}
