using UnityEngine;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine.TextCore.LowLevel;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ChineseFontImporter
{
    private static readonly string[] ChineseFontPaths = {
        @"C:\Windows\Fonts\msyh.ttc",
        @"C:\Windows\Fonts\msyhbd.ttc",
        @"C:\Windows\Fonts\msyhl.ttc",
        @"C:\Windows\Fonts\simsun.ttc",
        @"C:\Windows\Fonts\simkai.ttf",
        @"C:\Windows\Fonts\simhei.ttf",
        @"/System/Library/Fonts/STHeiti Light.ttc",
        @"/System/Library/Fonts/STHeiti Medium.ttc",
        @"/System/Library/Fonts/STKaiti.ttc",
        @"/usr/share/fonts/truetype/wqy/wqy-microhei.ttc",
        @"/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc"
    };

#if UNITY_EDITOR
    [MenuItem("Tools/导入中文字体")]
    public static void ImportChineseFontMenu()
    {
        ImportChineseFont();
        CreateTMPFontAssets();
    }
#endif

    public static bool ImportChineseFont()
    {
#if UNITY_EDITOR
        string fontFolder = "Assets/Fonts";
        if (!Directory.Exists(fontFolder))
        {
            Directory.CreateDirectory(fontFolder);
            AssetDatabase.Refresh();
        }

        for (int i = 0; i < ChineseFontPaths.Length; i++)
        {
            string sourcePath = ChineseFontPaths[i];
            if (File.Exists(sourcePath))
            {
                string fileName = Path.GetFileName(sourcePath);
                string targetPath = Path.Combine(fontFolder, fileName);

                if (!File.Exists(targetPath))
                {
                    File.Copy(sourcePath, targetPath);
                    Debug.Log($"已导入中文字体: {fileName}");
                }

                AssetDatabase.Refresh();

                string assetPath = $"Assets/Fonts/{fileName}";
                Object fontAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (fontAsset != null)
                {
                    Debug.Log($"字体资源加载成功: {assetPath}");
                    return true;
                }
            }
        }

        Debug.LogWarning("未找到可用的中文字体文件，请手动导入");
        return false;
#else
        return false;
#endif
    }

    public static TMP_FontAsset GetOrCreateChineseFontAsset()
    {
#if UNITY_EDITOR
        string fontFolder = "Assets/Fonts";
        if (!Directory.Exists(fontFolder))
            return null;

        string[] fontFiles = Directory.GetFiles(fontFolder, "*.ttf");
        fontFiles = fontFiles.Concat(Directory.GetFiles(fontFolder, "*.ttc")).ToArray();

        TMP_FontAsset msyhFont = null;

        foreach (string fontFile in fontFiles)
        {
            string fontAssetPath = fontFile + ".asset";
            bool needCreate = false;

            if (File.Exists(fontAssetPath))
            {
                TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
                if (fontAsset != null)
                {
                    if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                    {
                        if (fontFile.Contains("msyh") && !fontFile.Contains("msyhbd") && !fontFile.Contains("msyhl"))
                            msyhFont = fontAsset;
                        continue;
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(fontAssetPath);
                        File.Delete(fontAssetPath);
                        Debug.Log("删除损坏的字体资源: " + fontAssetPath);
                        needCreate = true;
                    }
                }
                else
                {
                    File.Delete(fontAssetPath);
                    needCreate = true;
                }
            }
            else
            {
                needCreate = true;
            }

            if (needCreate)
            {
                string assetPath = fontFile.Replace("\\", "/");
                Object fontObj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
                if (fontObj != null)
                {
                    Font font = fontObj as Font;
                    if (font != null)
                    {
                        try
                        {
                            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(font);
                            if (fontAsset != null)
                            {
                                fontAsset.name = Path.GetFileNameWithoutExtension(fontFile);
                                AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                Debug.Log($"已创建TextMeshPro字体资源: {fontAssetPath}");
                                
                                if (fontFile.Contains("msyh") && !fontFile.Contains("msyhbd") && !fontFile.Contains("msyhl"))
                                    msyhFont = fontAsset;
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError($"创建字体资源失败: {fontFile}, 错误: {e.Message}");
                        }
                    }
                }
            }
        }

        if (msyhFont != null)
            return msyhFont;

        foreach (string fontFile in fontFiles)
        {
            string fontAssetPath = fontFile + ".asset";
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
            if (fontAsset != null && fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0)
                return fontAsset;
        }

        return null;
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// 为所有 Fonts 文件夹中的字体创建 TMP_FontAsset
    /// </summary>
    public static void CreateTMPFontAssets()
    {
        string fontFolder = "Assets/Fonts";
        if (!Directory.Exists(fontFolder))
        {
            Debug.LogWarning("Fonts 文件夹不存在，请先点击 Tools/导入中文字体");
            return;
        }

        string[] fontFiles = Directory.GetFiles(fontFolder, "*.ttf");
        fontFiles = fontFiles.Concat(Directory.GetFiles(fontFolder, "*.ttc")).ToArray();

        if (fontFiles.Length == 0)
        {
            Debug.LogWarning("Fonts 文件夹中没有字体文件");
            return;
        }

        int createdCount = 0;

        foreach (string fontFile in fontFiles)
        {
            string assetPath = fontFile.Replace("\\", "/");
            string fontAssetPath = fontFile.Replace("\\", "/") + ".asset";

            // 检查是否已存在有效字体
            if (File.Exists(fontAssetPath))
            {
                TMP_FontAsset existingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontAssetPath);
                if (existingFont != null && existingFont.atlasTextures != null && existingFont.atlasTextures.Length > 0)
                {
                    Debug.Log($"字体资源已存在且有效: {Path.GetFileName(fontFile)}");
                    continue;
                }
                else
                {
                    // 删除损坏的字体
                    AssetDatabase.DeleteAsset(fontAssetPath);
                    Debug.Log($"删除损坏字体资源: {fontAssetPath}");
                }
            }

            // 导入字体为 TMP_FontAsset
            Font font = AssetDatabase.LoadAssetAtPath<Font>(assetPath);
            if (font != null)
            {
                try
                {
                    // 创建 SDF 字体
                    TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                        font,                    // Font source
                        90,                      // Face point size
                        256,                     // Padding
                        GlyphRenderMode.SDFAA,   // Render mode
                        4096,                    // Atlas width
                        4096,                    // Atlas height
                        AtlasPopulationMode.Dynamic
                    );

                    if (fontAsset != null)
                    {
                        fontAsset.name = Path.GetFileNameWithoutExtension(fontFile);
                        AssetDatabase.CreateAsset(fontAsset, fontAssetPath);
                        createdCount++;
                        Debug.Log($"已创建字体资源: {fontAssetPath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"创建字体失败: {fontFile}, 错误: {e.Message}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"字体导入完成！共创建 {createdCount} 个字体资源");
    }
#endif
}