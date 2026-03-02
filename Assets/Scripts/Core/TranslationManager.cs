using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Nomina
{
    /// <summary>
    /// Handles translation of detected object names using a translation API.
    /// Supports Google Cloud Translate and LibreTranslate as backends.
    /// Includes a local cache to minimize API calls.
    /// </summary>
    public class TranslationManager : MonoBehaviour
    {
        public enum TranslationBackend
        {
            MyMemory,     // Free, no API key needed (1000 words/day)
            GoogleCloud,
            LibreTranslate,
            Offline       // Uses local prefix fallback
        }

        [Header("Settings")]
        [SerializeField] private TranslationBackend backend = TranslationBackend.MyMemory;
        [SerializeField] private string googleApiKey = ""; // Set in Inspector or via config
        [SerializeField] private string libreTranslateUrl = "https://libretranslate.com/translate";

        [Header("Cache")]
        [SerializeField] private int maxCacheSize = 500;

        private void Awake()
        {
            // Force MyMemory if Google API key isn't configured
            // (Inspector serialized value may be stale)
            if (backend == TranslationBackend.GoogleCloud && string.IsNullOrEmpty(googleApiKey))
            {
                Debug.Log("[Nomina] No Google Translate API key — falling back to MyMemory");
                backend = TranslationBackend.MyMemory;
            }
        }

        // Cache: key = "word|sourceLang|targetLang", value = translation
        private System.Collections.Generic.Dictionary<string, string> translationCache =
            new System.Collections.Generic.Dictionary<string, string>();

        /// <summary>
        /// Translate a word from source language to target language.
        /// </summary>
        public void Translate(string text, string sourceLang, string targetLang, Action<string> onComplete)
        {
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke(null);
                return;
            }

            // Check cache first
            string cacheKey = $"{text.ToLower()}|{sourceLang}|{targetLang}";
            if (translationCache.TryGetValue(cacheKey, out string cached))
            {
                onComplete?.Invoke(cached);
                return;
            }

            switch (backend)
            {
                case TranslationBackend.MyMemory:
                    StartCoroutine(MyMemoryTranslate(text, sourceLang, targetLang, cacheKey, onComplete));
                    break;
                case TranslationBackend.GoogleCloud:
                    StartCoroutine(GoogleTranslate(text, sourceLang, targetLang, cacheKey, onComplete));
                    break;
                case TranslationBackend.LibreTranslate:
                    StartCoroutine(LibreTranslateRequest(text, sourceLang, targetLang, cacheKey, onComplete));
                    break;
                case TranslationBackend.Offline:
                    // Fallback: return the original word with a note
                    string fallback = $"[{targetLang}] {text}";
                    CacheTranslation(cacheKey, fallback);
                    onComplete?.Invoke(fallback);
                    break;
            }
        }

        private IEnumerator GoogleTranslate(string text, string sourceLang, string targetLang,
            string cacheKey, Action<string> onComplete)
        {
            string url = $"https://translation.googleapis.com/language/translate/v2" +
                         $"?key={googleApiKey}" +
                         $"&q={UnityWebRequest.EscapeURL(text)}" +
                         $"&source={sourceLang}" +
                         $"&target={targetLang}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<GoogleTranslateResponse>(request.downloadHandler.text);
                        string translation = response.data.translations[0].translatedText;
                        CacheTranslation(cacheKey, translation);
                        onComplete?.Invoke(translation);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Nomina] Google Translate parse error: {e.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Nomina] Google Translate error: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// MyMemory API: free translation up to 1000 words/day, no API key required.
        /// </summary>
        private IEnumerator MyMemoryTranslate(string text, string sourceLang, string targetLang,
            string cacheKey, Action<string> onComplete)
        {
            string langpair = $"{sourceLang}|{targetLang}";
            string url = $"https://api.mymemory.translated.net/get?q={UnityWebRequest.EscapeURL(text)}&langpair={UnityWebRequest.EscapeURL(langpair)}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<MyMemoryResponse>(request.downloadHandler.text);
                        if (response.responseStatus == 200 &&
                            !string.IsNullOrEmpty(response.responseData?.translatedText))
                        {
                            string translation = response.responseData.translatedText;
                            CacheTranslation(cacheKey, translation);
                            onComplete?.Invoke(translation);
                        }
                        else
                        {
                            Debug.LogWarning($"[Nomina] MyMemory returned status {response.responseStatus}");
                            onComplete?.Invoke(null);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Nomina] MyMemory parse error: {e.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Nomina] MyMemory error: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        private IEnumerator LibreTranslateRequest(string text, string sourceLang, string targetLang,
            string cacheKey, Action<string> onComplete)
        {
            var form = new WWWForm();
            form.AddField("q", text);
            form.AddField("source", sourceLang);
            form.AddField("target", targetLang);
            form.AddField("format", "text");

            using (UnityWebRequest request = UnityWebRequest.Post(libreTranslateUrl, form))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LibreTranslateResponse>(request.downloadHandler.text);
                        CacheTranslation(cacheKey, response.translatedText);
                        onComplete?.Invoke(response.translatedText);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Nomina] LibreTranslate parse error: {e.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[Nomina] LibreTranslate error: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

        private void CacheTranslation(string key, string value)
        {
            if (translationCache.Count >= maxCacheSize)
            {
                translationCache.Clear(); // Simple eviction
            }
            translationCache[key] = value;
        }

        public void ClearCache()
        {
            translationCache.Clear();
        }

        // JSON response classes
        [Serializable]
        private class GoogleTranslateResponse
        {
            public GoogleTranslateData data;
        }

        [Serializable]
        private class GoogleTranslateData
        {
            public GoogleTranslation[] translations;
        }

        [Serializable]
        private class GoogleTranslation
        {
            public string translatedText;
        }

        [Serializable]
        private class LibreTranslateResponse
        {
            public string translatedText;
        }

        [Serializable]
        private class MyMemoryResponse
        {
            public int responseStatus;
            public MyMemoryResponseData responseData;
        }

        [Serializable]
        private class MyMemoryResponseData
        {
            public string translatedText;
        }
    }
}