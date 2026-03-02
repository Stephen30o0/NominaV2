using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Nomina
{
    /// <summary>
    /// Tap-to-detect object identification via Azure OpenAI GPT-4o vision.
    /// User taps screen → captures center-crop from AR camera → sends to GPT-4o → returns everyday object name.
    /// GPT-4o understands context: we tell it we're a language-learning app and it returns
    /// the simple, common name people would normally call the object.
    /// </summary>
    public class ObjectDetector : MonoBehaviour
    {
        [Header("AR References")]
        [SerializeField] private ARCameraManager arCameraManager;

        [Header("Azure OpenAI Settings")]
        [SerializeField] private string azureEndpoint = "";
        [SerializeField] private string azureKey = "";
        [SerializeField] private string deploymentName = "gpt-4o";
        [SerializeField] private string apiVersion = "2024-10-21";
        [SerializeField] private int captureResolution = 512;

        [Header("Debug")]
        [SerializeField] private bool debugMode = false;

        // Current detection results
        public string CurrentDetection { get; private set; } = "";
        public float CurrentConfidence { get; private set; } = 0f;
        public bool IsRunning { get; private set; } = false;
        public bool IsScanning => isProcessing;

        public event Action<string, float> OnObjectDetected;

        private bool isProcessing = false;
        private Texture2D captureTexture;
        private int consecutiveErrors = 0;
        private const int maxConsecutiveErrors = 5;

        // System prompt that gives GPT-4o full context about its role
        private const string systemPrompt =
            "You are the vision system for Nomina, a language-learning app. " +
            "Users point their phone camera at real-world objects to learn their names in other languages. " +
            "Your job: identify the main object in the CENTER of the image and return its common, everyday English name — " +
            "the word a normal person would use in daily conversation. " +
            "Examples of GOOD responses: fan, laptop, pen, toilet paper, hairbrush, cup, chair, pillow, phone, remote, shoe, clock, lamp, kettle, towel, bed, book, wallet. " +
            "Rules:\n" +
            "1. Reply with ONLY the object name (1-3 words max), nothing else.\n" +
            "2. Use the simplest, most common word. Say 'fan' not 'electric fan' or 'ventilator'. Say 'laptop' not 'electronic device' or 'computer'.\n" +
            "3. Be specific: say 'hairbrush' not 'tool', say 'pen' not 'office instrument'.\n" +
            "4. If you see a person's hand holding an object, name the object, not the hand.\n" +
            "5. Focus on the single most prominent object at the center of frame.\n" +
            "6. If you truly cannot identify any object, reply with just: unknown";

        // Debug fallback labels
        private static readonly string[] debugLabels = {
            "Cup", "Bottle", "Phone", "Laptop", "Keyboard", "Mouse", "Book",
            "Pen", "Chair", "Table", "Monitor", "Lamp", "Backpack", "Shoe",
            "Hat", "Clock", "Plant", "Car", "Bicycle", "Dog", "Cat"
        };

        private void Start()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            var config = Resources.Load<TextAsset>("api_config");
            if (config != null)
            {
                try
                {
                    var data = JsonUtility.FromJson<ApiConfig>(config.text);
                    if (!string.IsNullOrEmpty(data?.azureOpenAIEndpoint))
                        azureEndpoint = data.azureOpenAIEndpoint;
                    if (!string.IsNullOrEmpty(data?.azureOpenAIKey))
                        azureKey = data.azureOpenAIKey;
                    if (!string.IsNullOrEmpty(data?.azureOpenAIDeployment))
                        deploymentName = data.azureOpenAIDeployment;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Nomina] Failed to parse api_config.json: {e.Message}");
                }
            }

            if (string.IsNullOrEmpty(azureEndpoint) || string.IsNullOrEmpty(azureKey))
            {
                Debug.LogWarning("[Nomina] No Azure OpenAI credentials configured. Running in debug mode.");
                debugMode = true;
            }
            else
            {
                Debug.Log($"[Nomina] Azure OpenAI GPT-4o ready: {azureEndpoint} (deployment: {deploymentName})");
            }
        }

        public void StartDetection()
        {
            IsRunning = true;
            consecutiveErrors = 0;
            Debug.Log("[Nomina] Detection ready — tap to identify objects with GPT-4o");
        }

        public void StopDetection()
        {
            IsRunning = false;
            CurrentDetection = "";
            CurrentConfidence = 0f;
            Debug.Log("[Nomina] Detection stopped");
        }

        /// <summary>
        /// Called when user taps the screen. Captures current frame and sends to GPT-4o.
        /// </summary>
        public void DetectOnce()
        {
            if (!IsRunning || isProcessing)
            {
                Debug.Log("[Nomina] DetectOnce skipped (not running or already processing)");
                return;
            }

            if (debugMode)
            {
                RunDebugDetection();
                return;
            }

            if (consecutiveErrors >= maxConsecutiveErrors)
            {
                Debug.LogWarning("[Nomina] Too many errors, falling back to debug mode");
                debugMode = true;
                consecutiveErrors = 0;
                RunDebugDetection();
                return;
            }

            StartCoroutine(RunGPT4oDetection());
        }

        /// <summary>
        /// Capture center of AR camera frame, convert to base64, send to Azure OpenAI GPT-4o.
        /// </summary>
        private IEnumerator RunGPT4oDetection()
        {
            if (arCameraManager == null) yield break;

            if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
            {
                Debug.LogWarning("[Nomina] Could not acquire camera image");
                yield break;
            }

            isProcessing = true;

            string base64Image = null;

            try
            {
                // Center-crop 60% of camera frame for better focus on target object
                int cropW = (int)(cpuImage.width * 0.6f);
                int cropH = (int)(cpuImage.height * 0.6f);
                int cropX = (cpuImage.width - cropW) / 2;
                int cropY = (cpuImage.height - cropH) / 2;

                int targetSize = Mathf.Min(captureResolution, Mathf.Min(cropW, cropH));

                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(cropX, cropY, cropW, cropH),
                    outputDimensions = new Vector2Int(targetSize, targetSize),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.MirrorY
                };

                int size = cpuImage.GetConvertedDataSize(conversionParams);
                var buffer = new Unity.Collections.NativeArray<byte>(size, Unity.Collections.Allocator.Temp);

                cpuImage.Convert(conversionParams, buffer);
                cpuImage.Dispose();

                if (captureTexture == null || captureTexture.width != targetSize)
                {
                    if (captureTexture != null) Destroy(captureTexture);
                    captureTexture = new Texture2D(targetSize, targetSize, TextureFormat.RGBA32, false);
                }

                captureTexture.LoadRawTextureData(buffer);
                captureTexture.Apply();
                buffer.Dispose();

                byte[] jpegBytes = captureTexture.EncodeToJPG(75);
                base64Image = Convert.ToBase64String(jpegBytes);

                Debug.Log($"[Nomina] Captured {targetSize}x{targetSize} image ({jpegBytes.Length / 1024}KB) for GPT-4o");
            }
            catch (Exception e)
            {
                cpuImage.Dispose();
                Debug.LogError($"[Nomina] Camera capture error: {e.Message}");
                isProcessing = false;
                yield break;
            }

            if (string.IsNullOrEmpty(base64Image))
            {
                isProcessing = false;
                yield break;
            }

            // Build Azure OpenAI Chat Completions request with vision
            string url = $"{azureEndpoint.TrimEnd('/')}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}";

            // Build JSON body manually (JsonUtility can't handle the nested polymorphic content array)
            string escapedSystemPrompt = EscapeJsonString(systemPrompt);
            string jsonBody = "{" +
                "\"messages\":[" +
                    "{\"role\":\"system\",\"content\":\"" + escapedSystemPrompt + "\"}," +
                    "{\"role\":\"user\",\"content\":[" +
                        "{\"type\":\"text\",\"text\":\"What is this object?\"}," +
                        "{\"type\":\"image_url\",\"image_url\":{\"url\":\"data:image/jpeg;base64," + base64Image + "\",\"detail\":\"low\"}}" +
                    "]}" +
                "]," +
                "\"max_tokens\":20," +
                "\"temperature\":0.1" +
            "}";

            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("api-key", azureKey);
                request.timeout = 20;

                Debug.Log($"[Nomina] Sending image to GPT-4o ({url.Split('?')[0]})...");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    consecutiveErrors = 0;
                    ParseGPT4oResponse(request.downloadHandler.text);
                }
                else
                {
                    consecutiveErrors++;
                    Debug.LogWarning($"[Nomina] GPT-4o error ({request.responseCode}): {request.error} [{consecutiveErrors}/{maxConsecutiveErrors}]");
                    if (request.downloadHandler?.text != null)
                    {
                        string resp = request.downloadHandler.text;
                        if (resp.Length > 400) resp = resp.Substring(0, 400);
                        Debug.LogWarning($"[Nomina] Response: {resp}");
                    }

                    // Notify UI that detection failed
                    OnObjectDetected?.Invoke("", 0f);
                }
            }

            isProcessing = false;
        }

        private void ParseGPT4oResponse(string json)
        {
            try
            {
                Debug.Log($"[Nomina] GPT-4o response: {(json.Length > 300 ? json.Substring(0, 300) + "..." : json)}");

                // Parse the chat completion response to extract the assistant's message
                var response = JsonUtility.FromJson<ChatCompletionResponse>(json);

                if (response?.choices != null && response.choices.Length > 0)
                {
                    string objectName = response.choices[0]?.message?.content?.Trim() ?? "";

                    // Clean up the response
                    objectName = CleanGPT4oResponse(objectName);

                    if (!string.IsNullOrEmpty(objectName) &&
                        !objectName.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                    {
                        CurrentDetection = objectName;
                        CurrentConfidence = 0.90f; // GPT-4o is highly reliable
                        OnObjectDetected?.Invoke(objectName, CurrentConfidence);
                        Debug.Log($"[Nomina] GPT-4o identified: {objectName}");
                    }
                    else
                    {
                        Debug.Log("[Nomina] GPT-4o could not identify object");
                        OnObjectDetected?.Invoke("", 0f);
                    }
                }
                else
                {
                    Debug.LogWarning("[Nomina] GPT-4o response had no choices");
                    OnObjectDetected?.Invoke("", 0f);
                }
            }
            catch (Exception e)
            {
                string preview = json != null && json.Length > 300 ? json.Substring(0, 300) : json ?? "(null)";
                Debug.LogError($"[Nomina] GPT-4o parse error: {e.Message}\nJSON: {preview}");
                OnObjectDetected?.Invoke("", 0f);
            }
        }

        /// <summary>
        /// Clean up GPT-4o's response to ensure we get just a clean object name.
        /// </summary>
        private string CleanGPT4oResponse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            string cleaned = raw.Trim();

            // Remove quotes if GPT wrapped it
            cleaned = cleaned.Trim('"', '\'', '.', '!', ',');

            // Remove "It's a" / "This is a" prefixes if GPT was too verbose
            string lower = cleaned.ToLower();
            string[] prefixes = { "it's a ", "it is a ", "this is a ", "this is an ", "it's an ", "a ", "an ", "the " };
            foreach (var prefix in prefixes)
            {
                if (lower.StartsWith(prefix))
                {
                    cleaned = cleaned.Substring(prefix.Length);
                    break;
                }
            }

            // Capitalize first letter
            if (cleaned.Length > 0)
                cleaned = char.ToUpper(cleaned[0]) + cleaned.Substring(1).ToLower();

            return cleaned;
        }

        private string EscapeJsonString(string s)
        {
            return s.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        private void RunDebugDetection()
        {
            int randomIndex = UnityEngine.Random.Range(0, debugLabels.Length);
            CurrentDetection = debugLabels[randomIndex];
            CurrentConfidence = UnityEngine.Random.Range(0.6f, 0.98f);
            OnObjectDetected?.Invoke(CurrentDetection, CurrentConfidence);
            Debug.Log($"[Nomina] Debug detection: {CurrentDetection}");
        }

        public void SetDetectionResult(string label, float confidence)
        {
            CurrentDetection = label;
            CurrentConfidence = confidence;
            OnObjectDetected?.Invoke(label, confidence);
        }

        private void OnDestroy()
        {
            if (captureTexture != null)
                Destroy(captureTexture);
        }

        // ========== JSON Classes ==========

        [Serializable]
        private class ApiConfig
        {
            public string azureOpenAIEndpoint;
            public string azureOpenAIKey;
            public string azureOpenAIDeployment;
        }

        // Azure OpenAI Chat Completion response
        [Serializable]
        private class ChatCompletionResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }
    }
}
