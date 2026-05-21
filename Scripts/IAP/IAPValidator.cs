// IAPValidator.cs
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Обёртка для серверной валидации покупок через Cloud Functions.
/// </summary>
public static class IAPValidator
{
    // URL твоей Cloud Function (замени на реальный после деплоя)
    private const string VALIDATE_URL = "https://us-central1-<PROJECT_ID>.cloudfunctions.net/validateIAP";

    [Serializable]
    private class ValidationRequest
    {
        public string userId;
        public string productId;
        public string receipt;
        public string platform; // "google_play", "app_store", etc.
    }

    [Serializable]
    public class ValidationResponse
    {
        public bool success;
        public string message;
        //public int grantedCoins;
        //public int grantedGems;
        //public string[] grantedItems;

        public RewardData granted;
        public string transactionId;
    }

    [Serializable]
    public class RewardData
    {
        public string type; // "coins" или "gems"
        public int amount;
    }

    /// <summary>
    /// Валидирует покупку на сервере и возвращает результат.
    /// </summary>
    public static async Task<ValidationResponse> ValidateAsync(string productId, string receipt)
    {
        var request = new ValidationRequest
        {
            userId = FirebaseAuthManager.UserId,
            productId = productId,
            receipt = receipt,
            platform = GetPlatformString()
        };

        // Используем JsonConvert вместо JsonUtility
        string json = JsonConvert.SerializeObject(request);
        //string json = JsonUtility.ToJson(request);

        using (var req = UnityWebRequest.PostWwwForm(VALIDATE_URL, json))
        {
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 30;

            var operation = req.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[IAP Validator] HTTP error: {req.error}");
                return new ValidationResponse { success = false, message = req.error };
            }

            try
            {
                // Используем JsonConvert для парсинга ответа
                var response = JsonConvert.DeserializeObject<ValidationResponse>(req.downloadHandler.text);
                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[IAP Validator] Parse error: {e.Message}");
                return new ValidationResponse { success = false, message = "Parse error" };
            }
        }
    }


    private static string GetPlatformString()
    {
#if UNITY_ANDROID
        return "google_play";
#elif UNITY_IOS
            return "app_store";
#elif UNITY_EDITOR
            return "google_play"; // Для тестов
#else
            return "unknown";
#endif
    }
}