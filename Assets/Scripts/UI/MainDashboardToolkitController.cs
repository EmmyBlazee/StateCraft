using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Map;
using StateCraft.Systems;

namespace StateCraft.UI
{
    // Run after all sub-dashboard controllers have initialized (default order = 0)
    [DefaultExecutionOrder(10)]
    public class MainDashboardToolkitController : MonoBehaviour
    {
        public static MainDashboardToolkitController Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _topBar;
        private VisualElement _panelContainer;
        private System.Collections.Generic.Dictionary<string, VisualElement> _panels = new System.Collections.Generic.Dictionary<string, VisualElement>();
        private VisualElement _bottomNavBar;
        
        // Navigation Buttons
        private Button _btnEconomy;
        private Button _btnMilitary;
        private Button _btnIntel;
        private Button _btnDiplomacy;
        private Button _btnEvents;
        private Button _btnSearch;
        private Button _btnTutorial;

        // Player Vitals
        private Label _treasuryLabel;
        private Label _stabilityLabel;
        private Label _readinessLabel;
        private Label _intelLabel;
        private Label _playerCountryLabel;
        private Image _playerFlag;
        private VisualElement _approvalFill;
        
        // Time Controls
        private Label _dateLabel;
        private Button _btnPause, _btn1x, _btn2x, _btn3x;
        private Button _btnNotificationHistory;
        private Button _btnSystemMenu;

        // System Menu Modal
        private VisualElement _systemMenuRoot;
        private Button _btnResume;
        private VisualElement _howToPlayRoot;
        
        // War Declared Modal
        private VisualElement _warDeclaredModal;
        private Label _warDeclaredMessage;
        private Button _btnAckWar;

        // Game Over Screen
        private VisualElement _gameOverRoot;
        private Label _gameOverTitle;
        private Label _gameOverSummary;
        private Label _gameOverStats;
        private Button _btnQuitToMenuFromGameOver;
        private Button _btnSaveScreenshot;

        // Event Log
        private VisualElement _eventLogPanel;
        private ScrollView _eventLogScrollView;

        // President Profile Modal
        private VisualElement _presidentProfileModal;
        private Label _presidentNameLabel;
        private Label _presidentCountryLabel;
        private Label _presidentCapitalLabel;
        private Label _presidentScenarioLabel;
        private Button _btnClosePresidentProfile;


        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;

            if (GetComponent<AIDiplomaticProposalController>() == null)
                gameObject.AddComponent<AIDiplomaticProposalController>();

            if (GetComponent<TutorialTourController>() == null)
                gameObject.AddComponent<TutorialTourController>();

            InitializeElements();
        }

        private void InitializeElements()
        {
            // Vitals
            _treasuryLabel = _root.Q<Label>("TreasuryValue");
            _stabilityLabel = _root.Q<Label>("StabilityValue");
            _readinessLabel = _root.Q<Label>("ReadinessValue");
            _intelLabel = _root.Q<Label>("IntelValue");
            _playerCountryLabel = _root.Q<Label>("PlayerCountryName");
            _playerFlag = _root.Q<Image>("PlayerFlag");
            _approvalFill = _root.Q<VisualElement>("ApprovalFill");

            // Time
            _dateLabel = _root.Q<Label>("CurrentDateValue");
            _btnPause = _root.Q<Button>("TimePause");
            _btn1x = _root.Q<Button>("Time1x");
            _btn2x = _root.Q<Button>("Time2x");
            _btn3x = _root.Q<Button>("Time3x");
            _btnNotificationHistory = _root.Q<Button>("BtnNotificationHistory");
            _btnSystemMenu = _root.Q<Button>("BtnSystemMenu");

            var systemMenuInstance = _root.Q<VisualElement>("SystemMenuInstance");
            if (systemMenuInstance != null) systemMenuInstance.style.display = DisplayStyle.None;

            _systemMenuRoot = _root.Q<VisualElement>("SystemMenuRoot");
            if (_systemMenuRoot != null)
            {
                _systemMenuRoot.style.display = DisplayStyle.None;
                
                _btnResume = _systemMenuRoot.Q<Button>("BtnResume");
                if (_btnResume != null) _btnResume.clicked += () => ToggleSystemMenu(false);

                var btnQuit = _systemMenuRoot.Q<Button>("BtnQuitToMenu");
                if (btnQuit != null) btnQuit.clicked += () =>
                {
                    GameContext.ReturningToMainMenu = true;
                    GameManager.Instance?.ResetAllCountriesToBaseline();
                    UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                };
                
                var inGameModal = _systemMenuRoot.Q<VisualElement>("InGameOverwriteSaveModal");
                var btnConfirmInGameSave = _systemMenuRoot.Q<Button>("BtnConfirmInGameSave");
                var btnCancelInGameSave = _systemMenuRoot.Q<Button>("BtnCancelInGameSave");

                if (inGameModal != null) inGameModal.style.display = DisplayStyle.None;

                void PerformSave()
                {
                    if (inGameModal != null) inGameModal.style.display = DisplayStyle.None;
                    GameManager.Instance?.SaveGame();
                    ToggleSystemMenu(false);
                }

                if (btnConfirmInGameSave != null) btnConfirmInGameSave.clicked += PerformSave;
                if (btnCancelInGameSave != null) btnCancelInGameSave.clicked += () => { if (inGameModal != null) inGameModal.style.display = DisplayStyle.None; };

                var btnSave = _systemMenuRoot.Q<Button>("BtnSave");
                if (btnSave != null) btnSave.clicked += () =>
                {
                    string path = GameManager.SaveFilePath;
                    if (System.IO.File.Exists(path))
                    {
                        if (inGameModal != null)
                        {
                            inGameModal.style.display = DisplayStyle.Flex;
                            return;
                        }
                    }
                    PerformSave();
                };

                // In-Game Settings Modal Wiring
                var settingsModal = _systemMenuRoot.Q<VisualElement>("InGameSettingsModal");
                var btnSettings = _systemMenuRoot.Q<Button>("BtnSettings");
                var btnCloseSettings = _systemMenuRoot.Q<Button>("BtnCloseInGameSettings");
                var btnSaveSettings = _systemMenuRoot.Q<Button>("BtnSaveInGameSettings");
                var btnResetSettings = _systemMenuRoot.Q<Button>("BtnResetInGameSettings");

                var sliderMusic = _systemMenuRoot.Q<Slider>("SliderInGameMusicVol");
                var lblMusic = _systemMenuRoot.Q<Label>("LblInGameMusicVol");
                var toggleFullscreen = _systemMenuRoot.Q<Toggle>("ToggleInGameFullscreen");
                var dropResolution = _systemMenuRoot.Q<DropdownField>("DropInGameResolution");

                if (settingsModal != null) settingsModal.style.display = DisplayStyle.None;

                // Load initial values from PlayerPrefs
                float savedMusic = PlayerPrefs.GetFloat("Settings_MusicVolume", 1.0f);
                bool savedFullscreen = PlayerPrefs.GetInt("Settings_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
                int savedResIndex = PlayerPrefs.GetInt("Settings_ResolutionIndex", 0);

                if (sliderMusic != null && lblMusic != null)
                {
                    sliderMusic.SetValueWithoutNotify(savedMusic * 100f);
                    lblMusic.text = $"{Mathf.RoundToInt(savedMusic * 100f)}%";
                    sliderMusic.RegisterValueChangedCallback(evt => {
                        lblMusic.text = $"{Mathf.RoundToInt(evt.newValue)}%";
                        float vol = evt.newValue / 100f;
                        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(vol);
                        PlayerPrefs.SetFloat("Settings_MusicVolume", vol);
                        PlayerPrefs.Save();
                    });
                }

                if (toggleFullscreen != null)
                {
                    toggleFullscreen.SetValueWithoutNotify(savedFullscreen);
                    toggleFullscreen.RegisterValueChangedCallback(evt => {
                        Screen.fullScreen = evt.newValue;
                        PlayerPrefs.SetInt("Settings_Fullscreen", evt.newValue ? 1 : 0);
                        PlayerPrefs.Save();
                    });
                }

                if (dropResolution != null)
                {
                    dropResolution.index = savedResIndex;
                    dropResolution.RegisterValueChangedCallback(evt => {
                        string res = evt.newValue;
                        if (res == "1920x1080") Screen.SetResolution(1920, 1080, Screen.fullScreen);
                        else if (res == "2560x1440") Screen.SetResolution(2560, 1440, Screen.fullScreen);
                        else if (res == "3840x2160") Screen.SetResolution(3840, 2160, Screen.fullScreen);
                        else if (res == "1280x720") Screen.SetResolution(1280, 720, Screen.fullScreen);
                        
                        int idx = dropResolution.choices.IndexOf(res);
                        if (idx >= 0)
                        {
                            PlayerPrefs.SetInt("Settings_ResolutionIndex", idx);
                            PlayerPrefs.Save();
                        }
                    });
                }

                void HideInGameSettings()
                {
                    if (settingsModal != null) settingsModal.style.display = DisplayStyle.None;
                }

                if (btnSettings != null) btnSettings.clicked += () =>
                {
                    if (settingsModal != null) settingsModal.style.display = DisplayStyle.Flex;
                };

                if (btnCloseSettings != null) btnCloseSettings.clicked += HideInGameSettings;
                if (btnSaveSettings != null) btnSaveSettings.clicked += HideInGameSettings;
                if (btnResetSettings != null) btnResetSettings.clicked += () =>
                {
                    if (sliderMusic != null) sliderMusic.value = 100;
                    if (toggleFullscreen != null) toggleFullscreen.value = true;
                    if (dropResolution != null) dropResolution.index = 0;
                };

                var btnHowToPlay = _systemMenuRoot.Q<Button>("BtnHowToPlay");
                if (btnHowToPlay != null) btnHowToPlay.clicked += () =>
                {
                    Debug.Log("[MainDashboard] BtnHowToPlay clicked! Triggering TutorialTourController.Instance.ShowPrompt()...");
                    ToggleSystemMenu(false);
                    if (TutorialTourController.Instance != null) TutorialTourController.Instance.ShowPrompt();
                };
            }

            // How To Play panel
            _howToPlayRoot = _root.Q<VisualElement>("HowToPlayRoot");
            if (_howToPlayRoot != null)
            {
                _howToPlayRoot.pickingMode = PickingMode.Position;
                var btnCloseHTP = _howToPlayRoot.Q<Button>("BtnCloseHTP");
                if (btnCloseHTP != null)
                {
                    btnCloseHTP.pickingMode = PickingMode.Position;
                    btnCloseHTP.clicked += () => ToggleHowToPlayModal(false);
                }

                void SwitchInGameHTPTab(int activeIdx)
                {
                    for (int i = 0; i <= 6; i++)
                    {
                        var page = _howToPlayRoot.Q<VisualElement>($"HTPPage{i}");
                        var tabBtn = _howToPlayRoot.Q<Button>($"HTPTab{i}");

                        if (page != null) page.style.display = (i == activeIdx) ? DisplayStyle.Flex : DisplayStyle.None;
                        if (tabBtn != null)
                        {
                            if (i == activeIdx) tabBtn.AddToClassList("htp-tab-active");
                            else tabBtn.RemoveFromClassList("htp-tab-active");
                        }
                    }
                }

                for (int i = 0; i <= 6; i++)
                {
                    int tabIdx = i;
                    var tabBtn = _howToPlayRoot.Q<Button>($"HTPTab{i}");
                    if (tabBtn != null)
                    {
                        tabBtn.pickingMode = PickingMode.Position;
                        tabBtn.clicked += () => SwitchInGameHTPTab(tabIdx);
                    }
                }
            }

            // President Profile Modal
            _presidentProfileModal = _root.Q<VisualElement>("PresidentProfileModal");
            if (_presidentProfileModal != null)
            {
                _presidentNameLabel = _presidentProfileModal.Q<Label>("PresidentNameLabel");
                _presidentCountryLabel = _presidentProfileModal.Q<Label>("PresidentCountryLabel");
                _presidentCapitalLabel = _presidentProfileModal.Q<Label>("PresidentCapitalLabel");
                _presidentScenarioLabel = _presidentProfileModal.Q<Label>("PresidentScenarioLabel");
                
                _btnClosePresidentProfile = _presidentProfileModal.Q<Button>("BtnClosePresidentProfile");
                if (_btnClosePresidentProfile != null)
                    _btnClosePresidentProfile.clicked += () => _presidentProfileModal.style.display = DisplayStyle.None;
            }

            if (_playerFlag != null)
            {
                _playerFlag.RegisterCallback<ClickEvent>(evt => ShowPresidentProfile());
            }

            _warDeclaredModal = _root.Q<VisualElement>("WarDeclaredModal");
            if (_warDeclaredModal != null)
            {
                _warDeclaredMessage = _warDeclaredModal.Q<Label>("WarDeclaredMessage");
                _btnAckWar = _warDeclaredModal.Q<Button>("BtnAckWar");
                if (_btnAckWar != null) _btnAckWar.clicked += () => 
                {
                    _warDeclaredModal.style.display = DisplayStyle.None;
                };
            }

            if (_btnPause != null) _btnPause.clicked += () => SetTimeSpeed(0);
            if (_btn1x != null) _btn1x.clicked += () => SetTimeSpeed(1);
            if (_btn2x != null) _btn2x.clicked += () => SetTimeSpeed(2);
            if (_btn3x != null) _btn3x.clicked += () => SetTimeSpeed(3);
            if (_btnNotificationHistory != null) _btnNotificationHistory.clicked += () => {
                if (NotificationManager.Instance != null) NotificationManager.Instance.ToggleHistoryModal();
            };
            if (_btnSystemMenu != null) _btnSystemMenu.clicked += () => ToggleSystemMenu(true);

            // Navigation (Modern Age Style - Bottom Bar)
            _topBar = _root.Q<VisualElement>("TopBar");
            _bottomNavBar = _root.Q<VisualElement>("BottomNavBar");
            _panelContainer = _root.Q<VisualElement>("PanelContainer");

            // Game Over Screen Setup
            _gameOverRoot = _root.Q<VisualElement>("GameOverRoot");
            if (_gameOverRoot != null)
            {
                _gameOverTitle = _gameOverRoot.Q<Label>("GameOverTitle");
                _gameOverSummary = _gameOverRoot.Q<Label>("GameOverSummary");
                _gameOverStats = _gameOverRoot.Q<Label>("GameOverStats");
                _btnQuitToMenuFromGameOver = _gameOverRoot.Q<Button>("BtnQuitToMenuFromGameOver");
                _btnSaveScreenshot = _gameOverRoot.Q<Button>("BtnSaveScreenshot");
                
                if (_btnQuitToMenuFromGameOver != null)
                {
                    _btnQuitToMenuFromGameOver.clicked += () => 
                    {
                        GameContext.ReturningToMainMenu = true;
                        GameManager.Instance?.ResetAllCountriesToBaseline();
                        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
                    };
                }

                if (_btnSaveScreenshot != null)
                {
                    _btnSaveScreenshot.clicked += () =>
                    {
                        string filename = $"StateCraft_GameOver_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
                        UnityEngine.ScreenCapture.CaptureScreenshot(filename);
                        if (NotificationManager.Instance != null)
                        {
                            NotificationManager.Instance.AddNotification($"Screenshot saved to {filename}", Color.green);
                        }
                    };
                }
            }

            // Register for Game Over
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += HandleGameOver;
            }
            
            _btnEconomy = _root.Q<Button>("NavEconomy");
            if (_btnEconomy != null) _btnEconomy.clicked += () => ShowPanel("Economy");

            _btnMilitary = _root.Q<Button>("NavMilitary");
            if (_btnMilitary != null) _btnMilitary.clicked += () => ShowPanel("Military");

            _btnIntel = _root.Q<Button>("NavIntelligence");
            if (_btnIntel != null) _btnIntel.clicked += () => ShowPanel("Intel");

            _btnDiplomacy = _root.Q<Button>("NavDiplomacy");
            if (_btnDiplomacy != null) _btnDiplomacy.clicked += () => ShowPanel("Diplomacy");

            _btnSearch = _root.Q<Button>("NavSearch");
            if (_btnSearch != null) _btnSearch.clicked += () => ShowPanel("Global");

            _btnEvents = _root.Q<Button>("NavEvents");
            if (_btnEvents != null) _btnEvents.clicked += () => ShowPanel("Events");

            _btnTutorial = _root.Q<Button>("NavTutorial");
            if (_btnTutorial != null) _btnTutorial.clicked += () => {
                if (TutorialTourController.Instance != null) TutorialTourController.Instance.ShowPrompt();
            };

            InjectNavImages();
        }

        private void InjectNavImages()
        {
            if (_btnEconomy != null) 
            {
                var icon = _btnEconomy.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/economy"));
            }
            if (_btnMilitary != null) 
            {
                var icon = _btnMilitary.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/Military"));
            }
            if (_btnIntel != null) 
            {
                var icon = _btnIntel.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/Intel"));
            }
            if (_btnDiplomacy != null) 
            {
                var icon = _btnDiplomacy.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/Diplomacy"));
            }
            if (_btnEvents != null) 
            {
                var icon = _btnEvents.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/EVENT"));
            }
            if (_btnSearch != null) 
            {
                var icon = _btnSearch.Q<Label>(className: "modern-nav-icon");
                if (icon != null) icon.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("NavIcons/Global"));
            }
        }

        private void Start()
        {
            if (_panelContainer == null) return;

            // Harvest sub-dashboards and parent them into the PanelContainer for SPA routing
            void InjectPanel(VisualElement p, string key)
            {
                if (p == null) return;
                p.RemoveFromHierarchy();
                // Override the standard-dashboard absolute position rules so it fills the container
                p.style.position = Position.Relative;
                p.style.left = StyleKeyword.Auto;
                p.style.top = StyleKeyword.Auto;
                p.style.right = StyleKeyword.Auto;
                p.style.bottom = StyleKeyword.Auto;
                p.style.width = Length.Percent(100);
                p.style.height = Length.Percent(100);
                p.style.maxWidth = StyleKeyword.None;
                p.style.alignSelf = Align.Auto;
                p.style.flexGrow = 1;
                _panelContainer.Add(p);
                _panels[key] = p;
            }

            // Helper: get a dashboard panel VisualElement from a controller's UIDocument.
            // Prefers rootVisualElement (already built), but falls back to CloneTree() directly
            // from the visualTreeAsset when rootVisualElement is empty — this happens when
            // UIDocument defers its build past our Start() call. The element will be injected
            // into MainDashboard's own panel container, so IntelUI's runtime panel is irrelevant.
            VisualElement GetDashboardPanel(MonoBehaviour ctrl, string panelName)
            {
                if (ctrl == null) return null;
                var doc = ctrl.GetComponent<UIDocument>();
                if (doc == null) return null;

                // First try: rootVisualElement (works if UIDocument has already built the tree)
                var panel = doc.rootVisualElement?.Q(panelName);

                // Fallback: clone directly from the visual tree asset
                if (panel == null && doc.visualTreeAsset != null)
                {
                    var container = new VisualElement();
                    doc.visualTreeAsset.CloneTree(container);
                    panel = container.Q(panelName);
                }

                if (panel == null)
                    Debug.LogWarning($"[Dashboard] '{panelName}' not found in {ctrl?.name}. Check the UXML element name.");
                return panel;
            }

            var ecoCtrl = FindObjectOfType<EconomyDashboardController>(true);
            InjectPanel(GetDashboardPanel(ecoCtrl, "EconomyDashboard"), "Economy");
            
            var milCtrl = FindObjectOfType<MilitaryDashboardController>(true);
            InjectPanel(GetDashboardPanel(milCtrl, "MilitaryDashboard"), "Military");
            
            var intCtrl = FindObjectOfType<IntelligenceDashboardController>(true);
            InjectPanel(GetDashboardPanel(intCtrl, "IntelligenceDashboard"), "Intel");

            var dipCtrl = FindObjectOfType<DiplomacyDashboardController>(true);
            InjectPanel(GetDashboardPanel(dipCtrl, "DiplomacyDashboard"), "Diplomacy");

            var evtCtrl = FindObjectOfType<EventDashboardController>(true);
            InjectPanel(GetDashboardPanel(evtCtrl, "EventDashboard"), "Events");

            var globalCtrl = FindObjectOfType<GlobalDashboardController>(true);
            InjectPanel(GetDashboardPanel(globalCtrl, "GlobalDashboard"), "Global");
            HideAllPanels();

            if (!GameContext.ShouldLoadSave)
            {
                StartCoroutine(DeferredNewGameTourCheck());
            }
        }

        private System.Collections.IEnumerator DeferredNewGameTourCheck()
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);
            if (TutorialTourController.Instance != null)
            {
                TutorialTourController.Instance.ShowPrompt();
            }
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }
        }

        
        public VisualElement GetPanelVisualElement(string key)
        {
            if (_panels != null && _panels.ContainsKey(key)) return _panels[key];
            return _panelContainer?.Q<VisualElement>($"{key}Dashboard");
        }

        public void ShowPanel(string name)
        {
            HideAllPanels();
            if (_systemMenuRoot != null) _systemMenuRoot.style.display = DisplayStyle.None;

            if (_panelContainer != null) _panelContainer.style.display = DisplayStyle.Flex;

            if (_panels.ContainsKey(name) && _panels[name] != null)
            {
                SetDashboardHeaderMode(true);
                _panels[name].style.display = DisplayStyle.Flex;
                
                // Trigger refresh if needed on the controller
                switch (name)
                {
                    case "Economy": FindObjectOfType<EconomyDashboardController>(true)?.Show(); break;
                    case "Military": FindObjectOfType<MilitaryDashboardController>(true)?.Show(); break;
                    case "Intel": FindObjectOfType<IntelligenceDashboardController>(true)?.Show(); break;
                    case "Diplomacy": FindObjectOfType<DiplomacyDashboardController>(true)?.Show(); break;
                    case "Events": FindObjectOfType<EventDashboardController>(true)?.Show(); break;
                    case "Global": FindObjectOfType<GlobalDashboardController>(true)?.Show(); break;
                }

                // Log any overflowing UI elements in this panel (runs after next layout pass)
                UIStateManager.LogOverflowingElements(_panels[name], name);
            }
            else
            {
                Debug.LogWarning($"[Dashboard] Panel '{name}' not found — check that the UXML is assigned.");
            }
            UpdateActiveNavButton(name);
        }


        public void HideAllPanels()
        {
            if (_panelContainer != null) _panelContainer.style.display = DisplayStyle.None;

            foreach (var panel in _panels.Values)
            {
                if (panel != null) panel.style.display = DisplayStyle.None;
            }

            SetDashboardHeaderMode(false);
            
            // Reset nav buttons
            _btnEconomy?.RemoveFromClassList("modern-nav-btn-active");
            _btnMilitary?.RemoveFromClassList("modern-nav-btn-active");
            _btnIntel?.RemoveFromClassList("modern-nav-btn-active");
            _btnDiplomacy?.RemoveFromClassList("modern-nav-btn-active");
            _btnEvents?.RemoveFromClassList("modern-nav-btn-active");
        }

        private void SetDashboardHeaderMode(bool panelOpen)
        {
            if (_topBar != null)
                _topBar.style.display = panelOpen ? DisplayStyle.None : DisplayStyle.Flex;

            if (_panelContainer != null)
                _panelContainer.style.top = panelOpen ? 0 : 65;
        }

        private void UpdateActiveNavButton(string panelName)
        {
            Button activeBtn = panelName switch
            {
                "Economy" => _btnEconomy,
                "Military" => _btnMilitary,
                "Intel" => _btnIntel,
                "Diplomacy" => _btnDiplomacy,
                "Events" => _btnEvents,
                _ => null
            };

            if (activeBtn != null)
            {
                activeBtn.AddToClassList("modern-nav-btn-active");
            }
        }

        private void ShowPresidentProfile()
        {
            if (_presidentProfileModal == null || GameManager.Instance == null || GameManager.Instance.playerCountry == null) return;
            
            var c = GameManager.Instance.playerCountry;
            if (_presidentNameLabel != null) _presidentNameLabel.text = $"PRESIDENT: {GameManager.Instance.playerName.ToUpper()}";
            if (_presidentCountryLabel != null) _presidentCountryLabel.text = $"NATION: {c.countryName.ToUpper()}";
            if (_presidentCapitalLabel != null) _presidentCapitalLabel.text = $"CAPITAL: {c.capitalCity.ToUpper()}";
            if (_presidentScenarioLabel != null) _presidentScenarioLabel.text = c.scenarioContext;
            
            _presidentProfileModal.style.display = DisplayStyle.Flex;
        }

        private void ToggleSystemMenu(bool show)
        {
            var menuInstance = _root?.Q<VisualElement>("SystemMenuInstance");
            if (menuInstance != null)
            {
                menuInstance.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                menuInstance.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
                if (show) menuInstance.BringToFront();
            }

            if (_systemMenuRoot != null)
            {
                _systemMenuRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                _systemMenuRoot.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
                if (show) _systemMenuRoot.BringToFront();
            }

            AudioManager.Instance?.PlayUIClick();

            if (show)
            {
                // Pause the game when opening the menu
                if (GameManager.Instance != null) SetTimeSpeed(0);
            }
            else
            {
                // Resume the game at 1x speed when unpausing / closing menu
                if (GameManager.Instance != null) SetTimeSpeed(1);
            }
        }

        private void ToggleHowToPlayModal(bool show)
        {
            var htpInstance = _root?.Q<VisualElement>("HowToPlayInstance");
            if (htpInstance != null)
            {
                htpInstance.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                htpInstance.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
                if (show) htpInstance.BringToFront();
            }

            if (_howToPlayRoot != null)
            {
                _howToPlayRoot.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                _howToPlayRoot.pickingMode = show ? PickingMode.Position : PickingMode.Ignore;
                if (show) _howToPlayRoot.BringToFront();
            }

            AudioManager.Instance?.PlayUIClick();
        }

        public void SetTimeSpeed(int level)
        {
            if (GameManager.Instance == null) return;

            // Update UI State
            _btnPause?.RemoveFromClassList("time-btn-active");
            _btn1x?.RemoveFromClassList("time-btn-active");
            _btn2x?.RemoveFromClassList("time-btn-active");
            _btn3x?.RemoveFromClassList("time-btn-active");

            switch (level)
            {
                case 0:
                    GameManager.Instance.isRealTime = false;
                    _btnPause?.AddToClassList("time-btn-active");
                    break;
                case 1:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 5f;
                    _btn1x?.AddToClassList("time-btn-active");
                    break;
                case 2:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 2.5f;
                    _btn2x?.AddToClassList("time-btn-active");
                    break;
                case 3:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 1f;
                    _btn3x?.AddToClassList("time-btn-active");
                    break;
            }
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
            {
                bool isCurrentlyOpen = _systemMenuRoot != null && _systemMenuRoot.style.display == DisplayStyle.Flex;
                ToggleSystemMenu(!isCurrentlyOpen);
            }

            if (GameManager.Instance == null) return;
            UpdatePlayerVitals();
            UpdateSimInfo();
        }

        private void UpdatePlayerVitals()
        {
            if (GameManager.Instance == null) return;

            var country = GameManager.Instance.playerCountry;
            
            // Treasury display (using local currency)
            if (_treasuryLabel != null) 
            {
                string symbol = (country != null && !string.IsNullOrEmpty(country.currencySymbol)) ? country.currencySymbol : "$";
                _treasuryLabel.text = FormatCurrency(GameManager.Instance.playerBudget, symbol);
            }
            if (country != null)
            {
                if (_stabilityLabel != null) _stabilityLabel.text = $"{country.stability:N0}%  |  POP: {FormatPopulation(country.population)} {FormatPopulationSigned(country.populationChangeLastTurn)}";
                
                string symbol = !string.IsNullOrEmpty(country.currencySymbol) ? country.currencySymbol : "$";

                var gdpLbl = _root?.Q<Label>("ValGDP");
                if (gdpLbl != null) gdpLbl.text = FormatCurrency(country.gdp * country.exchangeRate, symbol);

                var inflLbl = _root?.Q<Label>("ValInflation");
                if (inflLbl != null) inflLbl.text = $"{country.inflationRate:F1}%";

                var debtLbl = _root?.Q<Label>("ValTotalDebt");
                if (debtLbl != null) 
                {
                    debtLbl.text = FormatCurrency(country.debt * country.exchangeRate, symbol);
                }

                string displayName = country.countryName.ToUpper();
                if (displayName.EndsWith(" COUNTRY")) displayName = displayName.Replace(" COUNTRY", "");
                if (_playerCountryLabel != null) _playerCountryLabel.text = displayName;
                
                if (_playerFlag != null)
                {
                    string flagName = country.countryName.Replace(" ", "_");
                    Sprite flagSprite = Resources.Load<Sprite>($"Flags/{flagName}");
                    if (flagSprite != null && _playerFlag.sprite != flagSprite) 
                    {
                        _playerFlag.sprite = flagSprite;
                    }
                }
                
                // Update Approval Bar
                if (_approvalFill != null)
                    _approvalFill.style.width = Length.Percent(country.stability);
            }
            else
            {
                if (_stabilityLabel != null) _stabilityLabel.text = "--%";
                if (_playerCountryLabel != null) _playerCountryLabel.text = "SELECT COUNTRY";
                if (_playerFlag != null) _playerFlag.sprite = null;
                if (_approvalFill != null) _approvalFill.style.width = Length.Percent(0);
            }
            
            // Readiness linked to security/military power (hypothetical)
            if (_readinessLabel != null) _readinessLabel.text = $"{70 + Random.Range(-2, 2)}%"; 
        }

        private string FormatCurrency(float valueInBillions, string symbol) => UiFormat.Money(valueInBillions, symbol);

        private static string FormatPopulation(long pop)
        {
            if (pop >= 1_000_000_000) return $"{pop / 1_000_000_000f:F1}B";
            if (pop >= 1_000_000)     return $"{pop / 1_000_000f:F1}M";
            if (pop >= 1_000)         return $"{pop / 1_000f:F0}K";
            return pop.ToString("N0");
        }

        private static string FormatPopulationSigned(long pop)
        {
            if (pop == 0) return "(0)";
            string sign = pop > 0 ? "+" : "-";
            long absPop = System.Math.Abs(pop);
            return $"({sign}{FormatPopulation(absPop)})";
        }

        private void UpdateSimInfo()
        {
            if (GameManager.Instance?.TurnSystem == null) return;

            string currentDate = GameManager.Instance.TurnSystem.CurrentDate;

            if (_dateLabel != null)
                _dateLabel.text = currentDate;

            // Update date across all integrated dashboards
            _root.Query<Label>("DateMain").ForEach(lbl => lbl.text = currentDate);
            _root.Query<Label>("HdrDate").ForEach(lbl => lbl.text = currentDate);

            // ── Election countdown in header ──
            var country = GameManager.Instance.playerCountry;
            if (country == null) return;

            int currentTurn = GameManager.Instance.TurnSystem.CurrentTurn;
            int electionEvery = 48;
            int monthsUntilElection = electionEvery - (currentTurn % electionEvery);
            if (monthsUntilElection == electionEvery) monthsUntilElection = 0; // just had one

            // Calculate approval for display
            float stability     = country.stability;
            float econScore     = country.income > country.spending ? 30f : 15f;
            float prestigeScore = country.globalPrestige * 0.15f;
            float inflation     = Mathf.Clamp(country.inflationRate * 0.15f, 0f, 10f);
            float approvalEst   = Mathf.Clamp((stability * 0.40f) + econScore + prestigeScore - inflation, 0f, 100f);

            // Update IntelLabel repurposed as election/approval bar
            // (or add a dedicated label if IntelValue exists)
            if (_intelLabel != null)
            {
                string electionText;
                if (monthsUntilElection == 0)
                    electionText = "ELECTION THIS MONTH";
                else if (monthsUntilElection <= 3)
                    electionText = $"ELECTION: {monthsUntilElection}mo  |  APPROVAL: {approvalEst:F0}%";
                else if (monthsUntilElection <= 12)
                    electionText = $"ELECTION: {monthsUntilElection}mo  |  {approvalEst:F0}%";
                else
                    electionText = $"ELECTION: {monthsUntilElection / 12}y {monthsUntilElection % 12}m  |  {approvalEst:F0}%";

                _intelLabel.text = electionText;

                // Colour: red if losing, amber if marginal, green if safe
                _intelLabel.style.color = approvalEst >= 60f ? new Color(0.3f, 0.9f, 0.45f) :
                                          approvalEst >= 50f ? new Color(1f, 0.82f, 0.2f) :
                                                               new Color(1f, 0.35f, 0.35f);
            }

            // ── Pending event badge on NavEvents button ──
            var btnEvents = _root?.Q<Button>("NavEvents");
            if (btnEvents != null)
            {
                var evCountry = GameManager.Instance?.playerCountry;
                var evSys     = GameManager.Instance?.EventSystem;
                int pending   = (evCountry != null && evSys != null)
                    ? evSys.GetPendingEvents(evCountry).Count : 0;
                
                if (evCountry != null && GameManager.Instance?.TurnSystem != null) {
                    bool budgetOverdue = (GameManager.Instance.TurnSystem.CurrentTurn - evCountry.lastBudgetApprovalTurn) >= 3;
                    bool hasElectionResult = !string.IsNullOrEmpty(evCountry.latestElectionResult);
                    if (budgetOverdue) pending++;
                    if (hasElectionResult) pending++;
                }

                var badge = btnEvents.Q<Label>("EventBadge");
                if (badge != null)
                {
                    if (pending > 0)
                    {
                        badge.style.display = DisplayStyle.Flex;
                        badge.text = pending > 99 ? "99+" : pending.ToString();
                        
                        btnEvents.style.backgroundColor = new Color(0.6f, 0.0f, 0.0f, 0.7f);
                        btnEvents.style.borderTopColor    = new Color(0.9f, 0.1f, 0.1f);
                        btnEvents.style.borderBottomColor = new Color(0.9f, 0.1f, 0.1f);
                        btnEvents.style.borderLeftColor   = new Color(0.9f, 0.1f, 0.1f);
                        btnEvents.style.borderRightColor  = new Color(0.9f, 0.1f, 0.1f);
                        btnEvents.style.borderTopWidth    = 1;
                        btnEvents.style.borderBottomWidth = 1;
                        btnEvents.style.borderLeftWidth   = 1;
                        btnEvents.style.borderRightWidth  = 1;
                    }
                    else
                    {
                        badge.style.display = DisplayStyle.None;
                        
                        btnEvents.style.backgroundColor   = StyleKeyword.Null;
                        btnEvents.style.borderTopWidth    = 0;
                        btnEvents.style.borderBottomWidth = 0;
                        btnEvents.style.borderLeftWidth   = 0;
                        btnEvents.style.borderRightWidth  = 0;
                    }
                }
            }
        }

        private void OpenCountrySelector()
        {
            var selector = FindObjectOfType<CountrySelectorToolkitController>();
            if (selector != null) selector.Show();
        }

        public void ShowWarDeclaredModal(StateCraft.Data.CountryDataSO aggressor)
        {
            if (_warDeclaredModal != null && _warDeclaredMessage != null)
            {
                _warDeclaredMessage.text = $"{aggressor.countryName.ToUpper()} HAS FORMALLY DECLARED WAR UPON OUR NATION.";
                _warDeclaredModal.style.display = DisplayStyle.Flex;
                AudioManager.Instance?.PlayWarAlert();
                
                // Force pause when war is declared
                if (GameManager.Instance != null)
                {
                    SetTimeSpeed(0);
                }
            }
            else
            {
                Debug.LogWarning($"[UI] War Declared by {aggressor.countryName}! Modal UI not wired up properly.");
            }
        }

        private void HandleGameOver(bool isVictory, string title, string summary, string stats)
        {
            if (_gameOverRoot == null) return;
            
            _gameOverRoot.style.display = DisplayStyle.Flex;
            if (_gameOverTitle != null)
            {
                _gameOverTitle.text = title;
                _gameOverTitle.style.color = isVictory ? new Color(0.2f, 0.8f, 0.2f) : new Color(1f, 0.2f, 0.2f);
                _gameOverTitle.style.textShadow = isVictory 
                    ? new StyleTextShadow(new TextShadow { color = new Color(0.2f, 0.8f, 0.2f, 0.5f), blurRadius = 10 })
                    : new StyleTextShadow(new TextShadow { color = new Color(1f, 0.2f, 0.2f, 0.5f), blurRadius = 10 });
            }
            if (_gameOverSummary != null) _gameOverSummary.text = summary;
            if (_gameOverStats != null) _gameOverStats.text = stats;
        }
    }
}
