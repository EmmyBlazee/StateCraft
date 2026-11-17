using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Map;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public class MainDashboardToolkitController : MonoBehaviour
    {
        public static MainDashboardToolkitController Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _topBar;
        private VisualElement _panelContainer;
        private System.Collections.Generic.Dictionary<string, VisualElement> _panels = new System.Collections.Generic.Dictionary<string, VisualElement>();
        
        private Button _btnEconomy;
        private Button _btnMilitary;
        private Button _btnIntel;
        private Button _btnDiplomacy;
        private Button _btnEvents;
        private Button _btnSearch;

        private Label _treasuryLabel;
        private Label _stabilityLabel;
        private Label _readinessLabel;
        private Label _intelLabel;
        private Label _playerCountryLabel;
        private Image _playerFlag;
        private VisualElement _approvalFill;
        
        private Label _dateLabel;
        private Button _btnPause, _btn1x, _btn2x, _btn3x;

private VisualElement _eventLogPanel;
        private ScrollView _eventLogScrollView;

private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void InitializeElements()
        {
            _treasuryLabel = _root.Q<Label>("TreasuryValue");
            _stabilityLabel = _root.Q<Label>("StabilityValue");
            _readinessLabel = _root.Q<Label>("ReadinessValue");
            _intelLabel = _root.Q<Label>("IntelValue");
            _playerCountryLabel = _root.Q<Label>("PlayerCountryName");
            _playerFlag = _root.Q<Image>("PlayerFlag");
            _approvalFill = _root.Q<VisualElement>("ApprovalFill");

            _dateLabel = _root.Q<Label>("CurrentDateValue");
            _btnPause = _root.Q<Button>("TimePause");
            _btn1x = _root.Q<Button>("Time1x");
            _btn2x = _root.Q<Button>("Time2x");
            _btn3x = _root.Q<Button>("Time3x");

            if (_btnPause != null) _btnPause.clicked += () => SetTimeSpeed(0);
            if (_btn1x != null) _btn1x.clicked += () => SetTimeSpeed(1);
            if (_btn2x != null) _btn2x.clicked += () => SetTimeSpeed(2);
            if (_btn3x != null) _btn3x.clicked += () => SetTimeSpeed(3);

            _topBar = _root.Q<VisualElement>("TopBar");
            _panelContainer = _root.Q<VisualElement>("PanelContainer");
            
            _btnEconomy = _root.Q<Button>("NavEconomy");
            if (_btnEconomy != null) _btnEconomy.clicked += () => ShowPanel("Economy");

            _btnMilitary = _root.Q<Button>("NavMilitary");
            if (_btnMilitary != null) _btnMilitary.clicked += () => ShowPanel("Military");

            _btnIntel = _root.Q<Button>("NavIntelligence");
            if (_btnIntel != null) _btnIntel.clicked += () => ShowPanel("Intel");

            _btnDiplomacy = _root.Q<Button>("NavDiplomacy");
            if (_btnDiplomacy != null) _btnDiplomacy.clicked += () => ShowPanel("Diplomacy");

            _btnSearch = _root.Q<Button>("NavSearch");
            if (_btnSearch != null) _btnSearch.clicked += OpenCountrySelector;

            _btnEvents = _root.Q<Button>("NavEvents");
            if (_btnEvents != null) _btnEvents.clicked += () => ShowPanel("Events");
        }

        private void Start()
        {
            if (_panelContainer == null) return;

            void InjectPanel(VisualElement p, string key)
            {
                if (p == null) return;
                p.RemoveFromHierarchy();
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

            var ecoCtrl = FindObjectOfType<EconomyDashboardController>();
            if (ecoCtrl?.GetComponent<UIDocument>() != null)
                InjectPanel(ecoCtrl.GetComponent<UIDocument>().rootVisualElement.Q("EconomyDashboard"), "Economy");
            
            var milCtrl = FindObjectOfType<MilitaryDashboardController>();
            if (milCtrl?.GetComponent<UIDocument>() != null)
                InjectPanel(milCtrl.GetComponent<UIDocument>().rootVisualElement.Q("MilitaryDashboard"), "Military");
            
            var intCtrl = FindObjectOfType<IntelligenceDashboardController>();
            if (intCtrl?.GetComponent<UIDocument>() != null)
                InjectPanel(intCtrl.GetComponent<UIDocument>().rootVisualElement.Q("IntelligenceDashboard"), "Intel");

            var dipCtrl = FindObjectOfType<DiplomacyDashboardController>();
            if (dipCtrl?.GetComponent<UIDocument>() != null)
                InjectPanel(dipCtrl.GetComponent<UIDocument>().rootVisualElement.Q("DiplomacyDashboard"), "Diplomacy");

            var evtCtrl = FindObjectOfType<EventDashboardController>();
            if (evtCtrl?.GetComponent<UIDocument>() != null)
                InjectPanel(evtCtrl.GetComponent<UIDocument>().rootVisualElement.Q("EventDashboard"), "Events");

            HideAllPanels();
        }

        public void ShowPanel(string name)
        {
            HideAllPanels();
            if (_panels.ContainsKey(name) && _panels[name] != null)
            {
                SetDashboardHeaderMode(true);
                _panels[name].style.display = DisplayStyle.Flex;
                
                switch (name)
                {
                    case "Economy": FindObjectOfType<EconomyDashboardController>()?.Show(); break;
                    case "Military": FindObjectOfType<MilitaryDashboardController>()?.Show(); break;
                    case "Intel": FindObjectOfType<IntelligenceDashboardController>()?.Show(); break;
                    case "Diplomacy": FindObjectOfType<DiplomacyDashboardController>()?.Show(); break;
                    case "Events": FindObjectOfType<EventDashboardController>()?.Show(); break;
                }
            }
            UpdateActiveNavButton(name);
        }

        public void HideAllPanels()
        {
            foreach (var panel in _panels.Values)
            {
                if (panel != null) panel.style.display = DisplayStyle.None;
            }

            SetDashboardHeaderMode(false);
            
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

        private void SetTimeSpeed(int level)
        {
            if (GameManager.Instance == null) return;

            _btnPause.RemoveFromClassList("time-btn-active");
            _btn1x.RemoveFromClassList("time-btn-active");
            _btn2x.RemoveFromClassList("time-btn-active");
            _btn3x.RemoveFromClassList("time-btn-active");

            switch (level)
            {
                case 0:
                    GameManager.Instance.isRealTime = false;
                    _btnPause.AddToClassList("time-btn-active");
                    break;
                case 1:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 5f;
                    _btn1x.AddToClassList("time-btn-active");
                    break;
                case 2:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 2.5f;
                    _btn2x.AddToClassList("time-btn-active");
                    break;
                case 3:
                    GameManager.Instance.isRealTime = true;
                    GameManager.Instance.turnDuration = 1f;
                    _btn3x.AddToClassList("time-btn-active");
                    break;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            UpdatePlayerVitals();
            UpdateSimInfo();
        }

        private void UpdatePlayerVitals()
        {
            if (GameManager.Instance == null) return;

            var country = GameManager.Instance.playerCountry;
            
            if (_treasuryLabel != null) 
            {
                string symbol = (country != null && !string.IsNullOrEmpty(country.currencySymbol)) ? country.currencySymbol : "$";
                _treasuryLabel.text = FormatCurrency(GameManager.Instance.playerBudget, symbol);
            }
            if (country != null)
            {
                if (_stabilityLabel != null) _stabilityLabel.text = $"{country.stability:N0}%  |  POP: {FormatPopulation(country.population)} {FormatPopulationSigned(country.populationChangeLastTurn)}";
                
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

            if (_dateLabel != null)
                _dateLabel.text = GameManager.Instance.TurnSystem.CurrentDate;

            var country = GameManager.Instance.playerCountry;
            if (country == null) return;

            int currentTurn = GameManager.Instance.TurnSystem.CurrentTurn;
            int electionEvery = 48;
            int monthsUntilElection = electionEvery - (currentTurn % electionEvery);
            if (monthsUntilElection == electionEvery) monthsUntilElection = 0;

            float stability     = country.stability;
            float econScore     = country.income > country.spending ? 30f : 15f;
            float prestigeScore = country.globalPrestige * 0.15f;
            float inflation     = Mathf.Clamp(country.inflationRate * 0.15f, 0f, 10f);
            float approvalEst   = Mathf.Clamp((stability * 0.40f) + econScore + prestigeScore - inflation, 0f, 100f);

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

                _intelLabel.style.color = approvalEst >= 60f ? new Color(0.3f, 0.9f, 0.45f) :
                                          approvalEst >= 50f ? new Color(1f, 0.82f, 0.2f) :
                                                               new Color(1f, 0.35f, 0.35f);
            }

            var btnEvents = _root?.Q<Button>("NavEvents");
            if (btnEvents != null)
            {
                var evCountry = GameManager.Instance?.playerCountry;
                var evSys     = GameManager.Instance?.EventSystem;
                int pending   = (evCountry != null && evSys != null)
                    ? evSys.GetPendingEvents(evCountry).Count : 0;

                var iconLbl = btnEvents.Q<Label>(className: "modern-nav-icon");
                if (pending > 0)
                {
                    btnEvents.style.backgroundColor = new Color(0.55f, 0.08f, 0.55f, 0.7f);
                    btnEvents.style.borderTopColor    = new Color(0.9f, 0.4f, 0.9f);
                    btnEvents.style.borderBottomColor = new Color(0.9f, 0.4f, 0.9f);
                    btnEvents.style.borderLeftColor   = new Color(0.9f, 0.4f, 0.9f);
                    btnEvents.style.borderRightColor  = new Color(0.9f, 0.4f, 0.9f);
                    btnEvents.style.borderTopWidth    = 1;
                    btnEvents.style.borderBottomWidth = 1;
                    btnEvents.style.borderLeftWidth   = 1;
                    btnEvents.style.borderRightWidth  = 1;
                    if (iconLbl != null) iconLbl.text = $"📰({pending})";
                }
                else
                {
                    btnEvents.style.backgroundColor   = StyleKeyword.Null;
                    btnEvents.style.borderTopWidth    = 0;
                    btnEvents.style.borderBottomWidth = 0;
                    btnEvents.style.borderLeftWidth   = 0;
                    btnEvents.style.borderRightWidth  = 0;
                    if (iconLbl != null) iconLbl.text = "📰";
                }
            }
        }

        private void OpenCountrySelector()
        {
            var selector = FindObjectOfType<CountrySelectorToolkitController>();
            if (selector != null) selector.Show();
        }
    }
}
