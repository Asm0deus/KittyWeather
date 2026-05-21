using UnityEngine;
using Firebase;
using Firebase.Auth;
using System.Threading.Tasks;

public class FirebaseAuthManager : MonoBehaviour
{
    public static FirebaseAuthManager Instance { get; private set; }
    public static FirebaseAuth Auth { get; private set; }
    public static string UserId { get; internal set; }
    public static bool IsReady { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // ⛔ НЕ запускаем инициализацию в Awake! Ждём команды от GameBootstrap
    }

    /// <summary>
    /// Явная инициализация Firebase Auth. Вызывается из GameBootstrap.
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        if (IsReady) return true;

        Debug.Log("[Auth] 1/3 Проверка зависимостей Firebase...");
        var dependencyTask = FirebaseApp.CheckAndFixDependenciesAsync();
        await dependencyTask;

        if (dependencyTask.Result != DependencyStatus.Available)
        {
            Debug.LogError($"[Auth] ❌ Ошибка зависимостей: {dependencyTask.Result}");
            return false;
        }

        Debug.Log("[Auth] 2/3 Firebase готов. Выполняю вход...");
        Auth = FirebaseAuth.DefaultInstance;

        // Вход или восстановление сессии
        if (Auth.CurrentUser != null)
        {
            UserId = Auth.CurrentUser.UserId;
            Debug.Log($"[Auth] ✅ Сессия восстановлена. UID: {UserId}");
        }
        else
        {
            var result = await Auth.SignInAnonymouslyAsync();
            if (result?.User != null)
            {
                UserId = result.User.UserId;
                Debug.Log($"[Auth] ✅ Успешный вход. UID: {UserId}");
            }
            else
            {
                Debug.LogError("[Auth] ❌ Ошибка анонимного входа");
                return false;
            }
        }

        IsReady = true;
        Debug.Log("[Auth] 3/3 Готово!");
        return true;
    }

    // Безопасный сеттер для тестового режима из Bootstrap
    internal static void SetMockUserIdForEditor(string mockId) => UserId = mockId;
}