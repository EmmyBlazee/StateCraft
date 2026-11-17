using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class TutorialStep
    {
        public string moduleTag;
        public string stepTitle;
        public string stepIcon;
        public string description;
        public string targetPanelName;
        public string targetSubTabName;
        public string targetElementQuery;

        public TutorialStep(string tag, string title, string icon, string desc, string panel = "", string subTab = "", string targetQuery = "")
        {
            moduleTag = tag;
            stepTitle = title;
            stepIcon = icon;
            description = desc;
            targetPanelName = panel;
            targetSubTabName = subTab;
            targetElementQuery = targetQuery;
        }
    }

    public class TutorialTourController : MonoBehaviour
    {
        private static TutorialTourController _instance;
        public static TutorialTourController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TutorialTourController>();
                    if (_instance == null)
                    {
                        var go = new GameObject("TutorialTourController");
                        _instance = go.AddComponent<TutorialTourController>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private UIDocument _uiDoc;
        private VisualElement _root;
        private VisualElement _tourRoot;
        private VisualElement _spotlightFrame;
        private VisualElement _tourCard;
        private VisualElement _promptModal;

        private Label _lblModuleTag;
        private Label _lblStepCounter;
        private Label _lblStepIcon;
        private Label _lblStepTitle;
        private Label _lblStepDesc;

        private Button _btnPrev;
        private Button _btnNext;
        private Button _btnSkip;

        private Button _btnPromptStart;
        private Button _btnPromptSkip;

        private int _currentStepIndex = 0;
        private bool _isTourActive = false;
        public bool IsTourActive => _isTourActive;

        private List<TutorialStep> _steps = new List<TutorialStep>();

        private void Awake()
        {
            Debug.Log("[TutorialTour] Awake() called on GameObject: " + gameObject.name);
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            InitializeSteps();
        }

        private void Start()
        {
            StartCoroutine(DeferredInitializeUI());
        }

        private IEnumerator DeferredInitializeUI()
        {
            yield return new WaitForSeconds(0.2f);
            EnsureBound();
            if (!_isTourActive)
            {
                SetTourVisibility(false);
                HidePrompt();
            }
        }

        public bool EnsureBound()
        {
            _uiDoc = GetComponent<UIDocument>();

            if (_uiDoc == null || _uiDoc.visualTreeAsset == null)
            {
                var tourAsset = Resources.Load<VisualTreeAsset>("Layouts/TutorialTour");
#if UNITY_EDITOR
                if (tourAsset == null)
                {
                    tourAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/Layouts/TutorialTour.uxml");
                }
#endif
                if (tourAsset != null)
                {
                    if (_uiDoc == null) _uiDoc = gameObject.AddComponent<UIDocument>();
                    _uiDoc.visualTreeAsset = tourAsset;
                }
            }

            if (_uiDoc != null)
            {
                _uiDoc.sortingOrder = 999999;
                _root = _uiDoc.rootVisualElement;
                if (_root != null)
                {
                    _tourRoot = _root.Q<VisualElement>("TutorialTourRoot");
                }
            }

            if (_tourRoot == null)
            {
                var docs = FindObjectsOfType<UIDocument>();
                foreach (var doc in docs)
                {
                    if (doc == null || doc.rootVisualElement == null || doc == _uiDoc) continue;
                    var foundRoot = doc.rootVisualElement.Q<VisualElement>("TutorialTourRoot");
                    if (foundRoot != null)
                    {
                        _uiDoc = doc;
                        _uiDoc.sortingOrder = 999999;
                        _root = doc.rootVisualElement;
                        _tourRoot = foundRoot;
                        break;
                    }
                }
            }

            if (_tourRoot == null) return false;

            _spotlightFrame = _tourRoot.Q<VisualElement>("TourSpotlightFrame");
            _tourCard = _tourRoot.Q<VisualElement>("TourCard");
            _promptModal = _tourRoot.Q<VisualElement>("TourPromptModal");

            _lblModuleTag = _tourRoot.Q<Label>("TourModuleTag");
            _lblStepCounter = _tourRoot.Q<Label>("TourStepCounter");
            _lblStepIcon = _tourRoot.Q<Label>("TourStepIcon");
            _lblStepTitle = _tourRoot.Q<Label>("TourStepTitle");
            _lblStepDesc = _tourRoot.Q<Label>("TourStepDesc");

            _btnPrev = _tourRoot.Q<Button>("BtnTourPrev");
            _btnNext = _tourRoot.Q<Button>("BtnTourNext");
            _btnSkip = _tourRoot.Q<Button>("BtnTourSkip");

            _btnPromptStart = _tourRoot.Q<Button>("BtnPromptStart");
            _btnPromptSkip = _tourRoot.Q<Button>("BtnPromptSkip");

            if (_btnPrev != null) { _btnPrev.clicked -= OnPrevClicked; _btnPrev.clicked += OnPrevClicked; }
            if (_btnNext != null) { _btnNext.clicked -= OnNextClicked; _btnNext.clicked += OnNextClicked; }
            if (_btnSkip != null) { _btnSkip.clicked -= EndTour; _btnSkip.clicked += EndTour; }

            if (_btnPromptStart != null) { _btnPromptStart.clicked -= OnPromptStartClicked; _btnPromptStart.clicked += OnPromptStartClicked; }
            if (_btnPromptSkip != null) { _btnPromptSkip.clicked -= OnPromptSkipClicked; _btnPromptSkip.clicked += OnPromptSkipClicked; }

            return true;
        }

        private void InitializeSteps()
        {
            _steps = new List<TutorialStep>
            {
                new TutorialStep("COMMAND ESSENTIALS", "Welcome Commander", "",
                    "Welcome to StateCraft! You are the Head of State. Guide your nation through economic growth, military power, intelligence black ops, and international diplomacy.", "", "", ""),

                new TutorialStep("COMMAND ESSENTIALS", "Top Bar Telemetry", "",
                    "Your top bar displays vital state metrics: your Country Flag, Presidential Profile, Election Countdown, Treasury Balance, and National GDP.", "", "", "TopBar"),

                new TutorialStep("COMMAND ESSENTIALS", "Presidential Profile & Country Details", "",
                    "Clicking your Country Flag opens your Presidential Profile. View your Head of State details, capital city, election countdown, stability, and national vitals.", "", "", "PlayerFlag"),

                new TutorialStep("COMMAND ESSENTIALS", "Time & Game Controls", "",
                    "Control the pace of history using the Play (||), Fast-Forward (1x/2x/3x), and Pause buttons. Pause anytime to plan your moves.", "", "", "TimeControls"),

                new TutorialStep("COMMAND ESSENTIALS", "Command Notification", "",
                    "Critical state briefs, intelligence warnings, election reports, and diplomatic offers arrive in your Command Inbox. Always check notifications!", "", "", "BtnNotificationHistory"),

                new TutorialStep("COMMAND ESSENTIALS", "System Menu & Settings", "",
                    "Access Game Settings, Save your progress, Load existing sessions, or adjust Audio & Graphics preferences anytime via the top-right Gear Icon.", "", "", "BtnSystemMenu"),

                new TutorialStep("COMMAND ESSENTIALS", "Dashboard Navigation Bar", "",
                    "Use the bottom bar to navigate between your main government ministries: Economy, Military, Intelligence, Diplomacy, Events, and Global Power.", "", "", "BottomNavBar"),

                new TutorialStep("ECONOMY & TAXES", "Economy Overview", "",
                    "The Economy Dashboard tracks your GDP, annual growth rate, inflation, and public approval. A strong economy funds all national ambitions.", "Economy", "TabDashboard", "TabDashboard"),

                new TutorialStep("ECONOMY & TAXES", "Budget Allocations", "",
                    "Distribute state revenue across Military, Education, Healthcare, Infrastructure, Energy, and Welfare. Balanced spending improves public stability.", "Economy", "TabInfrastructure", "TabInfrastructure"),

                new TutorialStep("ECONOMY & TAXES", "Taxation Policy", "",
                    "Adjust Income Tax, Corporate Tax, VAT, and Customs Duties. Higher taxes boost government revenue but lower business growth and voter satisfaction.", "Economy", "TabProduction", "TabProduction"),

                new TutorialStep("ECONOMY & TAXES", "Industrial Growth", "",
                    "Develop national Oil & Gas, Mining, Agriculture, Manufacturing, and Tech sectors to multiply monthly revenue and create jobs.", "Economy", "TabIndustry", "TabIndustry"),

                new TutorialStep("ECONOMY & TAXES", "Infrastructure & National Projects", "",
                    "Commission mega infrastructure projects, energy grids, transport hubs, and industrial parks to boost national productivity.", "Economy", "TabConstruction", "TabConstruction"),

                new TutorialStep("MILITARY & DEFENSE", "Armed Forces Readiness", "",
                    "Manage Army, Navy, Air Force, and Rapid Deployment Forces. Keep readiness high and select your defense posture (Peacetime vs Mobilized).", "Military", "TabForces", "TabForces"),

                new TutorialStep("MILITARY & DEFENSE", "Recruitment & Personnel", "",
                    "Enlist active duty personnel and train special forces. Keep your garrisons staffed to maintain regional security.", "Military", "TabRecruit", "TabRecruit"),

                new TutorialStep("MILITARY & DEFENSE", "Weapons Arsenal & Stockpiles", "",
                    "Inspect standing inventory, service readiness, and equipment condition across Army, Navy, and Air Force stockpiles.", "Military", "WTabArsenal", "WTabArsenal"),

                new TutorialStep("MILITARY & DEFENSE", "Global Weapons Procurement", "",
                    "Procure main battle tanks, fighter jets, guided naval frigates, and service rifles from global arms suppliers to outfit your troops.", "Military", "WTabProcurement", "WTabProcurement"),

                new TutorialStep("MILITARY & DEFENSE", "Domestic Weapons Production", "",
                    "Manufacture assault rifles, armored personnel carriers, and artillery in your domestic defense factories.", "Military", "WTabProduction", "WTabProduction"),

                new TutorialStep("MILITARY & DEFENSE", "Bases & Defense Factories", "",
                    "Construct and expand military garrisons, airbases, and domestic defense plants to boost equipment production and troop capacity.", "Military", "TabBases", "TabBases"),

                new TutorialStep("MILITARY & DEFENSE", "Military Operations", "",
                    "Plan and execute combat operations, border patrols, joint peacekeeping missions, and tactical force deployments.", "Military", "TabOperations", "TabOperations"),

                new TutorialStep("MILITARY & DEFENSE", "Military Logistics & Munitions", "",
                    "Manage supply chains, fuel reserves, ammunition stockpiles, and equipment maintenance to sustain military readiness.", "Military", "TabLogistics", "TabLogistics"),

                new TutorialStep("MILITARY & DEFENSE", "Defense Budget & Upkeep", "",
                    "Monitor standing military expenses, equipment maintenance, and base upkeep to ensure military spending remains fiscally sustainable.", "Military", "TabBudget", "TabBudget"),

                new TutorialStep("INTELLIGENCE & BLACK OPS", "How Intelligence Works", "",
                    "Your intelligence agency runs automatically in the background. Field agents gather SIGINT, run surveillance, and execute counter-intel sweeps every month without your direct input. Your job as President is high-level oversight — not micromanagement.", "Intel", "TabOverview", "TabOverview"),

                new TutorialStep("INTELLIGENCE & BLACK OPS", "Drone Fleet & Surveillance Budget", "",
                    "Your key decisions here are: approve drone purchases to extend surveillance range, and set the monthly intelligence budget. A bigger budget means more active operatives, better cover, and lower exposure risk.", "Intel", "TabOverview", "TabOverview"),

                new TutorialStep("INTELLIGENCE & BLACK OPS", "Covert Operations", "",
                    "You can personally order black ops missions — political espionage, industrial sabotage, and covert destabilization against rival nations. These are high-risk, high-reward decisions that require your direct authorization.", "Intel", "TabCovertOps", "TabCovertOps"),

                new TutorialStep("INTELLIGENCE & BLACK OPS", "Threat Assessment", "",
                    "The agency automatically compiles threat reports on domestic instability, foreign incursions, and regional hotspots. Check this tab regularly so you stay informed — critical threats pause the game and notify you directly.", "Intel", "TabThreats", "TabThreats"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "Diplomatic Overview", "",
                    "Review international relations, diplomatic event logs, active treaties, and your nation's global reputation.", "Diplomacy", "TabOverview", "TabOverview"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "Bilateral Relations", "",
                    "Manage bilateral relations with individual foreign powers, track alliance status, and inspect actionable ambassador briefs.", "Diplomacy", "TabBilateral", "TabBilateral"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "Geopolitical Factions & Treaties", "",
                    "Form geopolitical alliance factions (e.g. Western Coalition, BRICS Bloc, African Defense League) or negotiate non-aggression pacts and mutual assistance treaties.", "Diplomacy", "TabTreaties", "TabTreaties"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "International Organizations", "",
                    "Participate in regional blocks (AU, ECOWAS, UN, OPEC) and vote on multilateral economic and defense resolutions.", "Diplomacy", "TabOrganizations", "TabOrganizations"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "Foreign Embassies", "",
                    "Establish overseas diplomatic missions, expand ambassadorial staffing, and manage international embassy budgets.", "Diplomacy", "TabEmbassies", "TabEmbassies"),

                new TutorialStep("DIPLOMACY & ALLIANCES", "Trade & Natural Resources", "",
                    "Negotiate strategic resource exchange agreements, export crude oil, minerals, and food, and import advanced industrial machinery.", "Diplomacy", "TabTrade", "TabTrade"),

                new TutorialStep("STRATEGIC EVENTS", "National Events & Crises", "",
                    "Monitor ongoing national developments, geopolitical crises, economic shocks, and presidential decrees in your Events Dashboard.", "Events", "", "NavEvents"),

                new TutorialStep("GLOBAL POWER", "Geopolitics & Superpowers", "",
                    "Compare your nation's GDP, military firepower rank, inflation rates, and geopolitical standing against all world powers.", "Global", "", "NavSearch"),

                new TutorialStep("VICTORY & GOVERNANCE", "Command Ready!", "",
                    "You are fully briefed! Survive elections, maintain state stability, expand national influence, and turn your country into a global superpower!", "", "", "")
            };
        }

        private void SetTourVisibility(bool visible)
        {
            if (_tourRoot != null)
            {
                _tourRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                _tourRoot.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
                if (_tourRoot.parent != null)
                {
                    _tourRoot.parent.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                    _tourRoot.parent.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
                    if (visible) _tourRoot.parent.BringToFront();
                }
                if (visible) _tourRoot.BringToFront();
            }
        }

        public void ShowPrompt()
        {
            EnsureBound();
            SetTourVisibility(true);
            if (_promptModal != null) _promptModal.style.display = DisplayStyle.Flex;
            if (_tourCard != null) _tourCard.style.display = DisplayStyle.None;
            if (_spotlightFrame != null) _spotlightFrame.style.display = DisplayStyle.None;
        }

        public void HidePrompt()
        {
            if (_promptModal != null) _promptModal.style.display = DisplayStyle.None;
        }

        public void StartTour()
        {
            EnsureBound();
            _isTourActive = true;
            SetTourVisibility(true);
            HidePrompt();

            MainDashboardToolkitController.Instance?.SetTimeSpeed(0);

            ShowStep(0);
        }

        private void OnPromptStartClicked()
        {
            HidePrompt();
            StartTour();
        }

        private void OnPromptSkipClicked()
        {
            PlayerPrefs.SetInt("HasCompletedTutorialTour", 1);
            PlayerPrefs.Save();
            HidePrompt();
        }

        public void ShowStep(int index)
        {
            if (index < 0 || index >= _steps.Count) return;
            _currentStepIndex = index;
            var step = _steps[index];
            Debug.Log($"[TutorialTour] Step {index + 1}/{_steps.Count}: '{step.stepTitle}' — Panel: '{step.targetPanelName}', Query: '{step.targetElementQuery}'");

            if (_lblModuleTag != null) _lblModuleTag.text = step.moduleTag;
            if (_lblStepCounter != null) _lblStepCounter.text = $"STEP {index + 1} OF {_steps.Count}";
            if (_lblStepIcon != null)
            {
                _lblStepIcon.text = step.stepIcon;
                _lblStepIcon.style.display = string.IsNullOrEmpty(step.stepIcon) ? DisplayStyle.None : DisplayStyle.Flex;
            }
            if (_lblStepTitle != null) _lblStepTitle.text = step.stepTitle;
            if (_lblStepDesc != null) _lblStepDesc.text = step.description;

            if (_btnPrev != null) _btnPrev.SetEnabled(index > 0);
            if (_btnNext != null) _btnNext.text = index == _steps.Count - 1 ? "FINISH TOUR 🏆" : "NEXT STEP ▶";

            if (_tourCard != null) _tourCard.style.display = DisplayStyle.Flex;

            if (!string.IsNullOrEmpty(step.targetPanelName))
            {
                MainDashboardToolkitController.Instance?.ShowPanel(step.targetPanelName);
            }
            else
            {
                MainDashboardToolkitController.Instance?.HideAllPanels();
            }

            if (!string.IsNullOrEmpty(step.targetSubTabName))
            {
                TriggerTargetClick(step.targetSubTabName);
            }
            else if (!string.IsNullOrEmpty(step.targetElementQuery))
            {
                TriggerTargetClick(step.targetElementQuery);
            }

            StopAllCoroutines();
            StartCoroutine(DeferredUpdateSpotlight(step));
        }

        private void TriggerTargetClick(string elementQuery)
        {
            if (string.IsNullOrEmpty(elementQuery)) return;

            var ecoCtrl = FindObjectOfType<EconomyDashboardController>(true);
            if (ecoCtrl != null) ecoCtrl.SwitchTabByName(elementQuery);

            var milCtrl = FindObjectOfType<MilitaryDashboardController>(true);
            if (milCtrl != null) milCtrl.SwitchTabByName(elementQuery);

            var intCtrl = FindObjectOfType<IntelligenceDashboardController>(true);
            if (intCtrl != null) intCtrl.SwitchTabByName(elementQuery);

            var dipCtrl = FindObjectOfType<DiplomacyDashboardController>(true);
            if (dipCtrl != null) dipCtrl.SwitchTabByName(elementQuery);

            var docs = FindObjectsOfType<UIDocument>();
            foreach (var doc in docs)
            {
                if (doc == null || doc.rootVisualElement == null || doc == _uiDoc) continue;
                var el = doc.rootVisualElement.Q<VisualElement>(elementQuery);
                if (el != null)
                {
                    using var evt = ClickEvent.GetPooled();
                    evt.target = el;
                    el.SendEvent(evt);
                    Debug.Log($"[TutorialTour] Auto-triggered click on '{elementQuery}' in {doc.name}");
                    break;
                }
            }
        }

        private VisualElement _lastHighlightedElement = null;

        private void ClearElementHighlight()
        {
            if (_lastHighlightedElement != null)
            {
                _lastHighlightedElement.RemoveFromClassList("tutorial-element-highlight");
                _lastHighlightedElement = null;
            }
        }

        private void HighlightTargetElement(VisualElement target)
        {
            ClearElementHighlight();
            if (target == null) return;
            _lastHighlightedElement = target;
            target.AddToClassList("tutorial-element-highlight");
        }

        private IEnumerator DeferredUpdateSpotlight(TutorialStep step)
        {
            yield return new WaitForEndOfFrame();
            
            VisualElement targetElement = null;
            if (!string.IsNullOrEmpty(step.targetElementQuery))
            {
                int retries = 5;
                while (retries > 0)
                {
                    targetElement = _root?.Q<VisualElement>(step.targetElementQuery);
                    if (targetElement != null) break;
                    retries--;
                    yield return null;
                }
            }

            if (_tourRoot != null)
            {
                _tourRoot.BringToFront();
                if (_tourRoot.parent != null) _tourRoot.parent.BringToFront();
            }

            bool isDashboardActive = !string.IsNullOrEmpty(step.targetPanelName);

            if (targetElement != null)
            {
                HighlightTargetElement(targetElement);

                if (_tourCard != null && (step.targetElementQuery == "TimeControls" || step.targetElementQuery == "BtnNotificationHistory" || step.targetElementQuery == "TopBar" || step.targetElementQuery == "BtnSystemMenu" || step.targetElementQuery == "PlayerFlag"))
                {
                    _tourCard.style.position = Position.Absolute;
                    _tourCard.style.translate = StyleKeyword.Null;
                    if (step.targetElementQuery == "PlayerFlag")
                    {
                        _tourCard.style.left = 20;
                        _tourCard.style.top = 75;
                        _tourCard.style.right = StyleKeyword.Null;
                        _tourCard.style.bottom = StyleKeyword.Null;
                    }
                    else
                    {
                        _tourCard.style.right = 20;
                        _tourCard.style.top = 75;
                        _tourCard.style.left = StyleKeyword.Null;
                        _tourCard.style.bottom = StyleKeyword.Null;
                    }
                    yield break;
                }
            }
            else
            {
                ClearElementHighlight();
                if (_spotlightFrame != null) _spotlightFrame.style.display = DisplayStyle.None;
            }

            if (_tourCard != null)
            {
                _tourCard.style.position = Position.Absolute;

                if (isDashboardActive)
                {
                    _tourCard.style.translate = StyleKeyword.Null;
                    _tourCard.style.right = 30;
                    _tourCard.style.bottom = 40;
                    _tourCard.style.left = StyleKeyword.Null;
                    _tourCard.style.top = StyleKeyword.Null;
                }
                else
                {
                    _tourCard.style.position = Position.Absolute;
                    _tourCard.style.left = Length.Percent(50);
                    _tourCard.style.top = Length.Percent(50);
                    _tourCard.style.translate = new StyleTranslate(new Translate(Length.Percent(-50), Length.Percent(-50)));
                    _tourCard.style.right = StyleKeyword.Null;
                    _tourCard.style.bottom = StyleKeyword.Null;
                }
            }
        }

        private void OnPrevClicked()
        {
            if (_currentStepIndex > 0) ShowStep(_currentStepIndex - 1);
        }

        private void OnNextClicked()
        {
            if (_currentStepIndex < _steps.Count - 1)
            {
                ShowStep(_currentStepIndex + 1);
            }
            else
            {
                EndTour();
            }
        }

        public void EndTour()
        {
            _isTourActive = false;
            PlayerPrefs.SetInt("HasCompletedTutorialTour", 1);
            PlayerPrefs.Save();

            ClearElementHighlight();
            SetTourVisibility(false);
            HidePrompt();
            if (_spotlightFrame != null) _spotlightFrame.style.display = DisplayStyle.None;

            MainDashboardToolkitController.Instance?.SetTimeSpeed(1);

            var player = GameManager.Instance?.playerCountry;
            if (player != null && !player.hasShownBriefing)
            {
                var stateManager = FindObjectOfType<UIStateManager>();
                if (stateManager != null)
                {
                    stateManager.ShowScenarioBriefingPublic(player);
                }
            }
        }
    }
}
