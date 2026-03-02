using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Nomina
{
    /// <summary>
    /// Text-to-speech using Azure Cognitive Services Neural Voices.
    /// Produces natural, native pronunciation for 15+ languages.
    /// Falls back to Android device TTS when offline or for unsupported languages.
    /// </summary>
    public class TTSManager : MonoBehaviour
    {
        private string speechKey;
        private string speechRegion;
        private string huggingFaceToken;
        private AudioSource audioSource;
        private bool isSpeaking = false;

        // Azure Neural Voice mapping — best female voice per language
        private static readonly Dictionary<string, string> azureVoiceMap = new Dictionary<string, string>()
        {
            { "es", "es-ES-ElviraNeural" },
            { "fr", "fr-FR-DeniseNeural" },
            { "pt", "pt-BR-FranciscaNeural" },
            { "de", "de-DE-KatjaNeural" },
            { "it", "it-IT-ElsaNeural" },
            { "sw", "sw-KE-ZuriNeural" },
            { "zu", "zu-ZA-ThandoNeural" },
            { "ar", "ar-SA-ZariyahNeural" },
            { "ja", "ja-JP-NanamiNeural" },
            { "ko", "ko-KR-SunHiNeural" },
            { "zh", "zh-CN-XiaoxiaoNeural" },
            { "hi", "hi-IN-SwaraNeural" },
            { "ru", "ru-RU-SvetlanaNeural" },
            { "tr", "tr-TR-EmelNeural" },
        };

        // Languages that use Meta MMS TTS (native speaker quality)
        // Yoruba: dedicated Yurikks HF Space
        // Hausa: dpc/mmstts multi-language Gradio Space
        private static readonly HashSet<string> metaMmsLanguages = new HashSet<string>()
        {
            "yo", "ha"
        };

        private const string YORUBA_TTS_URL = "https://yurikks-yoruba-tts.hf.space/tts";
        private const string GRADIO_MMS_URL = "https://dpc-mmstts.hf.space";

        // MMS language labels for Gradio Space (ISO 639-3 format)
        private static readonly Dictionary<string, string> mmsGradioLabels = new Dictionary<string, string>()
        {
            { "ha", "Hausa (hau)" },
        };

        // BCP 47 locale tags for Android TTS fallback
        private static readonly Dictionary<string, string> localeMap = new Dictionary<string, string>()
        {
            { "es", "es-ES" }, { "fr", "fr-FR" }, { "pt", "pt-BR" },
            { "de", "de-DE" }, { "it", "it-IT" }, { "yo", "yo-NG" },
            { "ig", "ig-NG" }, { "sw", "sw-KE" }, { "ha", "ha-NG" },
            { "zu", "zu-ZA" }, { "ar", "ar-SA" }, { "ja", "ja-JP" },
            { "ko", "ko-KR" }, { "zh", "zh-CN" }, { "hi", "hi-IN" },
            { "ru", "ru-RU" }, { "tr", "tr-TR" },
        };

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject ttsObject;
        private AndroidJavaObject activity;
        private bool androidTtsReady = false;
#endif

        private void Start()
        {
            // Create AudioSource for playing Azure TTS audio
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            LoadConfig();
            InitializeAndroidTTSFallback();
        }

        private void LoadConfig()
        {
            try
            {
                var configAsset = Resources.Load<TextAsset>("api_config");
                if (configAsset != null)
                {
                    var config = JsonUtility.FromJson<SpeechConfig>(configAsset.text);
                    speechKey = config.azureSpeechKey;
                    speechRegion = config.azureSpeechRegion;
                    huggingFaceToken = config.huggingFaceToken;

                    if (!string.IsNullOrEmpty(speechKey) && !string.IsNullOrEmpty(speechRegion))
                    {
                        Debug.Log($"[Nomina] Azure Speech configured: region={speechRegion}, voices={azureVoiceMap.Count}");
                    }

                    if (!string.IsNullOrEmpty(huggingFaceToken))
                    {
                        Debug.Log($"[Nomina] Meta MMS configured: {metaMmsLanguages.Count} African language models");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nomina] Failed to load speech config: {e.Message}");
            }
        }

        /// <summary>
        /// Speak text in the given language. Uses Azure Neural TTS if available,
        /// falls back to Android device TTS.
        /// </summary>
        public void Speak(string text, string languageCode)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (isSpeaking) Stop();

            Debug.Log($"[Nomina] TTS: '{text}' in {languageCode}");

            // Use Meta MMS for African languages without Azure voices
            if (metaMmsLanguages.Contains(languageCode) && !string.IsNullOrEmpty(huggingFaceToken))
            {
                if (languageCode == "yo")
                    StartCoroutine(SpeakYorubaMMS(text));
                else if (mmsGradioLabels.ContainsKey(languageCode))
                    StartCoroutine(SpeakGradioMMS(text, languageCode));
                else
                    SpeakAndroid(text, languageCode);
            }
            // Use Azure Neural TTS for all other supported languages
            else if (!string.IsNullOrEmpty(speechKey) && azureVoiceMap.ContainsKey(languageCode))
            {
                StartCoroutine(SpeakAzure(text, languageCode));
            }
            else
            {
                // Last resort: Android device TTS
                Debug.Log($"[Nomina] TTS: No cloud voice for '{languageCode}', using Android TTS");
                SpeakAndroid(text, languageCode);
            }
        }

        /// <summary>
        /// Meta MMS Yoruba TTS via dedicated Yurikks HF Space.
        /// Returns base64-encoded WAV audio with native Yoruba pronunciation.
        /// </summary>
        private IEnumerator SpeakYorubaMMS(string text)
        {
            isSpeaking = true;

            string jsonBody = $"{{\"text\": \"{EscapeJsonString(text)}\", \"speed\": 0.9}}";
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(YORUBA_TTS_URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Authorization", $"Bearer {huggingFaceToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Response is JSON with base64-encoded audio
                    string responseJson = request.downloadHandler.text;
                    var response = JsonUtility.FromJson<YorubaTTSResponse>(responseJson);

                    if (!string.IsNullOrEmpty(response.audio))
                    {
                        byte[] audioData = Convert.FromBase64String(response.audio);
                        Debug.Log($"[Nomina] Meta MMS: Got {audioData.Length} bytes for '{text}' (cached={response.cached})");

                        AudioClip clip = WavToAudioClip(audioData);
                        if (clip != null)
                        {
                            audioSource.clip = clip;
                            audioSource.Play();
                            Debug.Log($"[Nomina] Meta MMS: Playing '{text}' in Yoruba ({clip.length:F1}s)");
                        }
                        else
                        {
                            Debug.LogWarning("[Nomina] Meta MMS: Failed to decode audio, trying Android TTS");
                            SpeakAndroid(text, "yo");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[Nomina] Meta MMS Yoruba: Empty audio response, trying Android TTS");
                        SpeakAndroid(text, "yo");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Nomina] Meta MMS Yoruba failed ({request.responseCode}): {request.error} — using Android TTS");
                    SpeakAndroid(text, "yo");
                }
            }

            isSpeaking = false;
        }

        /// <summary>
        /// Meta MMS TTS via dpc/mmstts Gradio Space.
        /// Supports Hausa and other languages via the multi-language MMS model.
        /// Two-step: POST predict → GET file download.
        /// </summary>
        private IEnumerator SpeakGradioMMS(string text, string languageCode)
        {
            isSpeaking = true;
            string langLabel = mmsGradioLabels[languageCode];

            // Step 1: Call Gradio predict endpoint
            string jsonBody = $"{{\"data\": [\"{EscapeJsonString(text)}\", \"{langLabel}\"]}}";
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            string filePath = null;

            using (var request = new UnityWebRequest($"{GRADIO_MMS_URL}/api/predict", "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Authorization", $"Bearer {huggingFaceToken}");
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    // Parse Gradio response to get file path
                    var response = JsonUtility.FromJson<GradioResponse>(request.downloadHandler.text);
                    if (response.data != null && response.data.Length > 0 && !string.IsNullOrEmpty(response.data[0].name))
                    {
                        filePath = response.data[0].name;
                        Debug.Log($"[Nomina] Gradio MMS: predict OK, file={filePath}");
                    }
                    else
                    {
                        Debug.LogWarning("[Nomina] Gradio MMS: No file in response");
                    }
                }
                else
                {
                    Debug.LogWarning($"[Nomina] Gradio MMS predict failed ({request.responseCode}): {request.error}");
                }
            }

            // Step 2: Download the WAV file
            if (!string.IsNullOrEmpty(filePath))
            {
                string fileUrl = $"{GRADIO_MMS_URL}/file={filePath}";
                using (var fileReq = UnityWebRequest.Get(fileUrl))
                {
                    fileReq.SetRequestHeader("Authorization", $"Bearer {huggingFaceToken}");
                    fileReq.timeout = 15;

                    yield return fileReq.SendWebRequest();

                    if (fileReq.result == UnityWebRequest.Result.Success)
                    {
                        byte[] audioData = fileReq.downloadHandler.data;
                        Debug.Log($"[Nomina] Gradio MMS: Downloaded {audioData.Length} bytes for '{text}' ({langLabel})");

                        AudioClip clip = WavToAudioClip(audioData);
                        if (clip != null)
                        {
                            audioSource.clip = clip;
                            audioSource.Play();
                            Debug.Log($"[Nomina] Gradio MMS: Playing '{text}' in {languageCode} ({clip.length:F1}s)");
                        }
                        else
                        {
                            Debug.LogWarning("[Nomina] Gradio MMS: Failed to decode WAV, trying Android TTS");
                            SpeakAndroid(text, languageCode);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[Nomina] Gradio MMS file download failed ({fileReq.responseCode}): {fileReq.error}");
                        SpeakAndroid(text, languageCode);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[Nomina] Gradio MMS: No file path, falling back to Android TTS");
                SpeakAndroid(text, languageCode);
            }

            isSpeaking = false;
        }

        private string EscapeJsonString(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }

        /// <summary>
        /// Azure Cognitive Services Neural TTS via REST API.
        /// Returns natural-sounding audio with correct native pronunciation.
        /// </summary>
        private IEnumerator SpeakAzure(string text, string languageCode)
        {
            isSpeaking = true;
            string voiceName = azureVoiceMap[languageCode];

            // Extract locale from voice name (e.g. "es-ES" from "es-ES-ElviraNeural")
            string locale = voiceName.Substring(0, 5);

            // Build SSML (Speech Synthesis Markup Language)
            string ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{locale}'>
    <voice name='{voiceName}'>
        <prosody rate='-10%'>{EscapeXml(text)}</prosody>
    </voice>
</speak>";

            string url = $"https://{speechRegion}.tts.speech.microsoft.com/cognitiveservices/v1";

            using (var request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(ssml);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Ocp-Apim-Subscription-Key", speechKey);
                request.SetRequestHeader("Content-Type", "application/ssml+xml");
                request.SetRequestHeader("X-Microsoft-OutputFormat", "riff-16khz-16bit-mono-pcm");
                request.SetRequestHeader("User-Agent", "NominaApp");

                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] audioData = request.downloadHandler.data;
                    Debug.Log($"[Nomina] Azure TTS: Got {audioData.Length} bytes for '{text}' ({voiceName})");

                    AudioClip clip = WavToAudioClip(audioData);
                    if (clip != null)
                    {
                        audioSource.clip = clip;
                        audioSource.Play();
                        Debug.Log($"[Nomina] Azure TTS: Playing ({clip.length:F1}s)");
                    }
                    else
                    {
                        Debug.LogWarning("[Nomina] Azure TTS: Failed to decode WAV, falling back to Android TTS");
                        SpeakAndroid(text, languageCode);
                    }
                }
                else
                {
                    Debug.LogWarning($"[Nomina] Azure TTS failed ({request.responseCode}): {request.error} — using Android TTS");
                    SpeakAndroid(text, languageCode);
                }
            }

            isSpeaking = false;
        }

        /// <summary>
        /// Parse WAV (RIFF) audio data into a Unity AudioClip.
        /// Expects 16-bit mono PCM from Azure TTS.
        /// </summary>
        private AudioClip WavToAudioClip(byte[] wavData)
        {
            try
            {
                if (wavData == null || wavData.Length < 44) return null;

                // Parse WAV header
                int channels = BitConverter.ToInt16(wavData, 22);
                int sampleRate = BitConverter.ToInt32(wavData, 24);
                int bitsPerSample = BitConverter.ToInt16(wavData, 34);

                // Find "data" chunk
                int dataOffset = 12;
                int dataSize = 0;
                while (dataOffset < wavData.Length - 8)
                {
                    string chunkId = System.Text.Encoding.ASCII.GetString(wavData, dataOffset, 4);
                    int chunkSize = BitConverter.ToInt32(wavData, dataOffset + 4);
                    if (chunkId == "data")
                    {
                        dataOffset += 8;
                        dataSize = chunkSize;
                        break;
                    }
                    dataOffset += 8 + chunkSize;
                }

                if (dataSize == 0) return null;

                // Convert 16-bit PCM to float samples
                int sampleCount = dataSize / (bitsPerSample / 8);
                float[] samples = new float[sampleCount];

                for (int i = 0; i < sampleCount; i++)
                {
                    int byteIndex = dataOffset + i * 2;
                    if (byteIndex + 1 >= wavData.Length) break;
                    short sample = BitConverter.ToInt16(wavData, byteIndex);
                    samples[i] = sample / 32768f;
                }

                AudioClip clip = AudioClip.Create("AzureTTS", sampleCount, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nomina] WAV decode error: {e.Message}");
                return null;
            }
        }

        private string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        // ========== Android TTS Fallback ==========

        private void SpeakAndroid(string text, string languageCode)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (ttsObject == null || !androidTtsReady)
            {
                Debug.LogWarning("[Nomina] Android TTS not ready");
                return;
            }

            try
            {
                string bcp47Tag = localeMap.ContainsKey(languageCode) ? localeMap[languageCode] : languageCode;
                using (var localeClass = new AndroidJavaClass("java.util.Locale"))
                {
                    var locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", bcp47Tag);
                    ttsObject.Call<int>("setLanguage", locale);
                    locale.Dispose();
                }
                ttsObject.Call<int>("speak", text, 0, null, "NominaTTS");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nomina] Android TTS error: {e.Message}");
            }
#else
            Debug.Log($"[Nomina] TTS (editor): Would speak '{text}' in {languageCode}");
#endif
        }

        private void InitializeAndroidTTSFallback()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity,
                        new TTSInitListener(this));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nomina] Android TTS init failed: {e.Message}");
            }
#endif
        }

        public bool IsLanguageAvailable(string languageCode)
        {
            return azureVoiceMap.ContainsKey(languageCode) || metaMmsLanguages.Contains(languageCode);
        }

        public void Stop()
        {
            if (audioSource != null && audioSource.isPlaying)
                audioSource.Stop();

#if UNITY_ANDROID && !UNITY_EDITOR
            if (ttsObject != null)
                ttsObject.Call<int>("stop");
#endif
        }

        public void SetSpeechRate(float rate)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (ttsObject != null)
                ttsObject.Call<int>("setSpeechRate", rate);
#endif
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (ttsObject != null)
            {
                ttsObject.Call("shutdown");
                ttsObject.Dispose();
            }
#endif
        }

        // ========== JSON / Listener Classes ==========

        [Serializable]
        private class SpeechConfig
        {
            public string azureSpeechKey;
            public string azureSpeechRegion;
            public string huggingFaceToken;
        }

        [Serializable]
        private class YorubaTTSResponse
        {
            public string audio;
            public bool cached;
            public int remaining_requests;
        }

        [Serializable]
        private class GradioResponse
        {
            public GradioFileData[] data;
        }

        [Serializable]
        private class GradioFileData
        {
            public string name;
            public bool is_file;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class TTSInitListener : AndroidJavaProxy
        {
            private TTSManager manager;

            public TTSInitListener(TTSManager mgr) : base("android.speech.tts.TextToSpeech$OnInitListener")
            {
                manager = mgr;
            }

            void onInit(int status)
            {
                if (status == 0)
                {
                    manager.androidTtsReady = true;
                    Debug.Log("[Nomina] Android TTS fallback ready");
                }
                else
                {
                    Debug.LogError("[Nomina] Android TTS init failed: " + status);
                }
            }
        }
#endif
    }
}