using Firebase.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance { get; private set; }

    private FirebaseFirestore db;
    private DocumentReference playerDoc;

    public DocumentReference PlayerDoc => playerDoc;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(string userId)
    {
        if (string.IsNullOrEmpty(userId)) { Debug.LogError("[Firestore] Пустой userId"); return; }

        try
        {
            db = FirebaseFirestore.DefaultInstance;
            playerDoc = db.Collection("players").Document(userId);
            Debug.Log($"[Firestore] ✅ Привязка к players/{userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] Ошибка инициализации: {e.Message}");
        }
    }

    public async Task<Dictionary<string, AchievementProgress>> GetAchievementsProgressAsync()
    {
        if (playerDoc == null) return null;

        try
        {
            var snapshot = await playerDoc.Collection("achievements").GetSnapshotAsync();
            var result = new Dictionary<string, AchievementProgress>();

            foreach (var doc in snapshot.Documents)
            {
                result[doc.Id] = doc.ConvertTo<AchievementProgress>();
            }
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] Ошибка загрузки прогресса ачивок: {e.Message}");
            return null;
        }
    }


    /// <summary>
    /// Загрузка данных с автоматическими повторными попытками при оффлайне
    /// </summary>
    public async Task<PlayerData> LoadAsync(int maxRetries = 3)
    {
        if (playerDoc == null) return new PlayerData();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Debug.Log($"[Firestore] 🔄 Загрузка данных (попытка {attempt}/{maxRetries})...");
                var snapshot = await playerDoc.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    // ✅ Конвертируем Dictionary из Firestore в PlayerData
                    var map = snapshot.ToDictionary();
                    var data = PlayerData.FromFirestoreMap(map);

                    Debug.Log($"[Firestore] ✅ Данные найдены: {data.coins}🐟 | {data.gems}💎");
                    return data;


                    //var data = snapshot.ConvertTo<PlayerData>();
                    //Debug.Log($"[Firestore] ✅ Данные найдены: {data.coins}🐟 | {data.gems}💎");
                    //return data;
                }

                Debug.Log("[Firestore] 🆕 Новый профиль. Возвращаю стартовые 500 монет.");
                return new PlayerData { coins = 500, gems = 0 };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Firestore] ⚠️ Попытка {attempt} failed: {e.Message}");

                if (attempt < maxRetries)
                {
                    // Экспоненциальная задержка: 1с, 2с, 3с...
                    await Task.Delay(1000 * attempt);
                }
                else
                {
                    Debug.LogError($"[Firestore] ❌ Не удалось загрузить данные после {maxRetries} попыток.");
                    return new PlayerData(); // Вернёт пустые данные, CurrencyManager подхватит PlayerPrefs
                }
            }
        }
        return new PlayerData();
    }

    public async Task SaveAsync(PlayerData data)
    {
        if (playerDoc == null || data == null) return;
        try
        {
            // Конвертируем PlayerData в словарь, который понимает Firestore
            var firestoreMap = data.ToFirestoreMap();
            await playerDoc.SetAsync(firestoreMap, SetOptions.MergeAll);
            Debug.Log("[Firestore] 💾 Данные сохранены.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Firestore] Ошибка сохранения: {e.Message}");
        }
    }

    /// <summary>
    /// Загрузка прогресса ачивок из поля 'achievements' в документе игрока
    /// </summary>
    public async Task<Dictionary<string, AchievementProgress>> LoadAchievementProgressAsync()
    {
        if (playerDoc == null) return null;
        try
        {
            var snapshot = await playerDoc.GetSnapshotAsync();
            if (!snapshot.Exists) return null;

            // ✅ FIX: Явно указываем тип для TryGetValue
            if (snapshot.TryGetValue<Dictionary<string, object>>("achievements", out var rawAchievements) && rawAchievements != null)
            {
                var result = new Dictionary<string, AchievementProgress>();
                foreach (var kvp in rawAchievements)
                {
                    if (kvp.Value is Dictionary<string, object> progDict)
                    {
                        // Конвертируем Dictionary -> JSON -> AchievementProgress
                        var json = Newtonsoft.Json.JsonConvert.SerializeObject(progDict);
                        result[kvp.Key] = Newtonsoft.Json.JsonConvert.DeserializeObject<AchievementProgress>(json);
                    }
                }
                return result;
            }
            return new Dictionary<string, AchievementProgress>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firestore] Ошибка загрузки ачивок: {e.Message}");
            return null;
        }
    }


    /// <summary>
    /// Сохранение прогресса ачивок в поле 'achievements'
    /// </summary>
    public async Task SaveAchievementProgressAsync(Dictionary<string, AchievementProgress> progress)
    {


        if (playerDoc == null || progress == null) return;
        try
        {
            // Конвертируем AchievementProgress -> Dictionary<string, object> для Firestore
            var firestoreMap = new Dictionary<string, object>();
            foreach (var kvp in progress)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(kvp.Value);
                firestoreMap[kvp.Key] = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            }

            await playerDoc.UpdateAsync("achievements", firestoreMap);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firestore] Ошибка сохранения ачивок: {e.Message}");
        }
    }
}