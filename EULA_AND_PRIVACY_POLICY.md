# Nomina EULA and Privacy Policy

Effective Date: March 2026

Application: Nomina - AR Language Learning App

Developer: Stephen (ALU Capstone Project)

## End-User License Agreement (EULA)

### 1. Acceptance of Terms
By installing and using Nomina, you agree to the terms in this EULA. If you do not agree, uninstall the application.

### 2. License Grant
Nomina is licensed for personal, non-commercial, educational use. This app was developed as an academic capstone project and is not offered as a commercial product.

### 3. Description of Service
Nomina uses your device camera and AR capabilities to identify real-world objects, translate object names into a selected language, place translated AR labels, and play pronunciation audio.

### 4. Third-Party Services
Nomina uses third-party services for core functionality:

| Service | Purpose | Data Sent |
|---|---|---|
| Azure OpenAI (GPT-4o) | Object identification | Compressed camera image |
| Azure Speech Services | Neural speech synthesis | Text to synthesize |
| MyMemory Translation API | Translation | Source text and language pair |
| Hugging Face Spaces (Meta MMS) | Yoruba/Hausa speech synthesis | Text to synthesize |

All transmissions are made over HTTPS.

### 5. Camera Usage
Camera access is used only for object detection. Captured frames are center-cropped and compressed before transmission to the detection service. Images are processed for inference and are not stored by the app locally.

### 6. Local Storage
Nomina stores limited local data on-device using Unity PlayerPrefs:
- selected language preference
- saved vocabulary entries
- onboarding completion status

### 7. Service Availability
Core features depend on internet connectivity and third-party API availability. The app may degrade when services are unavailable or usage limits are reached.

### 8. Intellectual Property
Nomina source code, UI, and project documentation are owned by the developer. Third-party APIs and models remain the property of their respective owners.

### 9. Disclaimer and Limitation of Liability
Nomina is provided "as is" without warranties. The developer is not liable for direct or indirect damages resulting from app use, translation errors, pronunciation inaccuracies, or service interruptions.

### 10. Termination
This license ends automatically if you violate these terms. On termination, discontinue use and uninstall the app.

## Privacy Policy

### What Nomina Collects
Nomina does not require accounts and does not intentionally collect personally identifiable information such as names, emails, or phone numbers.

### Camera Data Processing
Camera input is used to detect objects for learning interactions. The app sends a compressed image to cloud inference services for object identification and receives only inference results for display and pronunciation flow.

### Third-Party Processing
Nomina relies on external providers to process detection, translation, and TTS requests:
- Microsoft Azure (OpenAI and Speech)
- MyMemory (Translated.net)
- Hugging Face Spaces

Use of those services is subject to each provider's terms and privacy policies.

### Data Stored on Device
The app stores only functional app data on-device:
- selected language
- saved vocabulary list
- onboarding completion state

Users can clear this data by uninstalling the app or clearing app storage in Android settings.

### Children and Educational Use
Nomina is built as an educational tool. Guardians should supervise camera use in sensitive environments.

### Policy Updates
Any updates to these legal terms will be published in this repository.

### Contact
For questions, use the project GitHub repository contact channel.