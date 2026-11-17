using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Data;

namespace StateCraft.Core
{
    [RequireComponent(typeof(VideoPlayer))]
    public class SplashController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("RawImage GameObject for the video texture.")]
        public UnityEngine.UI.RawImage videoDisplay;

        [Tooltip("CanvasGroup on the video layer — faded out after video.")]
        public CanvasGroup videoCanvasGroup;

        [Tooltip("CanvasGroup on the splash image — faded in after video.")]
        public CanvasGroup splashCanvasGroup;

        [Tooltip("UIDocument hosting SplashScreen.uxml (loading indicator).")]
        public UIDocument splashUIDocument;

        [Tooltip("UIDocument hosting MainMenu.uxml (buttons + name modal).")]
        public UIDocument menuUIDocument;

        [Tooltip("UIDocument hosting CountrySelector.uxml.")]
        public UIDocument selectorUIDocument;

        [Header("Settings")]
        public string mainSceneName = "SampleScene";
        public float fadeDuration    = 0.8f;
        public float minLoaderTime   = 2.5f;

        [Header("Country Data")]
        [Tooltip("Drag all CountryDataSO assets here, OR leave empty to auto-load via AssetDatabase in-editor.")]
        public CountryDataSO[] countryDataList;

        private VisualElement _dot1, _dot2, _dot3;
        private Label         _loadingLabel;
        private ProgressBar   _loadingBar;
        private float         _dotTimer;
        private int           _dotFrame;
        private const float   DOT_INTERVAL = 0.4f;

        private VisualElement _menuRoot;
        private VisualElement _nameModal;
        private TextField     _nameInput;
        private Button        _btnNewGame, _btnLoadGame, _btnHowToPlay, _btnSettings, _btnQuit;
        private Button        _btnConfirmName, _btnCancelName;

        private VisualElement _selectorRoot;
        private ScrollView    _countryList;
        private TextField     _selectorSearch;

        private VideoPlayer  _videoPlayer;
        private bool         _videoFinished;
        private bool         _countryChosen;
        private bool         _launchLoad;
        private string       _chosenCountryName;

private void Awake()
        {
            Application.runInBackground = true;
            _videoPlayer = GetComponent<VideoPlayer>();
            if (GetComponent<StateCraft.UI.TutorialTourController>() == null)
                gameObject.AddComponent<StateCraft.UI.TutorialTourController>();
        }

        private IEnumerator Start()
        {
            Debug.Log("[Splash] START — ReturningToMainMenu=" + GameContext.ReturningToMainMenu);

            if (videoCanvasGroup)  { videoCanvasGroup.alpha = 1f;  videoCanvasGroup.gameObject.SetActive(true); }
            if (splashCanvasGroup) { splashCanvasGroup.alpha = 0f; }

            Debug.Log("[Splash] videoCanvasGroup=" + (videoCanvasGroup != null)
                    + " splashCanvasGroup=" + (splashCanvasGroup != null)
                    + " splashUIDoc=" + (splashUIDocument != null)
                    + " menuUIDoc=" + (menuUIDocument != null)
                    + " selectorUIDoc=" + (selectorUIDocument != null));

            HideAllOverlays();

            if (!GameContext.ReturningToMainMenu)
            {
                Debug.Log("[Splash] Phase 1: Playing intro video. clip=" + (_videoPlayer.clip != null ? _videoPlayer.clip.name : "NULL"));

                _videoFinished = false;
                _videoPlayer.loopPointReached += _ => { Debug.Log("[Splash] loopPointReached fired."); _videoFinished = true; };
                _videoPlayer.prepareCompleted  += _ => { Debug.Log("[Splash] prepareCompleted — calling Play()."); _videoPlayer.Play(); };
                
                _videoPlayer.Prepare();
                _videoPlayer.Play();
                Debug.Log("[Splash] Prepare() and Play() called. Waiting for video to start...");

                float prepWait = 0f;
                while (!_videoPlayer.isPlaying && prepWait < 5f)
                {
                    prepWait += Time.deltaTime;
                    yield return null;
                }
                Debug.Log("[Splash] Video isPlaying=" + _videoPlayer.isPlaying + " after " + prepWait.ToString("F1") + "s prep wait.");

                float maxTime = _videoPlayer.clip != null ? (float)_videoPlayer.clip.length + 2f : 20f;
                Debug.Log("[Splash] Video maxTime=" + maxTime.ToString("F1") + "s");
                float vidElapsed = 0f;
                while (!_videoFinished && vidElapsed < maxTime)
                {
                    vidElapsed += Time.deltaTime;
                    if (prepWait < 5f && !_videoPlayer.isPlaying && vidElapsed > 2f)
                    {
                        Debug.Log("[Splash] Fallback: Video stopped playing at " + vidElapsed.ToString("F1") + "s — finishing phase.");
                        _videoFinished = true;
                    }
                    yield return null;
                }

                Debug.Log("[Splash] Video phase done. _videoFinished=" + _videoFinished + " elapsed=" + vidElapsed.ToString("F1") + "s");
                _videoPlayer.Stop();
            }
            else
            {
                Debug.Log("[Splash] Phase 1: SKIPPED (returning from game).");
            }

            Debug.Log("[Splash] Phase 2: CrossFade to splash image starting.");
            yield return StartCoroutine(CrossFadeToSplash());
            Debug.Log("[Splash] Phase 2: CrossFade done. Showing loader.");
            ShowLoader();
            Debug.Log("[Splash] Phase 2: LoadingLabel=" + (_loadingLabel != null) + " LoadingBar=" + (_loadingBar != null));
            yield return StartCoroutine(RunLoader());
            Debug.Log("[Splash] Phase 2: Loader complete.");

            Debug.Log("[Splash] Phase 3: Showing main menu.");
            HideLoader();
            BindAndShowMenu();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Debug.Log($"[Splash-UI-Diag] Mouse 0 Pressed at screen pos: {UnityEngine.Input.mousePosition}");
            }

            if (_nameModal != null && _nameModal.style.display == DisplayStyle.Flex)
            {
                if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Escape))
                {
                    OnCancelName();
                    return;
                }
                return;
            }

            if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space))
            {
                Debug.LogWarning("[Splash] SPACEBAR PRESSED — FORCING SKIP TO MENU!");
                StopAllCoroutines();
                if (videoCanvasGroup != null) videoCanvasGroup.alpha = 0;
                if (splashCanvasGroup != null) splashCanvasGroup.alpha = 1;
                HideLoader();
                BindAndShowMenu();
            }

            if (_dot1 == null) return;
            _dotTimer += Time.deltaTime;
            if (_dotTimer >= DOT_INTERVAL)
            {
                _dotTimer = 0f;
                _dotFrame = (_dotFrame + 1) % 4;
                SetDotOpacity(_dot1, _dotFrame >= 1);
                SetDotOpacity(_dot2, _dotFrame >= 2);
                SetDotOpacity(_dot3, _dotFrame >= 3);
            }
        }

private void HideAllOverlays()
        {
            if (splashUIDocument   != null) splashUIDocument.rootVisualElement.style.opacity   = 0;
            else Debug.LogWarning("[Splash] splashUIDocument is NULL — loader won't show!");

            if (menuUIDocument     != null) menuUIDocument.rootVisualElement.style.display      = DisplayStyle.None;
            else Debug.LogWarning("[Splash] menuUIDocument is NULL — main menu won't show!");

            if (selectorUIDocument != null) selectorUIDocument.rootVisualElement.style.display  = DisplayStyle.None;
            else Debug.LogWarning("[Splash] selectorUIDocument is NULL — country selector won't show!");
        }

        private void ShowLoader()
        {
            if (splashUIDocument == null) return;
            var root = splashUIDocument.rootVisualElement;
            _loadingLabel = root.Q<Label>("LoadingLabel");
            _loadingBar   = root.Q<ProgressBar>("LoadingBar");
            _dot1         = root.Q<VisualElement>("Dot1");
            _dot2         = root.Q<VisualElement>("Dot2");
            _dot3         = root.Q<VisualElement>("Dot3");
            root.style.opacity = 1;
        }

        private IEnumerator RunLoader()
        {
            float elapsed = 0f;
            while (elapsed < minLoaderTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / minLoaderTime);
                if (_loadingBar  != null) _loadingBar.value  = t * 100f;
                if (_loadingLabel != null && t < 1f)
                    _loadingLabel.text = $"LOADING  {Mathf.RoundToInt(t * 100f)}%";
                yield return null;
            }
            if (_loadingLabel != null) _loadingLabel.text = "READY";
            yield return new WaitForSeconds(0.4f);
        }

        private void HideLoader()
        {
            if (splashUIDocument != null)
            {
                splashUIDocument.rootVisualElement.style.display = DisplayStyle.None;
                splashUIDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
            }

            Debug.Log("[Splash] Loader hidden and display set to None so it does not block mouse clicks.");
        }

        private VisualElement _settingsModal;
        private Button _btnCloseSettings, _btnSaveSettings, _btnResetSettings;
        private VisualElement _howToPlayOverlay;
        private Button _btnCloseHTP;
        private Label _lblNoSave;

        private bool HasSaveFile()
        {
            string savePath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                "StateCraft", "saves", "savegame.json");
            return System.IO.File.Exists(savePath);
        }

        private void BindAndShowMenu()
        {
            if (menuUIDocument == null) { Debug.LogError("[Splash] menuUIDocument is not assigned! Menu cannot show."); return; }
            if (menuUIDocument.panelSettings == null) { Debug.LogError("<color=red>[Splash] CRITICAL: menuUIDocument is missing Panel Settings! The menu will NOT render.</color>"); }
            
            menuUIDocument.sortingOrder = 100;
            if (selectorUIDocument != null) selectorUIDocument.sortingOrder = 101;

            var root = menuUIDocument.rootVisualElement;
            Debug.Log("[Splash] BindAndShowMenu — rootVisualElement=" + (root != null));

            root.pickingMode = PickingMode.Position;

            _menuRoot       = root.Q<VisualElement>("MainMenuRoot");
            if (_menuRoot != null) _menuRoot.pickingMode = PickingMode.Position;

            _nameModal      = root.Q<VisualElement>("PlayerNameModal");
            _nameInput      = root.Q<TextField>("InputPlayerName");
            _btnNewGame     = root.Q<Button>("BtnNewGame");
            _btnLoadGame    = root.Q<Button>("BtnLoadGame");
            _btnHowToPlay   = root.Q<Button>("BtnHowToPlay");
            _btnSettings    = root.Q<Button>("BtnSettings");
            _btnQuit        = root.Q<Button>("BtnQuit");
            _btnConfirmName = root.Q<Button>("BtnConfirmName");
            _btnCancelName  = root.Q<Button>("BtnCancelName");
            _lblNoSave      = root.Q<Label>("LblNoSave");

            _settingsModal    = root.Q<VisualElement>("SettingsModal");
            _btnCloseSettings = root.Q<Button>("BtnCloseSettings");
            _btnSaveSettings  = root.Q<Button>("BtnSaveSettings");
            _btnResetSettings = root.Q<Button>("BtnResetSettings");

            _howToPlayOverlay = root.Q<VisualElement>("HowToPlayOverlay");
            if (_howToPlayOverlay != null)
            {
                _howToPlayOverlay.style.display = DisplayStyle.None;
                _howToPlayOverlay.pickingMode = PickingMode.Position;
                var htpPanel = _howToPlayOverlay.Q<VisualElement>("HowToPlayRoot");
                if (htpPanel != null)
                {
                    htpPanel.style.display = DisplayStyle.None;
                    htpPanel.pickingMode = PickingMode.Position;
                }

                _btnCloseHTP = _howToPlayOverlay.Q<Button>("BtnCloseHTP");
                if (_btnCloseHTP != null)
                {
                    _btnCloseHTP.pickingMode = PickingMode.Position;
                    _btnCloseHTP.clicked -= HideHowToPlay;
                    _btnCloseHTP.clicked += HideHowToPlay;
                }
                
                for (int i = 0; i <= 5; i++)
                {
                    int tabIdx = i;
                    var tabBtn = _howToPlayOverlay.Q<Button>($"HTPTab{i}");
                    if (tabBtn != null)
                    {
                        tabBtn.pickingMode = PickingMode.Position;
                        tabBtn.clicked -= () => SwitchHTPTab(tabIdx);
                        tabBtn.clicked += () => SwitchHTPTab(tabIdx);
                    }
                }
            }

            bool saveExists = HasSaveFile();
            if (_btnLoadGame != null)
            {
                if (!saveExists)
                {
                    _btnLoadGame.SetEnabled(false);
                    _btnLoadGame.style.opacity = 0.4f;
                }
                else
                {
                    _btnLoadGame.SetEnabled(true);
                    _btnLoadGame.style.opacity = 1f;
                }
            }

            _overwriteSaveModal   = root.Q<VisualElement>("OverwriteSaveModal");
            _btnConfirmOverwrite = root.Q<Button>("BtnConfirmOverwrite");
            _btnCancelOverwrite  = root.Q<Button>("BtnCancelOverwrite");

            if (_overwriteSaveModal != null) _overwriteSaveModal.style.display = DisplayStyle.None;
            if (_btnConfirmOverwrite != null) _btnConfirmOverwrite.clicked += ProceedToNameModal;
            if (_btnCancelOverwrite != null) _btnCancelOverwrite.clicked += () => _overwriteSaveModal.style.display = DisplayStyle.None;

            if (_nameModal != null) _nameModal.style.display = DisplayStyle.None;
            if (_settingsModal != null) _settingsModal.style.display = DisplayStyle.None;

            if (_btnNewGame      != null) _btnNewGame.clicked      += OnNewGame;
            if (_btnLoadGame     != null) _btnLoadGame.clicked     += OnLoadGame;
            if (_btnHowToPlay    != null) _btnHowToPlay.clicked    += OnHowToPlay;
            if (_btnSettings     != null) _btnSettings.clicked     += OnSettings;
            if (_btnQuit         != null) _btnQuit.clicked         += OnQuit;
            if (_btnConfirmName  != null) _btnConfirmName.clicked  += OnConfirmName;
            if (_btnCancelName   != null) _btnCancelName.clicked   += OnCancelName;

            if (_btnCloseSettings != null) _btnCloseSettings.clicked += HideSettings;
            if (_btnSaveSettings != null) _btnSaveSettings.clicked += HideSettings;

            var sliderMusic = root.Q<Slider>("SliderMusicVol");
            var lblMusic = root.Q<Label>("LblMusicVol");
            if (sliderMusic != null && lblMusic != null)
            {
                float savedMusic = PlayerPrefs.GetFloat("Settings_MusicVolume", 1.0f);
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

            var toggleFullscreen = root.Q<Toggle>("ToggleFullscreen");
            var dropResolution = root.Q<DropdownField>("DropResolution");
            
            bool savedFullscreen = PlayerPrefs.GetInt("Settings_Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            int savedResIndex = PlayerPrefs.GetInt("Settings_ResolutionIndex", 0);

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

            if (_btnResetSettings != null) _btnResetSettings.clicked += () =>
            {
                if (sliderMusic != null) sliderMusic.value = 100;
                if (toggleFullscreen != null) toggleFullscreen.value = true;
                if (dropResolution != null) dropResolution.index = 0;
            };

            root.style.display = DisplayStyle.Flex;
            Debug.Log("[Splash] Menu display set to Flex — menu should now be visible.");
        }

        private VisualElement _overwriteSaveModal;
        private Button _btnConfirmOverwrite, _btnCancelOverwrite;

        private void OnNewGame()
        {
            if (HasSaveFile())
            {
                Debug.Log("[Splash] Existing save found. Prompting overwrite modal.");
                if (_overwriteSaveModal != null)
                {
                    _overwriteSaveModal.style.display = DisplayStyle.Flex;
                    return;
                }
            }

            ProceedToNameModal();
        }

        private void ProceedToNameModal()
        {
            if (_overwriteSaveModal != null) _overwriteSaveModal.style.display = DisplayStyle.None;
            Debug.Log("[Splash] Showing player name modal.");
            if (_nameModal != null) _nameModal.style.display = DisplayStyle.Flex;
            if (_nameInput != null) { _nameInput.value = "Leader"; _nameInput.Focus(); }
        }

        private void OnConfirmName()
        {
            string name = (_nameInput != null && !string.IsNullOrWhiteSpace(_nameInput.value))
                ? _nameInput.value.Trim() : "Leader";
            
            Debug.Log($"[Splash] OnConfirmName clicked. PlayerName set to '{name}'. Hiding modal and showing country selector.");

            GameContext.PlayerName    = name;
            GameContext.SelectedCountryName = "";
            GameContext.ShouldLoadSave = false;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerCountry = null;
                GameManager.Instance.playerName = name;
            }

            if (_nameModal != null) _nameModal.style.display = DisplayStyle.None;
            if (_menuRoot  != null) _menuRoot.style.display  = DisplayStyle.None;

            if (menuUIDocument != null)
            {
                var menuRoot = menuUIDocument.rootVisualElement;
                menuRoot.pickingMode = PickingMode.Ignore;
                menuRoot.Query<VisualElement>().ForEach(ve => ve.pickingMode = PickingMode.Ignore);
            }

            ShowCountrySelector();
        }

        private void OnCancelName()
        {
            Debug.Log("[Splash] OnCancelName clicked. Hiding player name modal.");
            if (_nameModal != null) _nameModal.style.display = DisplayStyle.None;
        }

        private void OnLoadGame()
        {
            if (!HasSaveFile())
            {
                if (_lblNoSave != null) _lblNoSave.style.display = DisplayStyle.Flex;
                return;
            }

            Debug.Log("[Splash] Save file found. Loading scene: " + mainSceneName);
            GameContext.ShouldLoadSave = true;
            GameContext.ReturningToMainMenu = false;
            StartCoroutine(LoadSceneWithLoader(mainSceneName, "war"));
        }

        private void OnHowToPlay()
        {
            Debug.Log("[Splash] How To Play clicked.");
            if (StateCraft.UI.TutorialTourController.Instance != null)
            {
                StateCraft.UI.TutorialTourController.Instance.StartTour();
                return;
            }

            if (_howToPlayOverlay != null)
            {
                _howToPlayOverlay.style.display = DisplayStyle.Flex;
                var htpPanel = _howToPlayOverlay.Q<VisualElement>("HowToPlayRoot");
                if (htpPanel != null) htpPanel.style.display = DisplayStyle.Flex;
                SwitchHTPTab(0);
            }
        }

        private void HideHowToPlay()
        {
            if (_howToPlayOverlay != null)
            {
                _howToPlayOverlay.style.display = DisplayStyle.None;
                var htpPanel = _howToPlayOverlay.Q<VisualElement>("HowToPlayRoot");
                if (htpPanel != null) htpPanel.style.display = DisplayStyle.None;
            }
        }

        private void SwitchHTPTab(int activeIdx)
        {
            if (_howToPlayOverlay == null) return;

            for (int i = 0; i <= 5; i++)
            {
                var page = _howToPlayOverlay.Q<VisualElement>($"HTPPage{i}");
                var tabBtn = _howToPlayOverlay.Q<Button>($"HTPTab{i}");

                if (page != null)
                    page.style.display = (i == activeIdx) ? DisplayStyle.Flex : DisplayStyle.None;

                if (tabBtn != null)
                {
                    if (i == activeIdx) tabBtn.AddToClassList("htp-tab-active");
                    else tabBtn.RemoveFromClassList("htp-tab-active");
                }
            }
        }

        private void OnSettings()
        {
            Debug.Log("[Splash] Settings clicked.");
            if (_settingsModal != null) _settingsModal.style.display = DisplayStyle.Flex;
        }

        private void HideSettings()
        {
            if (_settingsModal != null) _settingsModal.style.display = DisplayStyle.None;
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

private void ShowCountrySelector()
        {
            Debug.Log("[Splash] ShowCountrySelector called.");
            if (selectorUIDocument == null) { Debug.LogError("[Splash] selectorUIDocument not assigned!"); return; }

            var root = selectorUIDocument.rootVisualElement;
            _selectorRoot   = root.Q<VisualElement>("SelectorRoot");
            _countryList    = root.Q<ScrollView>("CountryList");
            _selectorSearch = root.Q<TextField>("SearchInput");

            Debug.Log("[Splash] Country Selector elements: SelectorRoot=" + (_selectorRoot != null) + " CountryList=" + (_countryList != null) + " SearchInput=" + (_selectorSearch != null));

            var closeBtn = root.Q<Button>("CloseSelector");
            if (closeBtn != null) 
            {
                Debug.Log("[Splash] Found CloseSelector button. Wiring click event.");
                closeBtn.clicked += HideCountrySelector;
            }

            if (_selectorSearch != null)
                _selectorSearch.RegisterValueChangedCallback(evt => PopulateSelectorList(evt.newValue));

            Debug.Log("[Splash] Populating country list...");
            PopulateSelectorList("");

            root.style.display = DisplayStyle.Flex;
            if (_selectorRoot != null)
            {
                _selectorRoot.style.opacity = 1;
                _selectorRoot.style.visibility = Visibility.Visible;
                _selectorRoot.pickingMode = PickingMode.Position;
                Debug.Log("[Splash] Selector root style set to visible and picking mode position.");
            }
        }

        private void HideCountrySelector()
        {
            Debug.Log("[Splash] HideCountrySelector called. Hiding selector.");
            if (selectorUIDocument == null) return;
            var root = selectorUIDocument.rootVisualElement;
            root.style.display = DisplayStyle.None;
            root.pickingMode   = PickingMode.Ignore;

            if (menuUIDocument != null)
            {
                var menuRoot = menuUIDocument.rootVisualElement;
                menuRoot.pickingMode = PickingMode.Position;
                menuRoot.Query<VisualElement>().ForEach(ve => ve.pickingMode = PickingMode.Position);
            }

            if (_menuRoot != null) _menuRoot.style.display = DisplayStyle.Flex;
        }

        private CountryDataSO[] LoadAllCountries()
        {
            if (countryDataList != null && countryDataList.Length > 0)
            {
                Debug.Log($"[Splash] Loading {countryDataList.Length} countries from inspector list.");
                return countryDataList;
            }

#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:CountryDataSO", new[] { "Assets/Data/Countries" });
            var list  = new List<CountryDataSO>();
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<CountryDataSO>(path);
                if (asset != null) list.Add(asset);
            }
            Debug.Log($"[Splash] Editor: loaded {list.Count} countries from Assets/Data/Countries.");
            return list.OrderBy(c => c.countryName).ToArray();
#else
            var built = Resources.LoadAll<CountryDataSO>("Countries");
            Debug.Log($"[Splash] Build: loaded {built.Length} countries from Resources/Countries.");
            return built.OrderBy(c => c.countryName).ToArray();
#endif
        }

        private void PopulateSelectorList(string filter)
        {
            if (_countryList == null) return;
            _countryList.Clear();

            var all = LoadAllCountries();
            if (all == null || all.Length == 0)
            {
                Debug.LogWarning("[Splash] No CountryDataSO found! Assign them to SplashController.countryDataList in the Inspector, or ensure assets are in Assets/Data/Countries/.");
                return;
            }

            int added = 0;
            foreach (var c in all)
            {
                if (c == null) continue;
                if (!string.IsNullOrEmpty(filter) && !c.countryName.ToLower().Contains(filter.ToLower()))
                    continue;
                _countryList.Add(CreateSelectorRow(c));
                added++;
            }
            Debug.Log($"[Splash] Populated selector with {added} countries (filter='{filter}').");
        }

        private VisualElement CreateSelectorRow(CountryDataSO country)
        {
            var row = new Button();
            row.name = $"Row_{country.countryName}";
            row.AddToClassList("selector-row");
            row.style.flexDirection    = FlexDirection.Row;
            row.style.alignItems       = Align.Center;
            row.style.paddingLeft      = 15;
            row.style.paddingRight     = 15;
            row.style.height           = 50;
            row.style.backgroundColor  = new Color(1f, 1f, 1f, 0.03f);
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1f, 1f, 1f, 0.05f);
            row.style.borderTopWidth = row.style.borderLeftWidth = row.style.borderRightWidth = 0;
            row.pickingMode            = PickingMode.Position;

            var flag = new Image();
            flag.style.width = 30; flag.style.height = 20; flag.style.marginRight = 15;
            flag.pickingMode = PickingMode.Ignore;
            if (country.countryFlag != null) flag.sprite = country.countryFlag;
            row.Add(flag);

            var label = new Label(country.countryName.ToUpper());
            label.style.color    = Color.white;
            label.style.fontSize = 14;
            label.pickingMode    = PickingMode.Ignore;
            row.Add(label);

            row.clicked += () =>
            {
                GameContext.SelectedCountryName = country.countryName;
                GameContext.ReturningToMainMenu  = false;
                HideCountrySelector();
                StartCoroutine(LoadSceneWithLoader(mainSceneName, "peace"));
            };

            return row;
        }

private IEnumerator LoadSceneWithLoader(string sceneName, string musicMood = "peace")
        {
            if (menuUIDocument != null)
                menuUIDocument.rootVisualElement.style.display = DisplayStyle.None;
            if (selectorUIDocument != null)
                selectorUIDocument.rootVisualElement.style.display = DisplayStyle.None;

            if (splashCanvasGroup != null)
            {
                splashCanvasGroup.alpha = 1f;
                splashCanvasGroup.gameObject.SetActive(true);
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMoodTrack(musicMood);

            ShowLoader();
            if (_loadingLabel != null) _loadingLabel.text = "LOADING  0%";
            if (_loadingBar   != null) _loadingBar.value  = 0f;
            yield return null;

            var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            float displayProgress = 0f;
            while (op.progress < 0.9f || displayProgress < 100f)
            {
                float target = (op.progress < 0.9f)
                    ? op.progress / 0.9f * 95f
                    : 100f;

                displayProgress = Mathf.MoveTowards(displayProgress, target, Time.deltaTime * 80f);

                if (_loadingBar   != null) _loadingBar.value  = displayProgress;
                if (_loadingLabel != null) _loadingLabel.text = $"LOADING  {Mathf.RoundToInt(displayProgress)}%";

                if (displayProgress >= 100f) break;
                yield return null;
            }

            if (_loadingLabel != null) _loadingLabel.text = "READY";
            if (_loadingBar   != null) _loadingBar.value  = 100f;
            yield return new WaitForSeconds(0.4f);

            op.allowSceneActivation = true;
        }

private IEnumerator CrossFadeToSplash()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                if (videoCanvasGroup)  videoCanvasGroup.alpha  = 1f - t;
                if (splashCanvasGroup) splashCanvasGroup.alpha = t;
                yield return null;
            }
            if (videoCanvasGroup)  { videoCanvasGroup.alpha = 0f;  videoCanvasGroup.gameObject.SetActive(false); }
            if (splashCanvasGroup)   splashCanvasGroup.alpha = 1f;
        }

        private static void SetDotOpacity(VisualElement dot, bool active)
        {
            if (dot == null) return;
            dot.style.opacity = active ? 1f : 0.2f;
        }
    }
}
