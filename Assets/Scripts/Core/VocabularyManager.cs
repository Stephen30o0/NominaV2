using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nomina
{
    /// <summary>
    /// Manages the saved vocabulary (words the user wants to remember).
    /// Persists data using PlayerPrefs/JSON.
    /// </summary>
    public class VocabularyManager : MonoBehaviour
    {
        [Serializable]
        public class VocabEntry
        {
            public string originalWord;
            public string translatedWord;
            public string languageCode;
            public string languageName;
            public string dateAdded;
            public string id;
        }

        [Serializable]
        private class VocabData
        {
            public List<VocabEntry> entries = new List<VocabEntry>();
        }

        private VocabData data = new VocabData();
        private const string SAVE_KEY = "Nomina_Vocabulary";

        public List<VocabEntry> Entries => data.entries;
        public int Count => data.entries.Count;

        public event Action<VocabEntry> OnWordSaved;
        public event Action<VocabEntry> OnWordRemoved;
        public event Action OnVocabularyCleared;

        private void Awake()
        {
            LoadVocabulary();
        }

        public void SaveWord(string original, string translated, string languageCode)
        {
            // Check for duplicates
            foreach (var entry in data.entries)
            {
                if (entry.originalWord.Equals(original, StringComparison.OrdinalIgnoreCase) &&
                    entry.languageCode == languageCode)
                {
                    Debug.Log($"[Nomina] Word already saved: {original} ({languageCode})");
                    return;
                }
            }

            var newEntry = new VocabEntry
            {
                originalWord = original,
                translatedWord = translated,
                languageCode = languageCode,
                languageName = GetLanguageName(languageCode),
                dateAdded = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                id = Guid.NewGuid().ToString()
            };

            data.entries.Insert(0, newEntry); // Add to front (newest first)
            SaveVocabulary();
            OnWordSaved?.Invoke(newEntry);

            Debug.Log($"[Nomina] Saved word: {original} -> {translated} ({languageCode})");
        }

        public void RemoveWord(string id)
        {
            var entry = data.entries.Find(e => e.id == id);
            if (entry != null)
            {
                data.entries.Remove(entry);
                SaveVocabulary();
                OnWordRemoved?.Invoke(entry);
            }
        }

        public void ClearAll()
        {
            data.entries.Clear();
            SaveVocabulary();
            OnVocabularyCleared?.Invoke();
        }

        public List<VocabEntry> GetEntriesByLanguage(string languageCode)
        {
            return data.entries.FindAll(e => e.languageCode == languageCode);
        }

        public List<VocabEntry> GetEntriesSorted(bool newestFirst = true)
        {
            var sorted = new List<VocabEntry>(data.entries);
            if (newestFirst)
                sorted.Sort((a, b) => string.Compare(b.dateAdded, a.dateAdded, StringComparison.Ordinal));
            else
                sorted.Sort((a, b) => string.Compare(a.dateAdded, b.dateAdded, StringComparison.Ordinal));
            return sorted;
        }

        private void SaveVocabulary()
        {
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadVocabulary()
        {
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                data = JsonUtility.FromJson<VocabData>(json);
                if (data == null)
                    data = new VocabData();
            }
        }

        private string GetLanguageName(string code)
        {
            if (AppManager.Instance?.LanguageManager != null)
            {
                foreach (var lang in AppManager.Instance.LanguageManager.SupportedLanguages)
                {
                    if (lang.code == code) return lang.displayName;
                }
            }
            return code;
        }
    }
}