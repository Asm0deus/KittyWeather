using System;
using UnityEngine;

/// <summary>
/// УСТАРЕВШИЙ КЛАСС. Не использовать в новом коде.
/// Для загрузки ачивок RemoteAchievementLoader.cs (CDN + JSON + кэш).
/// Этот класс оставлен только для обратной совместимости и будет удалён в следующей версии.
/// </summary>

[Obsolete("Use RemoteAchievementLoader instead. CSV-based loading is deprecated.", false)]
public class AchievementManager : MonoBehaviour
{
    //[Header("Настройки списка")]
    //[SerializeField] private Transform contentPanel; // Scroll View Content
    //[SerializeField] private GameObject itemPrefab;
    ////[SerializeField] private GameObject unlockedPrefab;

    //[Header("Настройки файла")]
    //[SerializeField] private string fileName = "achievements.csv";
    //[SerializeField] private TextAsset textAsset = null;
    //[SerializeField] private char delimiter = ';';

    //private List<AchievementData> achievementsList = new List<AchievementData>();

    //// Структура данных
    //public struct AchievementData
    //{
    //    public string id;
    //    public string name;
    //    public string description;
    //    public bool isUnlocked; // true если в файле написано "unlocked"
    //    public int progress;
    //    public float rarity;
    //    public string date;
    //    public string time;
    //    public string iconSpriteName;
    //}

    void Awake()
    {
        Debug.LogWarning("[AchievementManager] ⚠️ Этот класс устарел! Используйте RemoteAchievementLoader для загрузки ачивок через CDN.");
        Destroy(gameObject); // Автоматически удаляем, чтобы не мешал
    }

    // Пустые методы-заглушки, чтобы не ломать ссылки в инспекторе при переходе
    public void LoadAndBuildList() { }
    public void BuildUI() { }

    //void Start()
    //{
    //    LoadAndBuildList();
    //}

    //void LoadAndBuildList()
    //{
    //    // 1. Загрузка файла (из Resources или StreamingAssets)
    //    // Для простоты примера берем из Resources/Text/
    //    //TextAsset textAsset = Resources.Load<TextAsset>("Text/" + fileName);

    //    if (textAsset == null)
    //    {
    //        Debug.LogError($"Файл {fileName} не найден в папке Resources/Text/");
    //        return;
    //    }

    //    string[] rows = textAsset.text.Split('\n');
    //    bool isHeader = true;

    //    foreach (string row in rows)
    //    {
    //        if (isHeader) { isHeader = false; continue; } // Пропускаем заголовок
    //        if (string.IsNullOrWhiteSpace(row)) continue;

    //        // 2. Парсинг строки
    //        string[] values = row.Split(delimiter);

    //        // Проверка на корректность данных (защита от краша)
    //        if (values.Length < 7) continue;

    //        AchievementData data = new AchievementData
    //        {
    //            id = values[0],
    //            name = values[1],
    //            description = values[2],
    //            isUnlocked = values[3].Trim().ToLower() == "unlocked",
    //            progress = int.TryParse(values[4], out int p) ? p : 0,
    //            rarity = float.TryParse(values[5], out float r) ? r : 0,
    //            date = values.Length > 6 ? values[6] : "",
    //            time = values.Length > 7 ? values[7] : "",
    //            iconSpriteName = values.Length > 8 ? values[8] : ""
    //        };

    //        achievementsList.Add(data);
    //    }

    //    // 3. Сборка UI
    //    BuildUI();
    //}

    //void BuildUI()
    //{
    //    // Очистка старого контента
    //    foreach (Transform child in contentPanel)
    //    {
    //        Destroy(child.gameObject);
    //    }

    //    foreach (var data in achievementsList)
    //    {
    //        // Выбираем нужный префаб
    //        GameObject prefab = itemPrefab;//data.isUnlocked ? unlockedPrefab : lockedPrefab;
    //        GameObject item = Instantiate(prefab, contentPanel);

    //        // Заполняем данными
    //        // Предполагается, что у префаба есть скрипт AchievementUIItem (см. ниже)
    //        AchievementUIItem uiItem = item.GetComponent<AchievementUIItem>();
    //        if (uiItem != null)
    //        {
    //            uiItem.SetData(data);
    //        }
    //    }
    //}
}