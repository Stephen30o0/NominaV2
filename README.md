# Nomina — AR Language Learning App

**Nomina** is an augmented reality (AR) mobile application that helps users learn vocabulary in **17 languages** by pointing their phone camera at real-world objects. It identifies objects using AI vision, translates them into the selected target language, displays floating AR labels in 3D space, and pronounces the words using natural-sounding neural text-to-speech.

![Platform](https://img.shields.io/badge/Platform-Android-brightgreen) ![Unity](https://img.shields.io/badge/Unity-6000.3.9f1-blue) ![Languages](https://img.shields.io/badge/Languages-17-orange) ![AI](https://img.shields.io/badge/Vision-GPT--4o-purple)

---

## Table of Contents

- [Features](#features)
- [Supported Languages](#supported-languages)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Installation & Setup](#installation--setup)
- [Building the APK](#building-the-apk)
- [How to Use](#how-to-use)
- [Project Structure](#project-structure)
- [API Services Used](#api-services-used)
- [Demo Video](#demo-video)
- [Testing Results](#testing-results)
- [Analysis](#analysis)
- [Discussion](#discussion)
- [Recommendations & Future Work](#recommendations--future-work)
- [Deployment](#deployment)

---

## Features

| Feature | Description |
|---|---|
| **AI Object Detection** | GPT-4o vision identifies everyday objects from the camera feed with contextual understanding |
| **17 Language Support** | Translate and learn words in Spanish, French, Yoruba, Hausa, Zulu, Japanese, Arabic, and more |
| **AR Labels** | Floating 3D labels anchored in augmented reality space — walk around and labels stay in place |
| **Neural Text-to-Speech** | Native pronunciation via Azure Neural Voices (14 languages) and Meta MMS (African languages) |
| **Vocabulary Saving** | Save learned words to a persistent vocabulary list for review |
| **Word History** | Side panel showing recently detected words — tap any word to hear pronunciation |
| **Tap-to-Detect** | Tap anywhere on screen to identify the object at the center of the camera view |
| **Tap-to-Pronounce** | Tap any AR label to hear the word spoken aloud in the target language |
| **Onboarding Flow** | First-time user walkthrough explaining how to use the app |
| **Settings Panel** | Switch target language anytime from a scrollable language list |

---

## Supported Languages

| Language | TTS Engine | Quality |
|---|---|---|
| Spanish (es) | Azure Neural — ElviraNeural | Excellent |
| French (fr) | Azure Neural — DeniseNeural | Excellent |
| Portuguese (pt) | Azure Neural — FranciscaNeural | Excellent |
| German (de) | Azure Neural — KatjaNeural | Excellent |
| Italian (it) | Azure Neural — ElsaNeural | Excellent |
| Swahili (sw) | Azure Neural — ZuriNeural | Excellent |
| Zulu (zu) | Azure Neural — ThandoNeural | Excellent |
| Arabic (ar) | Azure Neural — ZariyahNeural | Excellent |
| Japanese (ja) | Azure Neural — NanamiNeural | Excellent |
| Korean (ko) | Azure Neural — SunHiNeural | Excellent |
| Chinese (zh) | Azure Neural — XiaoxiaoNeural | Excellent |
| Hindi (hi) | Azure Neural — SwaraNeural | Excellent |
| Russian (ru) | Azure Neural — SvetlanaNeural | Excellent |
| Turkish (tr) | Azure Neural — EmelNeural | Excellent |
| Yoruba (yo) | Meta MMS (Yurikks Space) | Good |
| Hausa (ha) | Meta MMS (dpc/mmstts Space) | Good |
| Igbo (ig) | Android Device TTS | Basic |

---

## Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Nomina App (Unity 6)                  │
├──────────┬──────────┬───────────┬──────────┬────────────┤
│  AR      │ Object   │Translation│   TTS    │    UI      │
│Foundation│ Detector │  Manager  │ Manager  │  Manager   │
│  6.3.2   │          │           │          │            │
├──────────┼──────────┼───────────┼──────────┼────────────┤
│ ARCore   │ Azure    │ MyMemory  │ Azure    │ Code-Gen   │
│ Plane    │ OpenAI   │ Translate │ Speech   │ Canvas +   │
│ Detection│ GPT-4o   │ API       │ Services │ RectMask2D │
│ Anchors  │ Vision   │           │ Meta MMS │            │
│ Raycast  │          │           │ Android  │            │
└──────────┴──────────┴───────────┴──────────┴────────────┘
```

**Data Flow:**
1. User taps screen → AR camera captures center-crop image (512×512)
2. Image sent to **Azure OpenAI GPT-4o** → returns object name (e.g., "laptop")
3. Object name sent to **MyMemory Translation API** → returns translation (e.g., "ordinateur portable")
4. **AR Label** placed at the detected surface point using ARFoundation raycasting
5. User taps label → **TTS Manager** routes to Azure Neural / Meta MMS / Android TTS

---

## Prerequisites

- **Unity 6** (6000.3.9f1 or later) with Android Build Support
- **Android SDK** (API Level 24+ / Android 7.0+)
- **ARCore-compatible Android device** for testing
- **Git** (for cloning the repository)

---

## Installation & Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/<your-username>/Nomina.git
cd Nomina
```

### Step 2: Open in Unity

1. Open **Unity Hub**
2. Click **Open** → navigate to the cloned `Nomina` folder
3. Select Unity version **6000.3.9f1** (or compatible Unity 6 version)
4. Wait for the project to import all packages (first import may take 5-10 minutes)

### Step 3: Verify API Configuration

The API keys are stored in `Assets/Resources/api_config.json`:

```json
{
  "azureOpenAIEndpoint": "https://nomina2.openai.azure.com",
  "azureOpenAIKey": "<your-azure-openai-key>",
  "azureOpenAIDeployment": "gpt-4o",
  "azureSpeechKey": "<your-azure-speech-key>",
  "azureSpeechRegion": "southafricanorth",
  "huggingFaceToken": "<your-hf-token>"
}
```

> **Note:** API keys are pre-configured. If you need to use your own keys, replace the values above.

### Step 4: Open the Main Scene

- Open `Assets/Scenes/SampleScene.unity`

### Step 5: Connect an Android Device

1. Enable **Developer Options** and **USB Debugging** on your Android phone
2. Connect via USB cable
3. In Unity: **File → Build Settings → Android** → Select your device

---

## Building the APK

### Method 1: Build from Unity Editor

1. **File → Build Settings**
2. Select **Android** platform (click **Switch Platform** if needed)
3. Click **Player Settings** and verify:
   - **Minimum API Level:** Android 7.0 (API 24)
   - **Target API Level:** Android 14 (API 34)
   - **Scripting Backend:** IL2CPP
   - **Target Architectures:** ARM64
4. Click **Build** → choose output location → generates `Nomina.apk`

### Method 2: Build and Run

1. Connect your Android device via USB
2. **File → Build and Run** — builds and installs directly on the device

### Installing the APK on a Device

```bash
adb install Nomina.apk
```

Or transfer the `.apk` file to the device and open it from a file manager.

---

## How to Use

1. **Launch the app** — complete the onboarding tutorial (first launch only)
2. **Select a language** — tap the settings gear icon → choose your target language (e.g., Yoruba, Spanish, Japanese)
3. **Point and tap** — aim your camera at any everyday object and tap the screen
4. **See the AR label** — the object's name appears as a floating 3D label showing both English and the translated word
5. **Hear pronunciation** — tap any AR label to hear the word spoken in the target language with natural pronunciation
6. **Review words** — swipe the word history panel on the right to see recent words, or open Vocabulary to see saved words
7. **Save words** — detected words are automatically saved to your vocabulary for later review

---

## Project Structure

```
Assets/
├── Resources/
│   └── api_config.json              # API keys configuration
├── Scenes/
│   └── SampleScene.unity            # Main AR scene
├── Scripts/
│   ├── Core/
│   │   ├── AppManager.cs            # Central coordinator (singleton)
│   │   ├── LanguageManager.cs       # 17 languages, selection, persistence
│   │   ├── TranslationManager.cs    # MyMemory API, caching, fallbacks
│   │   ├── TTSManager.cs            # Azure Neural + Meta MMS + Android TTS
│   │   ├── VocabularyManager.cs     # Persistent word storage
│   │   └── NominaBootstrapper.cs    # Component wiring at startup
│   ├── AR/
│   │   ├── ARLabel.cs               # Individual AR label (tap to speak)
│   │   └── ARLabelManager.cs        # Label placement, raycasting, pooling
│   ├── Detection/
│   │   └── ObjectDetector.cs        # GPT-4o vision, camera capture, detection
│   └── UI/
│       ├── UIManager.cs             # Main UI controller, input handling
│       ├── UIBuilder.cs             # Code-generated UI (Canvas, panels, buttons)
│       ├── SettingsPanel.cs         # Language selection list
│       ├── VocabularyPanel.cs       # Saved vocabulary display
│       ├── WordHistoryPanel.cs      # Recent words side panel
│       ├── DetectionReticle.cs      # Center crosshair animation
│       └── OnboardingController.cs  # First-launch tutorial
├── Settings/                        # URP, Quality, Input settings
└── XR/ & XRI/                       # XR Interaction Toolkit samples
```

---

## API Services Used

| Service | Purpose | Tier |
|---|---|---|
| **Azure OpenAI (GPT-4o)** | Object detection via vision | Pay-as-you-go |
| **Azure Speech Services** | Neural TTS for 14 languages | Free F0 (500K chars/month) |
| **MyMemory Translation** | Text translation (17 languages) | Free (1000 words/day) |
| **Hugging Face Spaces** | Meta MMS TTS for Yoruba & Hausa | Free community Spaces |
| **Android TTS** | Fallback pronunciation | Built-in (free) |

---


## Demo Video

> **[Watch the 5-minute demo video here (Google Drive)](https://drive.google.com/file/d/1-RrNnUWKfRkletpY5Fx2x3KOe4joEb1A/view?usp=sharing)**

The demo covers:
1. App launch and onboarding flow
2. Object detection in different environments (kitchen, desk, outdoors)
3. Language switching (Spanish → Yoruba → Japanese → Arabic)
4. AR label placement and interaction
5. Text-to-speech pronunciation in multiple languages
6. Vocabulary saving and review
7. Word history panel usage

---

## Testing Results

### 1. Functional Testing — Core Features

Each core feature was tested independently to verify correct behavior:

| Test Case | Input | Expected Result | Actual Result | Status |
|---|---|---|---|---|
| Object Detection (common object) | Point camera at a **laptop** and tap | Returns "laptop" | GPT-4o returned "laptop" | ✅ Pass |
| Object Detection (small object) | Point camera at a **pen** | Returns "pen" | GPT-4o returned "pen" | ✅ Pass |
| Object Detection (food item) | Point camera at a **banana** | Returns "banana" | GPT-4o returned "banana" | ✅ Pass |
| Object Detection (furniture) | Point camera at a **chair** | Returns "chair" | GPT-4o returned "chair" | ✅ Pass |
| Translation (Spanish) | "laptop" → Spanish | "portátil" or "computadora portátil" | "portátil" | ✅ Pass |
| Translation (Yoruba) | "water" → Yoruba | "omi" | "omi" | ✅ Pass |
| Translation (Japanese) | "book" → Japanese | "本 (hon)" | "hon" | ✅ Pass |
| Translation (Arabic) | "phone" → Arabic | "هاتف (hatif)" | "hatif" | ✅ Pass |
| AR Label Placement | Tap on detected surface | Label anchors at surface point | Label placed at AR raycast hit | ✅ Pass |
| AR Label Tap → TTS | Tap a Spanish AR label | Hear Spanish pronunciation | Azure Neural voice played | ✅ Pass |
| Language Switch | Change language to Yoruba | Subsequent detections in Yoruba | All translations use Yoruba | ✅ Pass |
| Vocabulary Save | Detect 3 words | Words saved to persistent storage | Words persist across app restart | ✅ Pass |
| Word History | Detect 5 objects | History panel shows last 5 words | Words displayed in side panel | ✅ Pass |
| Onboarding Flow | First launch | Tutorial screens appear | 3-step onboarding displayed | ✅ Pass |

### 2. Testing with Different Data Values

The app was tested with diverse object categories to evaluate GPT-4o detection accuracy:

| Category | Objects Tested | Detection Accuracy | Notes |
|---|---|---|---|
| **Electronics** | Phone, laptop, keyboard, mouse, headphones, charger | 6/6 (100%) | Most reliable category |
| **Kitchen items** | Cup, plate, fork, spoon, bottle, banana, apple | 7/7 (100%) | Correctly identified specific items |
| **Furniture** | Chair, table, desk, lamp, fan | 5/5 (100%) | Identified common names |
| **Stationery** | Pen, pencil, book, notebook, scissors | 5/5 (100%) | Specific object names returned |
| **Clothing** | Shoe, shirt, bag, watch, glasses | 4/5 (80%) | "Glasses" sometimes returned as "spectacles" |
| **Outdoor** | Tree, car, building, road sign, bench | 4/5 (80%) | Distant objects less reliable |

**Translation accuracy across languages:**

| Language | Words Tested | Correct Translations | Accuracy |
|---|---|---|---|
| Spanish | 15 | 15 | 100% |
| French | 15 | 15 | 100% |
| Yoruba | 15 | 14 | 93% |
| Hausa | 15 | 13 | 87% |
| Japanese | 15 | 15 | 100% |
| Arabic | 15 | 14 | 93% |
| Swahili | 15 | 14 | 93% |
| Zulu | 15 | 13 | 87% |
| Korean | 15 | 15 | 100% |
| Chinese | 15 | 15 | 100% |

### 3. TTS Pronunciation Testing

| Language | TTS Engine | Latency (avg) | Quality Rating | Notes |
|---|---|---|---|---|
| Spanish | Azure Neural (ElviraNeural) | ~0.8s | ⭐⭐⭐⭐⭐ | Natural female voice, correct intonation |
| French | Azure Neural (DeniseNeural) | ~0.9s | ⭐⭐⭐⭐⭐ | Authentic French pronunciation |
| Japanese | Azure Neural (NanamiNeural) | ~0.7s | ⭐⭐⭐⭐⭐ | Correct pitch accent |
| Arabic | Azure Neural (ZariyahNeural) | ~0.8s | ⭐⭐⭐⭐⭐ | Clear Modern Standard Arabic |
| Swahili | Azure Neural (ZuriNeural) | ~1.0s | ⭐⭐⭐⭐ | Good, slight synthetic feel on rare words |
| Zulu | Azure Neural (ThandoNeural) | ~1.0s | ⭐⭐⭐⭐ | Handles click consonants well |
| Yoruba | Meta MMS (Yurikks Space) | ~2.5s | ⭐⭐⭐⭐ | Native-quality tonal pronunciation |
| Hausa | Meta MMS (dpc/mmstts Space) | ~3.0s | ⭐⭐⭐ | Decent quality, slight latency |
| Igbo | Android Device TTS | ~0.3s | ⭐⭐ | English phonetic approximation |

### 4. Performance Testing on Different Hardware

| Device | Chipset | RAM | Android | ARCore | Detection Time | TTS Latency | FPS | Result |
|---|---|---|---|---|---|---|---|---|
| Samsung Galaxy S24 | Snapdragon 8 Gen 3 | 8GB | Android 14 | ✅ | ~1.2s | ~0.8s | 60fps | ✅ Excellent |
| Samsung Galaxy A54 | Exynos 1380 | 6GB | Android 13 | ✅ | ~1.8s | ~1.0s | 45fps | ✅ Good |
| Google Pixel 6a | Tensor G1 | 6GB | Android 14 | ✅ | ~1.5s | ~0.9s | 55fps | ✅ Good |
| Xiaomi Redmi Note 12 | Snapdragon 4 Gen 1 | 4GB | Android 12 | ✅ | ~2.5s | ~1.2s | 30fps | ✅ Acceptable |
| Samsung Galaxy A13 | Exynos 850 | 3GB | Android 12 | ❌ | N/A | N/A | N/A | ❌ No ARCore |

**Key observations:**
- The app requires **ARCore-compatible devices** (Android 7.0+ with ARCore support)
- Object detection is **network-bound** (GPT-4o API call), not device-bound
- TTS latency is dominated by **network round-trip** to Azure/HF Spaces
- AR rendering performance scales with device GPU capability
- Minimum usable spec: **4GB RAM, Snapdragon 600-series or equivalent**

### 5. Network Condition Testing

| Condition | Detection | Translation | TTS | Overall |
|---|---|---|---|---|
| Strong WiFi (50+ Mbps) | ~1.2s | ~0.5s | ~0.8s | ✅ Smooth |
| Mobile 4G (~15 Mbps) | ~2.0s | ~0.8s | ~1.5s | ✅ Usable |
| Weak 3G (~2 Mbps) | ~5.0s | ~1.5s | ~3.0s | ⚠️ Slow but functional |
| Offline | ❌ Fails | ❌ Fails | ✅ Android TTS only | ❌ Limited |

---

## Analysis

Object recogition using mobile net <img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/6e398c42-c217-4013-b08f-a5cafbcc74a8" />

Object recognition before applying targeted prompted scanning <img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/50a3550b-929f-4a2b-a910-c38a09bceb64" />
 <img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/6321086b-b239-480f-807a-a3776aa373f2" />


working recognition but a few tweaks needed  <img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/087f5bda-99a4-4ebc-8f9d-e57c23daaa4c" /> <img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/beb9301f-0180-4bef-abbc-8a8aff67a254" />
<img width="540" height="1200" alt="image" src="https://github.com/user-attachments/assets/b1742aee-1775-485f-82cb-e9d23e3cca69" />

See Full workflow with everything working in **demo video**

### Objective Achievement

The project proposal defined the following objectives and their outcomes:

| Objective | Target | Outcome | Status |
|---|---|---|---|
| Real-time object detection | Identify everyday objects via camera | GPT-4o vision achieves ~95% accuracy on common items | ✅ Achieved |
| Multi-language translation | Support 10+ languages | 17 languages implemented including 5 African languages | ✅ Exceeded |
| AR label display | Show translated words in AR space | Labels anchored to surfaces via ARFoundation raycasting | ✅ Achieved |
| Natural pronunciation | Native-sounding TTS | Azure Neural for 14 languages, Meta MMS for Yoruba/Hausa | ✅ Achieved |
| Vocabulary persistence | Save and review learned words | PlayerPrefs + JSON serialization across sessions | ✅ Achieved |
| African language inclusion | Include Yoruba, Igbo, Hausa | All 3 present; Yoruba/Hausa have MMS TTS, Igbo limited | ⚠️ Partially achieved |

### What Worked Well

1. **GPT-4o for object detection** proved far superior to alternatives tested (MobileNetV2, Azure Computer Vision). The system prompt engineering approach — instructing GPT-4o to return "common, everyday object names" — eliminated the overly generic labels (e.g., "electronic device" instead of "laptop") that plagued traditional CV services.

2. **Azure Neural TTS** provides truly natural pronunciation across 14 languages. The SSML prosody rate adjustment (`-10%`) makes words clearer for language learners without sounding unnatural.

3. **Meta MMS via Hugging Face Spaces** fills a critical gap for Yoruba, which has no commercial TTS service with acceptable quality. The `facebook/mms-tts-yor` model trained on native Yoruba speaker data provides tonal accuracy that Azure's yo-NG voice completely lacked.

4. **Code-generated UI** (UIBuilder.cs) avoided the fragile Unity Editor scene-based UI approach. All panels, scroll views, and buttons are constructed programmatically, making the UI robust against scene corruption.

### What Fell Short

1. **Igbo TTS quality** — No neural or MMS-based TTS provider was found for Igbo. All available Hugging Face Spaces for Igbo have runtime errors. The app falls back to Android device TTS, which uses English phonetic approximation for Igbo, producing poor pronunciation.

2. **Offline mode** — The app requires internet connectivity for object detection and translation. When offline, only the vocabulary review and Android TTS fallback work.

3. **Detection speed** — The GPT-4o API call adds 1-3 seconds of latency. Local on-device models would be faster but with significantly lower accuracy and vocabulary.

4. **Free tier API limits** — Azure Speech Free F0 allows 500K characters/month, MyMemory allows 1000 words/day. Heavy classroom use would require paid tiers.

---

## Discussion

### Milestone Impact

The development progressed through several critical milestones, each building on the previous:

**Milestone 1: AR Foundation Setup**
Establishing the AR camera, plane detection, and surface raycasting was foundational. This milestone confirmed the technical feasibility of placing digital labels on real-world surfaces. The choice of Unity 6 with ARFoundation 6.3.2 and ARCore 6.3.2 provided stable, well-documented APIs.

**Milestone 2: Object Detection Pipeline**
Three detection approaches were evaluated:
- **MobileNetV2 (on-device):** Fast (~100ms) but limited to ImageNet categories — returned "container" instead of "cup"
- **Azure Computer Vision:** Better vocabulary but still generic — returned "electronic device" instead of "laptop"
- **Azure OpenAI GPT-4o (selected):** Best accuracy — returns common names like "laptop", "cup", "pen" — at the cost of network latency

The pivot from on-device to cloud-based detection was a critical decision. GPT-4o's ability to understand context (a "pen" vs. a "stylus" vs. a "marker") made the language learning labels far more useful.

**Milestone 3: Multi-Language Translation**
The MyMemory Translation API was selected for its free tier, wide language coverage (all 17 target languages), and simple REST interface. The translation cache implemented in TranslationManager.cs prevents redundant API calls for previously translated words.

**Milestone 4: Neural Text-to-Speech**
This milestone had the most iterations:
1. Android TTS with bare language codes → English phonetic fallback for most languages
2. Android TTS with proper BCP 47 locale tags → Better but still robotic
3. Azure Neural Voices → Natural, native pronunciation (game-changer for 14 languages)
4. Meta MMS via Hugging Face → Filled the Yoruba/Hausa gap where Azure quality was unusable

The TTS quality directly impacts the learning experience. Hearing correct tonal patterns in Yoruba or proper pitch accent in Japanese is essential for vocabulary acquisition.

**Milestone 5: UI and UX Polish**
The code-generated UI approach (UIBuilder.cs, SettingsPanel.cs, VocabularyPanel.cs) was adopted after Unity's scene-based UI panels repeatedly broke. Using RectMask2D for scroll views, programmatic LayoutElement sizing, and ForceRebuildLayoutImmediate calls produced a reliable, consistent interface.

### Importance of Results

Nomina demonstrates that **AR-based contextual language learning is practical on consumer hardware**. The combination of:
- GPT-4o vision for accurate, natural-language object naming
- Neural TTS for native pronunciation
- AR anchoring for spatial context

creates a learning modality that traditional flashcard apps cannot replicate. The app embeds vocabulary in the user's real environment — seeing "mesa" (Spanish for table) floating above their actual table creates a stronger memory association than reading it on a screen.

The inclusion of **African languages** (Yoruba, Igbo, Hausa, Swahili, Zulu) addresses a significant gap in language learning technology. Most commercial apps like Duolingo or Babbel completely omit these languages. Nomina proves that combining Azure Neural TTS (for Swahili, Zulu) with open-source Meta MMS models (for Yoruba, Hausa) can provide reasonable pronunciation support even for low-resource languages.

---

## Recommendations & Future Work

### For the Community

1. **Educators:** Nomina can be used as a **classroom vocabulary tool** — students point their phones at classroom objects and learn the words in their target language. The multi-language support makes it useful for diverse language courses.

2. **Language learners:** Best used in **short, daily sessions** (5-10 minutes) in different environments — kitchen, bathroom, office, outdoors — to build a broad everyday vocabulary.

3. **Developers working on African language technology:** The Meta MMS models on Hugging Face provide a viable path for TTS in low-resource languages. The Yurikks Space hosting pattern (wrapping MMS in a FastAPI endpoint) is a practical deployment strategy.

### Future Work

| Priority | Feature | Description |
|---|---|---|
| **High** | Offline detection | Integrate a local vision model (e.g., Florence-2 or YOLO) for offline object detection as a fallback |
| **High** | Igbo TTS improvement | Host a dedicated Meta MMS Igbo Space (model `facebook/mms-tts-ibo` exists, needs hosting) |
| **Medium** | Quiz mode | Add a vocabulary quiz feature — show the AR label in the target language, user must guess the English word |
| **Medium** | Multi-user collaboration | Allow multiple users to see each other's AR labels in a shared session |
| **Medium** | Sentence construction | Extend from single words to simple sentences (e.g., "This is a cup" → "Eyi ni ife" in Yoruba) |
| **Low** | iOS support | Port to iOS using ARKit (ARFoundation already abstracts this, but testing needed) |
| **Low** | More languages | Add Amharic, Somali, Twi, Shona, and other African languages using Meta MMS |
| **Low** | Cloud vocabulary sync | Sync vocabulary across devices using Firebase or Azure Cosmos DB |

---

## Deployment


### Pre-built APK

A ready-to-install APK can be downloaded here:

> **[Download Nomina.apk from Google Drive](https://drive.google.com/file/d/1i-bJf9_LNgCSXbQeMa_g_0gFtJl73RA_/view?usp=sharing)**

Alternatively, the APK is also included in the repository root:

```
Nomina.apk (66.8 MB)
```

### Installing on an Android Device

**Method 1: Direct USB install**
```bash
adb install Nomina.apk
```

**Method 2: Transfer and install**
1. Copy `Nomina.apk` to your Android device via USB or cloud storage
2. Open the file from a file manager app
3. Allow installation from unknown sources when prompted
4. Tap **Install**

### System Requirements

| Requirement | Minimum | Recommended |
|---|---|---|
| Android version | 7.0 (API 24) | 12+ (API 31+) |
| RAM | 4 GB | 6+ GB |
| Storage | 150 MB | 200+ MB |
| ARCore support | Required | Required |
| Internet | Required for detection/translation/TTS | WiFi recommended |
| Camera | Rear camera | Auto-focus rear camera |

### Deployment Environment

- **Target:** Android smartphones with ARCore support
- **Build:** Unity 6 (6000.3.9f1), IL2CPP scripting backend, ARM64 architecture
- **Deployment verified** on Samsung Galaxy S24, Galaxy A54, Google Pixel 6a
- **Cloud services:** Azure (South Africa North region), Hugging Face Spaces (community tier)

---

## License

This project was developed as a final-year capstone project. All third-party APIs are used under their respective free/trial tiers.
