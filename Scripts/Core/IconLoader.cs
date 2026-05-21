using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public static class IconLoader
{
    private const string ICONS_FOLDER = "ShopIcons";
    private const string CACHE_SUBFOLDER = "DownloadedIcons";
    private const string FALLBACK_ICON = "icon_placeholder";

    public static Sprite LoadFromResources(string iconName)
    {
        if (string.IsNullOrEmpty(iconName)) iconName = FALLBACK_ICON;
        string path = $"{ICONS_FOLDER}/{iconName}";
        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null) return sprite;

        sprite = Resources.Load<Sprite>($"{ICONS_FOLDER}/{FALLBACK_ICON}");
        if (sprite == null) Debug.LogError("[IconLoader] Fallback icon missing! Place in Resources/ShopIcons/icon_placeholder.png");
        return sprite;
    }

    public static IEnumerator LoadIcon(string iconName, System.Action<Sprite> onLoaded)
    {
        if (string.IsNullOrEmpty(iconName)) iconName = FALLBACK_ICON;

        string cachePath = Path.Combine(Application.persistentDataPath, CACHE_SUBFOLDER, $"{iconName}.png");
        if (File.Exists(cachePath))
        {
            byte[] fileData = File.ReadAllBytes(cachePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData);
            onLoaded?.Invoke(Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)));
            yield break;
        }

        Sprite resSprite = LoadFromResources(iconName);
        if (resSprite != null) { onLoaded?.Invoke(resSprite); yield break; }
        onLoaded?.Invoke(LoadFromResources(null));
    }

    public static void SaveToCache(string iconName, byte[] imageData)
    {
        string cacheFolder = Path.Combine(Application.persistentDataPath, CACHE_SUBFOLDER);
        if (!Directory.Exists(cacheFolder)) Directory.CreateDirectory(cacheFolder);
        File.WriteAllBytes(Path.Combine(cacheFolder, $"{iconName}.png"), imageData);
    }
}