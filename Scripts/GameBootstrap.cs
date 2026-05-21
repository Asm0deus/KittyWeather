// version 1.0.0
using UnityEngine;
using System;
using System.Threading.Tasks;

public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }
    public static event Action OnAllSystemsReady;
    public static bool IsReady { get; private set; } = false;

    [Header("Настройки")]
    [SerializeField] private bool skipFirebaseInEditor = true;

    [Header("Менеджеры (назначь в инспекторе!)")]
    [SerializeField] private EconomyConfigLoader configLoader;
    [SerializeField] private FirebaseAuthManager authManager;
    [SerializeField] private FirestoreManager firestoreManager;
    [SerializeField] private CurrencyManager currencyManager;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RunInitialization();
    }

    private async void RunInitialization()
    {
        Debug.Log("[Bootstrap] 🚀 Запуск последовательности инициализации...");

        // 1. Ждём конфиг
        if (!await WaitForConfigLoadAsync()) return;

        // Инициализация LocalizationManager
        //LocalizationManager.Init();

        // 2. Firebase Auth
        bool useFirebase = !skipFirebaseInEditor || Application.isMobilePlatform;
        if (useFirebase)
        {
            if (authManager == null) authManager = FindObjectOfType<FirebaseAuthManager>();
            if (authManager == null) { Debug.LogError("[Bootstrap] ❌ FirebaseAuthManager не найден!"); return; }

            bool authOk = await authManager.InitializeAsync();
            if (!authOk) return;
        }
        else
        {
            Debug.LogWarning("[Bootstrap] ⚠️ Firebase пропущен (Editor Mode)");
            FirebaseAuthManager.SetMockUserIdForEditor("editor_test_" + UnityEngine.Random.Range(1000, 9999));
        }

        // 3. Инициализация Firestore
        if (firestoreManager == null) firestoreManager = FindObjectOfType<FirestoreManager>();
        if (firestoreManager != null && !string.IsNullOrEmpty(FirebaseAuthManager.UserId))
        {
            Debug.Log("[Bootstrap] 📡 Инициализация Firestore...");
            firestoreManager.Initialize(FirebaseAuthManager.UserId);

            // ⏳ ДАЁМ FIRESTORE 1.5 СЕКУНДЫ НА УСТАНОВКУ СОЕДИНЕНИЯ
            await Task.Delay(1500);
        }
        else
        {
            Debug.LogWarning("[Bootstrap] ⚠️ FirestoreManager не найден или UserId пуст.");
        }

        // 4. Синхронизация данных (теперь Firestore готов)
        if (currencyManager != null && !string.IsNullOrEmpty(FirebaseAuthManager.UserId))
        {
            Debug.Log("[Bootstrap] 🔄 Синхронизация баланса...");
            currencyManager.SyncFromCloud(FirebaseAuthManager.UserId);
            await Task.Delay(300);
        }

        // 5. ВСЁ ГОТОВО
        IsReady = true;
        Debug.Log("[Bootstrap] ✅ Все системы готовы!");
        OnAllSystemsReady?.Invoke();
    }

    private async Task<bool> WaitForConfigLoadAsync()
    {
        if (EconomyConfigLoader.Current != null) return true;

        float timer = 0f;
        while (EconomyConfigLoader.Current == null && timer < 15f) // Увеличен таймаут до 15с
        {
            await Task.Delay(100);
            timer += 0.1f;
        }

        if (EconomyConfigLoader.Current == null)
        {
            Debug.LogError("[Bootstrap] ❌ Не удалось загрузить EconomyConfig за 15 сек");
            return false;
        }
        Debug.Log($"[Bootstrap] ✅ Конфиг загружен: версия {EconomyConfigLoader.Current.version}");
        return true;
    }

    // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ДРУГИХ СКРИПТОВ ===
    public static async Task WaitForReadyAsync()
    {
        if (IsReady) return;
        int timeout = 0;
        while (!IsReady && timeout < 150) { await Task.Delay(100); timeout++; }
        if (!IsReady) Debug.LogWarning("[Bootstrap] Timeout waiting for readiness");
    }

    public static bool CanUseManagers() => IsReady;

    public void Reinitialize()
    {
        if (IsReady) return;
        RunInitialization();
    }
}