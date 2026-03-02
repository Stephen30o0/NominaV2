using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nomina
{
    /// <summary>
    /// Manages supported languages and current language selection.
    /// </summary>
    public class LanguageManager : MonoBehaviour
    {
        [Serializable]
        public class Language
        {
            public string displayName;
            public string code;       // ISO 639-1 (e.g. "es", "fr", "pt")
            public string nativeName; // e.g. "Español"
        }

        // NOT [SerializeField] — must always use code-defined list, not stale scene data
        private List<Language> supportedLanguages = new List<Language>();

        private int currentLanguageIndex = 0;

        public Language CurrentLanguage => supportedLanguages[currentLanguageIndex];
        public string CurrentLanguageCode => supportedLanguages[currentLanguageIndex].code;
        public string CurrentLanguageName => supportedLanguages[currentLanguageIndex].displayName;
        public List<Language> SupportedLanguages => supportedLanguages;

        public event Action<Language> OnLanguageChanged;

        private void Awake()
        {
            // Always initialize from code (not serialized scene data)
            InitializeLanguages();

            // Load saved language preference
            string savedCode = PlayerPrefs.GetString("SelectedLanguage", "es");
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                if (supportedLanguages[i].code == savedCode)
                {
                    currentLanguageIndex = i;
                    break;
                }
            }
        }

        private void InitializeLanguages()
        {
            supportedLanguages.Clear();
            supportedLanguages.AddRange(new Language[]
            {
                new Language { displayName = "Spanish", code = "es", nativeName = "Español" },
                new Language { displayName = "French", code = "fr", nativeName = "Français" },
                new Language { displayName = "Portuguese", code = "pt", nativeName = "Português" },
                new Language { displayName = "German", code = "de", nativeName = "Deutsch" },
                new Language { displayName = "Italian", code = "it", nativeName = "Italiano" },
                new Language { displayName = "Yoruba", code = "yo", nativeName = "Yoruba" },
                new Language { displayName = "Igbo", code = "ig", nativeName = "Igbo" },
                new Language { displayName = "Swahili", code = "sw", nativeName = "Kiswahili" },
                new Language { displayName = "Hausa", code = "ha", nativeName = "Hausa" },
                new Language { displayName = "Zulu", code = "zu", nativeName = "isiZulu" },
                new Language { displayName = "Arabic", code = "ar", nativeName = "Al-Arabiyya" },
                new Language { displayName = "Japanese", code = "ja", nativeName = "Nihongo" },
                new Language { displayName = "Korean", code = "ko", nativeName = "Hangugeo" },
                new Language { displayName = "Chinese", code = "zh", nativeName = "Zhongwen" },
                new Language { displayName = "Hindi", code = "hi", nativeName = "Hindi" },
                new Language { displayName = "Russian", code = "ru", nativeName = "Russkiy" },
                new Language { displayName = "Turkish", code = "tr", nativeName = "Türkçe" },
            });
            Debug.Log($"[Nomina] LanguageManager: Initialized {supportedLanguages.Count} languages");
        }

        public void SetLanguage(int index)
        {
            if (index < 0 || index >= supportedLanguages.Count) return;
            currentLanguageIndex = index;
            PlayerPrefs.SetString("SelectedLanguage", CurrentLanguageCode);
            PlayerPrefs.Save();
            OnLanguageChanged?.Invoke(CurrentLanguage);
        }

        public void SetLanguage(string code)
        {
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                if (supportedLanguages[i].code == code)
                {
                    SetLanguage(i);
                    return;
                }
            }
        }
    }
}