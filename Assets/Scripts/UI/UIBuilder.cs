using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Builds the entire Nomina UI programmatically at runtime.
    /// Attach to the NominaUI GameObject. On Awake, creates all Canvas children,
    /// panels, buttons, and wires references into UIManager, SettingsPanel, etc.
    /// </summary>
    public class UIBuilder : MonoBehaviour
    {
        [Header("Color Theme")]
        public Color primaryColor = new Color(0.15f, 0.55f, 1f);
        public Color accentColor = new Color(0.2f, 0.85f, 0.6f);
        public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        public Color surfaceColor = new Color(0.12f, 0.12f, 0.18f, 0.9f);
        public Color textColor = Color.white;
        public Color textSecondary = new Color(0.7f, 0.7f, 0.8f);
        public Color dangerColor = new Color(1f, 0.35f, 0.35f);

        private Canvas canvas;
        private UIManager uiManager;

        // Cached references from BuildMainView for wiring
        private GameObject _detectionOverlay;
        private GameObject _detectionInfoPanel;
        private TMPro.TextMeshProUGUI _detectionLabel;
        private TMPro.TextMeshProUGUI _confidenceLabel;
        private TMPro.TextMeshProUGUI _translationLabel;
        private TMPro.TextMeshProUGUI _currentLanguageLabel;
        private TMPro.TextMeshProUGUI _scanningIndicator;
        private Button _detectButton;
        private Button _settingsButton;
        private Button _vocabularyButton;
        private Button _clearLabelsButton;

        private void Awake()
        {
            uiManager = GetComponent<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[Nomina] UIBuilder: No UIManager found on this GameObject!");
                return;
            }

            // Find existing canvas or create one from scratch
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("NominaCanvas");
                canvasGO.layer = 5; // UI layer
                canvasGO.transform.SetParent(transform, false);
                canvas = canvasGO.AddComponent<Canvas>();
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                Debug.Log("[Nomina] UIBuilder: Created Canvas from scratch");
            }

            // Ensure ScreenSpaceOverlay (scene should already be set, but just in case)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.gameObject.layer = 5;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 2340);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Destroy ALL existing canvas children (scene-created objects may have
            // stale transform references due to Canvas renderMode changes).
            // UIBuilder creates everything from scratch for reliability.
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(canvas.transform.GetChild(i).gameObject);

            // Build all views (each creates its container from scratch)
            try { BuildMainView(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildMainView failed: {e}"); }

            try { BuildOnboardingView(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildOnboardingView failed: {e}"); }

            try { BuildSettingsView(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildSettingsView failed: {e}"); }

            try { BuildVocabularyView(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildVocabularyView failed: {e}"); }

            try { BuildLabelDetailView(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildLabelDetailView failed: {e}"); }

            try { BuildSaveConfirmation(); }
            catch (System.Exception e) { Debug.LogError($"[Nomina] BuildSaveConfirmation failed: {e}"); }

            // Re-wire UIManager panel references (UIBuilder is the authority)
            WireUIManagerPanels();

            Debug.Log("[Nomina] UI built successfully");
        }

        /// <summary>
        /// Creates a fresh view container under the canvas. Always creates new.
        /// </summary>
        private Transform GetOrCreateView(string name)
        {
            var go = new GameObject(name);
            go.layer = 5;
            go.transform.SetParent(canvas.transform, false);
            go.AddComponent<RectTransform>();
            return go.transform;
        }

        /// <summary>
        /// Wire all panel references into UIManager after UI is fully built.
        /// This runs AFTER all Build methods, so references are guaranteed valid.
        /// </summary>
        private void WireUIManagerPanels()
        {
            // Screen panels
            SetPrivateField(uiManager, "mainView", canvas.transform.Find("MainView")?.gameObject);
            SetPrivateField(uiManager, "onboardingView", canvas.transform.Find("OnboardingView")?.gameObject);
            SetPrivateField(uiManager, "settingsView", canvas.transform.Find("SettingsView")?.gameObject);
            SetPrivateField(uiManager, "vocabularyView", canvas.transform.Find("VocabularyView")?.gameObject);

            // HUD elements from BuildMainView
            SetPrivateField(uiManager, "detectionOverlay", _detectionOverlay);
            SetPrivateField(uiManager, "detectionInfoPanel", _detectionInfoPanel);
            SetPrivateField(uiManager, "detectionLabel", _detectionLabel);
            SetPrivateField(uiManager, "confidenceLabel", _confidenceLabel);
            SetPrivateField(uiManager, "translationLabel", _translationLabel);
            SetPrivateField(uiManager, "currentLanguageLabel", _currentLanguageLabel);
            SetPrivateField(uiManager, "scanningIndicator", _scanningIndicator);

            // Buttons from BuildMainView
            SetPrivateField(uiManager, "detectButton", _detectButton);
            SetPrivateField(uiManager, "settingsButton", _settingsButton);
            SetPrivateField(uiManager, "vocabularyButton", _vocabularyButton);
            SetPrivateField(uiManager, "clearLabelsButton", _clearLabelsButton);

            // labelDetailView and saveConfirmationPopup are wired inside their Build methods
            Debug.Log("[Nomina] UIBuilder: All UIManager references wired");
        }

        // ========== MAIN VIEW (AR HUD) ==========
        private void BuildMainView()
        {
            var mainView = GetOrCreateView("MainView");
            var mainRT = EnsureRectTransform(mainView.gameObject);
            StretchFull(mainRT);

            // === Top Bar ===
            var topBar = CreatePanel(mainView, "TopBar", new Color(0, 0, 0, 0.5f));
            var topRT = topBar.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0, 1);
            topRT.anchorMax = new Vector2(1, 1);
            topRT.pivot = new Vector2(0.5f, 1);
            topRT.sizeDelta = new Vector2(0, 140);

            var topLayout = topBar.AddComponent<HorizontalLayoutGroup>();
            topLayout.padding = new RectOffset(30, 30, 50, 10);
            topLayout.spacing = 20;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childForceExpandWidth = false;

            // App title
            var title = CreateText(topBar.transform, "AppTitle", "NOMINA", 28, textColor);
            title.fontStyle = FontStyles.Bold;
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Current language display
            var langLabel = CreateText(topBar.transform, "CurrentLanguage", "Spanish", 20, accentColor);
            var langLE = langLabel.gameObject.AddComponent<LayoutElement>();
            langLE.preferredWidth = 120;

            // Settings button
            var settingsBtn = CreateButton(topBar.transform, "SettingsBtn", "...", 30, surfaceColor);
            var settingsBtnLE = settingsBtn.gameObject.AddComponent<LayoutElement>();
            settingsBtnLE.preferredWidth = 60;
            settingsBtnLE.preferredHeight = 60;

            // === Detection Overlay (center crosshair) ===
            var detectionOverlay = new GameObject("DetectionOverlay");
            detectionOverlay.transform.SetParent(mainView, false);
            var detOverlayRT = detectionOverlay.AddComponent<RectTransform>();
            detOverlayRT.anchoredPosition = Vector2.zero;
            detOverlayRT.sizeDelta = new Vector2(200, 200);

            // Thin crosshair lines instead of corner bracket squares
            CreateCrosshairLine(detectionOverlay.transform, "HLine", new Vector2(200, 2), Vector2.zero);
            CreateCrosshairLine(detectionOverlay.transform, "VLine", new Vector2(2, 200), Vector2.zero);

            // Small center dot
            var centerDot = new GameObject("CenterDot");
            centerDot.transform.SetParent(detectionOverlay.transform, false);
            var dotRT = centerDot.AddComponent<RectTransform>();
            dotRT.sizeDelta = new Vector2(6, 6);
            var dotImg = centerDot.AddComponent<Image>();
            dotImg.color = primaryColor;

            // === Detection Info Panel (below crosshair) ===
            var detInfoPanel = CreatePanel(mainView, "DetectionInfoPanel", new Color(0, 0, 0, 0.6f));
            var detInfoRT = detInfoPanel.GetComponent<RectTransform>();
            detInfoRT.anchoredPosition = new Vector2(0, -180);
            detInfoRT.sizeDelta = new Vector2(450, 140);

            var detInfoLayout = detInfoPanel.AddComponent<VerticalLayoutGroup>();
            detInfoLayout.padding = new RectOffset(20, 20, 8, 8);
            detInfoLayout.childAlignment = TextAnchor.MiddleCenter;

            var detLabel = CreateText(detInfoPanel.transform, "DetectionLabel", "Point at an object...", 20, textColor);
            detLabel.alignment = TextAlignmentOptions.Center;
            var confLabel = CreateText(detInfoPanel.transform, "ConfidenceLabel", "", 14, textSecondary);
            confLabel.alignment = TextAlignmentOptions.Center;

            // Translation display (shows translated word in accent color)
            var transLabel = CreateText(detInfoPanel.transform, "TranslationLabel", "", 28, accentColor);
            transLabel.alignment = TextAlignmentOptions.Center;
            transLabel.fontStyle = FontStyles.Bold;

            // Scanning indicator (animated "Scanning..." text)
            var scanIndicator = CreateText(detInfoPanel.transform, "ScanningIndicator", "", 16, new Color(0.6f, 0.75f, 1f, 0.8f));
            scanIndicator.alignment = TextAlignmentOptions.Center;
            scanIndicator.fontStyle = FontStyles.Italic;
            scanIndicator.gameObject.SetActive(false);

            // === Bottom Bar ===
            var bottomBar = CreatePanel(mainView, "BottomBar", new Color(0, 0, 0, 0.6f));
            var botRT = bottomBar.GetComponent<RectTransform>();
            botRT.anchorMin = new Vector2(0, 0);
            botRT.anchorMax = new Vector2(1, 0);
            botRT.pivot = new Vector2(0.5f, 0);
            botRT.sizeDelta = new Vector2(0, 160);

            var botLayout = bottomBar.AddComponent<HorizontalLayoutGroup>();
            botLayout.padding = new RectOffset(40, 40, 10, 40);
            botLayout.spacing = 30;
            botLayout.childAlignment = TextAnchor.MiddleCenter;
            botLayout.childForceExpandWidth = true;

            // Vocabulary button
            var vocabBtn = CreateButton(bottomBar.transform, "VocabularyBtn", "Words", 18, surfaceColor);
            var vocabBtnLE = vocabBtn.gameObject.AddComponent<LayoutElement>();
            vocabBtnLE.preferredHeight = 70;

            // Detect button (main action - big circle)
            var detectBtn = CreateButton(bottomBar.transform, "DetectBtn", "SCAN", 20, primaryColor);
            var detectBtnLE = detectBtn.gameObject.AddComponent<LayoutElement>();
            detectBtnLE.preferredHeight = 80;

            // Clear labels button
            var clearBtn = CreateButton(bottomBar.transform, "ClearLabelsBtn", "X", 24, surfaceColor);
            var clearBtnLE = clearBtn.gameObject.AddComponent<LayoutElement>();
            clearBtnLE.preferredHeight = 70;

            // === Store references for WireUIManagerPanels ===
            _detectionOverlay = detectionOverlay;
            _detectionInfoPanel = detInfoPanel;
            _detectionLabel = detLabel;
            _confidenceLabel = confLabel;
            _translationLabel = transLabel;
            _currentLanguageLabel = langLabel;
            _scanningIndicator = scanIndicator;
            _detectButton = detectBtn;
            _settingsButton = settingsBtn;
            _vocabularyButton = vocabBtn;
            _clearLabelsButton = clearBtn;
        }

        // ========== ONBOARDING VIEW ==========
        private void BuildOnboardingView()
        {
            var onboardingView = GetOrCreateView("OnboardingView");
            var onbRT = EnsureRectTransform(onboardingView.gameObject);
            StretchFull(onbRT);

            // Ensure OnboardingController exists
            onboardingView.gameObject.AddComponent<OnboardingController>();

            // Background
            var bg = onboardingView.gameObject.AddComponent<Image>();
            bg.color = backgroundColor;

            var layout = onboardingView.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(60, 60, 120, 80);
            layout.spacing = 30;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Page 1: Welcome
            var page1 = CreateOnboardingPage(onboardingView, "Page1",
                "Welcome to Nomina",
                "Your AR-powered language learning companion.\nPoint your camera at objects to learn their names in any language.");

            // Page 2: How to detect
            var page2 = CreateOnboardingPage(onboardingView, "Page2",
                "Detect Objects",
                "Tap the detect button to start scanning.\nA crosshair will appear — point it at any object.");

            // Page 3: Place labels
            var page3 = CreateOnboardingPage(onboardingView, "Page3",
                "Place Labels",
                "Tap the screen to anchor a translation label in AR space.\nTap a label to hear the pronunciation.");

            // Page 4: Build vocabulary
            var page4 = CreateOnboardingPage(onboardingView, "Page4",
                "Build Your Vocabulary",
                "Save words to your personal collection.\nTrack your progress across languages.");

            page2.SetActive(false);
            page3.SetActive(false);
            page4.SetActive(false);

            // Navigation buttons
            var navBar = CreatePanel(onboardingView, "NavBar", Color.clear);
            var navLE = navBar.AddComponent<LayoutElement>();
            navLE.preferredHeight = 80;
            var navLayout = navBar.AddComponent<HorizontalLayoutGroup>();
            navLayout.spacing = 20;
            navLayout.childAlignment = TextAnchor.MiddleCenter;

            var skipBtn = CreateButton(navBar.transform, "SkipBtn", "Skip", 18, Color.clear);
            var nextBtn = CreateButton(navBar.transform, "NextBtn", "Next \u2192", 20, primaryColor);
            var getStartedBtn = CreateButton(navBar.transform, "GetStartedBtn", "Get Started!", 20, accentColor);
            getStartedBtn.gameObject.SetActive(false);

            // Wire to OnboardingController
            var controller = onboardingView.GetComponent<OnboardingController>();
            if (controller != null)
            {
                // Use reflection to set serialized fields
                SetPrivateField(controller, "pages", new GameObject[] { page1, page2, page3, page4 });
                SetPrivateField(controller, "nextButton", nextBtn);
                SetPrivateField(controller, "skipButton", skipBtn);
                SetPrivateField(controller, "getStartedButton", getStartedBtn);
            }
        }

        private GameObject CreateOnboardingPage(Transform parent, string name, string title, string description)
        {
            var page = new GameObject(name);
            page.transform.SetParent(parent, false);
            var pageRT = page.AddComponent<RectTransform>();
            StretchFull(pageRT);

            var pageLayout = page.AddComponent<VerticalLayoutGroup>();
            pageLayout.padding = new RectOffset(40, 40, 100, 40);
            pageLayout.spacing = 30;
            pageLayout.childAlignment = TextAnchor.MiddleCenter;
            pageLayout.childForceExpandHeight = false;

            // Icon placeholder
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(page.transform, false);
            var iconRT = iconObj.AddComponent<RectTransform>();
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.color = primaryColor;
            var iconLE = iconObj.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 120;
            iconLE.preferredHeight = 120;

            // Title
            var titleText = CreateText(page.transform, "Title", title, 36, textColor);
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;

            // Description
            var descText = CreateText(page.transform, "Description", description, 20, textSecondary);
            descText.alignment = TextAlignmentOptions.Center;

            return page;
        }

        // ========== SETTINGS VIEW ==========
        private void BuildSettingsView()
        {
            var settingsView = GetOrCreateView("SettingsView");
            var setRT = EnsureRectTransform(settingsView.gameObject);
            StretchFull(setRT);

            // Ensure SettingsPanel exists
            settingsView.gameObject.AddComponent<SettingsPanel>();

            var bg = settingsView.gameObject.AddComponent<Image>();
            bg.color = backgroundColor;

            var layout = settingsView.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 60, 30);
            layout.spacing = 10;
            layout.childForceExpandHeight = false;

            // Header with back button
            var header = CreatePanel(settingsView, "Header", Color.clear);
            var headerLE = header.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 80;
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 15;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;

            var backBtn = CreateButton(header.transform, "BackBtn", "\u2190", 28, Color.clear);
            var backBtnLE = backBtn.gameObject.AddComponent<LayoutElement>();
            backBtnLE.preferredWidth = 60;

            var headerTitle = CreateText(header.transform, "Title", "Settings", 28, textColor);
            headerTitle.fontStyle = FontStyles.Bold;

            // Language section label
            var langSectionLabel = CreateText(settingsView, "LanguageSection", "TARGET LANGUAGE", 14, textSecondary);
            langSectionLabel.fontStyle = FontStyles.Bold;

            // Language list scroll view
            var scrollObj = new GameObject("LanguageScroll");
            scrollObj.transform.SetParent(settingsView, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            var scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            StretchFull(vpRT);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);

            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;

            // Wire to SettingsPanel
            var settingsPanel = settingsView.GetComponent<SettingsPanel>();
            if (settingsPanel != null)
            {
                SetPrivateField(settingsPanel, "languageListContainer", content.transform);
                SetPrivateField(settingsPanel, "backButton", backBtn);
            }
        }

        // ========== VOCABULARY VIEW ==========
        private void BuildVocabularyView()
        {
            var vocabView = GetOrCreateView("VocabularyView");
            var vocRT = EnsureRectTransform(vocabView.gameObject);
            StretchFull(vocRT);

            // Ensure VocabularyPanel exists
            vocabView.gameObject.AddComponent<VocabularyPanel>();

            var bg = vocabView.gameObject.AddComponent<Image>();
            bg.color = backgroundColor;

            var layout = vocabView.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 60, 30);
            layout.spacing = 10;
            layout.childForceExpandHeight = false;

            // Header
            var header = CreatePanel(vocabView, "Header", Color.clear);
            var headerLE = header.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 80;
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 15;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childForceExpandWidth = false;

            var backBtn = CreateButton(header.transform, "BackBtn", "\u2190", 28, Color.clear);
            var backBtnLE = backBtn.gameObject.AddComponent<LayoutElement>();
            backBtnLE.preferredWidth = 60;

            var headerTitle = CreateText(header.transform, "Title", "Saved Words", 28, textColor);
            headerTitle.fontStyle = FontStyles.Bold;
            var titleLE = headerTitle.gameObject.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            var wordCount = CreateText(header.transform, "WordCount", "0 words", 18, textSecondary);
            var wordCountLE = wordCount.gameObject.AddComponent<LayoutElement>();
            wordCountLE.preferredWidth = 100;

            // Empty state
            var emptyState = CreatePanel(vocabView, "EmptyState", Color.clear);
            var emptyStateLE = emptyState.AddComponent<LayoutElement>();
            emptyStateLE.flexibleHeight = 1;
            var emptyLayout = emptyState.AddComponent<VerticalLayoutGroup>();
            emptyLayout.childAlignment = TextAnchor.MiddleCenter;
            var emptyText = CreateText(emptyState.transform, "EmptyText",
                "No saved words yet.\nTap a label in AR view, then tap Save!", 20, textSecondary);
            emptyText.alignment = TextAlignmentOptions.Center;

            // List scroll
            var scrollObj = new GameObject("VocabScroll");
            scrollObj.transform.SetParent(vocabView, false);
            var scrollRT = scrollObj.AddComponent<RectTransform>();
            var scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            StretchFull(vpRT);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);
            contentRT.sizeDelta = new Vector2(0, 0);
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 4;
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.childForceExpandHeight = false;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandWidth = true;
            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;

            // Wire to VocabularyPanel
            var vocabPanel = vocabView.GetComponent<VocabularyPanel>();
            if (vocabPanel != null)
            {
                SetPrivateField(vocabPanel, "listContainer", content.transform);
                SetPrivateField(vocabPanel, "backButton", backBtn);
                SetPrivateField(vocabPanel, "emptyStatePanel", emptyState);
                SetPrivateField(vocabPanel, "emptyStateText", emptyText);
                SetPrivateField(vocabPanel, "wordCountLabel", wordCount);
            }
        }

        // ========== LABEL DETAIL VIEW (popup) ==========
        private void BuildLabelDetailView()
        {
            var detailView = GetOrCreateView("LabelDetailView");
            var detRT = EnsureRectTransform(detailView.gameObject);
            StretchFull(detRT);

            // Semi-transparent background overlay
            var overlay = detailView.gameObject.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

            // Card panel centered
            var card = CreatePanel(detailView, "Card", surfaceColor);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchoredPosition = Vector2.zero;
            cardRT.sizeDelta = new Vector2(600, 400);

            var cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(40, 40, 30, 30);
            cardLayout.spacing = 15;
            cardLayout.childAlignment = TextAnchor.MiddleCenter;
            cardLayout.childForceExpandHeight = false;

            // Translation (large)
            var transText = CreateText(card.transform, "TranslatedText", "Translation", 42, textColor);
            transText.fontStyle = FontStyles.Bold;
            transText.alignment = TextAlignmentOptions.Center;

            // Original word
            var origText = CreateText(card.transform, "OriginalText", "Original Word", 22, textSecondary);
            origText.alignment = TextAlignmentOptions.Center;

            // Language
            var langText = CreateText(card.transform, "LanguageText", "Language", 16, accentColor);
            langText.alignment = TextAlignmentOptions.Center;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(card.transform, false);
            spacer.AddComponent<RectTransform>();
            var spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.preferredHeight = 10;

            // Buttons row
            var btnRow = CreatePanel(card.transform, "ButtonRow", Color.clear);
            var btnRowLE = btnRow.AddComponent<LayoutElement>();
            btnRowLE.preferredHeight = 60;
            var btnRowLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnRowLayout.spacing = 20;
            btnRowLayout.childAlignment = TextAnchor.MiddleCenter;

            var listenBtn = CreateButton(btnRow.transform, "ListenBtn", "> Listen", 18, primaryColor);
            var saveBtn = CreateButton(btnRow.transform, "SaveBtn", "+ Save", 18, accentColor);
            var closeBtn = CreateButton(btnRow.transform, "CloseBtn", "X", 18, new Color(0.4f, 0.4f, 0.45f));

            // Saved indicator
            var savedIndicator = new GameObject("SavedIndicator");
            savedIndicator.transform.SetParent(card.transform, false);
            savedIndicator.AddComponent<RectTransform>();
            var savedText = CreateText(savedIndicator.transform, "SavedText", "Saved to vocabulary!", 16, accentColor);
            savedText.alignment = TextAlignmentOptions.Center;
            savedIndicator.SetActive(false);

            // Wire to UIManager
            if (uiManager != null)
            {
                SetPrivateField(uiManager, "labelDetailView", detailView.gameObject);
                SetPrivateField(uiManager, "detailOriginalText", origText);
                SetPrivateField(uiManager, "detailTranslatedText", transText);
                SetPrivateField(uiManager, "detailLanguageText", langText);
                SetPrivateField(uiManager, "detailListenButton", listenBtn);
                SetPrivateField(uiManager, "detailSaveButton", saveBtn);
                SetPrivateField(uiManager, "detailCloseButton", closeBtn);
                SetPrivateField(uiManager, "detailSavedIndicator", savedIndicator);
            }
        }

        // ========== SAVE CONFIRMATION ==========
        private void BuildSaveConfirmation()
        {
            var popup = CreatePanel(canvas.transform, "SaveConfirmation", new Color(0.1f, 0.7f, 0.4f, 0.9f));
            var popRT = popup.GetComponent<RectTransform>();
            popRT.anchorMin = new Vector2(0.1f, 0.85f);
            popRT.anchorMax = new Vector2(0.9f, 0.9f);
            popRT.offsetMin = Vector2.zero;
            popRT.offsetMax = Vector2.zero;

            var popText = CreateText(popup.transform, "Text", "Word saved!", 20, Color.white);
            popText.alignment = TextAlignmentOptions.Center;
            var popTextRT = popText.GetComponent<RectTransform>();
            StretchFull(popTextRT);

            popup.SetActive(false);

            if (uiManager != null)
            {
                SetPrivateField(uiManager, "saveConfirmationPopup", popup);
            }
        }

        // ========== HELPERS ==========

        private void CreateCrosshairLine(Transform parent, string name, Vector2 size, Vector2 position)
        {
            var line = new GameObject(name);
            line.transform.SetParent(parent, false);
            var rt = line.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
            var img = line.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.5f);
        }

        private void CreateCornerBracket(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float rotation)
        {
            var bracket = new GameObject(name);
            bracket.transform.SetParent(parent, false);
            var rt = bracket.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = new Vector2(40, 40);
            rt.anchoredPosition = Vector2.zero;

            var img = bracket.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.6f);

            bracket.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        private RectTransform EnsureRectTransform(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            return rt;
        }

        private void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private GameObject CreatePanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            var img = panel.AddComponent<Image>();
            img.color = color;
            return panel;
        }

        private GameObject CreatePanel(Component parent, string name, Color color)
        {
            return CreatePanel(parent.transform, name, color);
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private TextMeshProUGUI CreateText(Component parent, string name, string text, int fontSize, Color color)
        {
            return CreateText(parent.transform, name, text, fontSize, color);
        }

        private Button CreateButton(Transform parent, string name, string label, int fontSize, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            btn.colors = colors;

            // Label text
            var textObj = new GameObject("Label");
            textObj.transform.SetParent(go.transform, false);
            var textRT = textObj.AddComponent<RectTransform>();
            StretchFull(textRT);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning($"[Nomina] Could not find field '{fieldName}' on {target.GetType().Name}");
            }
        }
    }
}