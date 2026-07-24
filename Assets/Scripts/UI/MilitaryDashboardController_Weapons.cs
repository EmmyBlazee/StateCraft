using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public partial class MilitaryDashboardController
    {
        private VisualElement _wPanelArsenal;
        private VisualElement _wPanelProduction;
        private VisualElement _wPanelProcurement;
        private VisualElement _wPanelResearch;

        private VisualElement _wGridArmy;
        private VisualElement _wGridAir;
        private VisualElement _wGridNavy;

        private Button _wTabArsenal;
        private Button _wTabProduction;
        private Button _wTabProcurement;
        private Button _wTabResearch;

        private string _procurementCategoryFilter = "All Categories";

        // Cached root used for lazy re-query on PopulateWeapons
        private VisualElement _weaponsRoot;
        private bool _weaponsWired = false;

        /// <summary>
        /// Recursively pierces TemplateContainer shadow trees at any depth.
        /// Unity's Q() only crosses one shadow-tree boundary; nested
        /// ui:Instance elements require this deeper walk.
        /// </summary>
        private static T DeepQ<T>(VisualElement root, string name) where T : VisualElement
        {
            if (root == null) return null;
            var hit = root.Q<T>(name);
            if (hit != null) return hit;
            foreach (var child in root.Children())
            {
                if (child is TemplateContainer tc)
                {
                    var inner = DeepQ<T>(tc.contentContainer ?? tc, name);
                    if (inner != null) return inner;
                }
            }
            return null;
        }

        /// <summary>
        /// Called during Awake. Only caches the root and attempts to wire sub-tab
        /// buttons. The grid/panel elements inside nested ui:Instance templates are
        /// NOT yet in the visual tree at Awake time — Unity instantiates them lazily.
        /// All inner queries are deferred to EnsureWeaponsWired().
        /// </summary>
        private void InitializeWeapons(VisualElement root)
        {
            UnityEngine.Debug.Log("[Weapons] InitializeWeapons called — deferring inner queries to first tab open.");
            _weaponsRoot = root;

            // Attempt early wire of sub-tab buttons (one shadow layer — may succeed)
            _wTabArsenal     = DeepQ<Button>(root, "WTabArsenal");
            _wTabProduction  = DeepQ<Button>(root, "WTabProduction");
            _wTabProcurement = DeepQ<Button>(root, "WTabProcurement");
            _wTabResearch    = DeepQ<Button>(root, "WTabResearch");
            if (_wTabResearch != null) _wTabResearch.style.display = UnityEngine.UIElements.DisplayStyle.None;

            if (_wTabArsenal    != null) _wTabArsenal.clicked    += () => SwitchWeaponsTab(0);
            if (_wTabProduction != null) _wTabProduction.clicked += () => SwitchWeaponsTab(1);
            if (_wTabProcurement!= null) _wTabProcurement.clicked+= () => SwitchWeaponsTab(2);
            if (_wTabResearch   != null) _wTabResearch.clicked   += () => SwitchWeaponsTab(3);

            UnityEngine.Debug.Log($"[Weapons] Early wire — Arsenal={_wTabArsenal!=null}, Production={_wTabProduction!=null}");
        }

        /// <summary>
        /// Queries all inner weapon elements. Called every time the Weapons main tab
        /// is activated (SwitchTab(2)) — by that point _panelWeapons is visible so
        /// Unity has fully resolved all nested TemplateContainers inside it.
        /// Marks _weaponsWired=true once the required grids are found, so subsequent
        /// calls short-circuit immediately.
        /// </summary>
        private void EnsureWeaponsWired()
        {
            if (_weaponsWired) return;

            // _panelWeapons is cached by InitializeElements (root.Q("PanelWeapons")).
            // It is a direct child of the dashboard root, so it is always resolved.
            // Sub-panels (WPanelArsenal etc.) and grids (WeaponsGrid etc.) live inside
            // it — but in nested <ui:Instance> templates.  Unity only instantiates
            // nested template content once the ancestor is visible, so we must wait
            // until SwitchTab(2) makes _panelWeapons Flex before querying here.
            var searchRoot = _panelWeapons ?? (_root ?? _weaponsRoot);
            if (searchRoot == null) return;

            // Sub-panels — direct children of _panelWeapons (one shadow layer max)
            _wPanelArsenal     = DeepQ<VisualElement>(searchRoot, "WPanelArsenal");
            _wPanelProduction  = DeepQ<VisualElement>(searchRoot, "WPanelProduction");
            _wPanelProcurement = DeepQ<VisualElement>(searchRoot, "WPanelProcurement");
            _wPanelResearch    = DeepQ<VisualElement>(searchRoot, "WPanelResearch");

            // Grids live inside the Arsenal sub-panel
            var arsenalRoot = _wPanelArsenal ?? searchRoot;
            _wGridArmy  = DeepQ<VisualElement>(arsenalRoot, "WeaponsGrid");
            _wGridAir   = DeepQ<VisualElement>(arsenalRoot, "WeaponsGridAir");
            _wGridNavy  = DeepQ<VisualElement>(arsenalRoot, "WeaponsGridNavy");

            // Fallback: search full root if still null
            if (_wGridArmy == null) _wGridArmy  = DeepQ<VisualElement>(searchRoot, "WeaponsGrid");
            if (_wGridAir  == null) _wGridAir   = DeepQ<VisualElement>(searchRoot, "WeaponsGridAir");
            if (_wGridNavy == null) _wGridNavy  = DeepQ<VisualElement>(searchRoot, "WeaponsGridNavy");

            // Wire sub-tab buttons if not yet wired (should already be from InitializeWeapons)
            if (_wTabArsenal == null)
            {
                _wTabArsenal     = DeepQ<Button>(searchRoot, "WTabArsenal");
                _wTabProduction  = DeepQ<Button>(searchRoot, "WTabProduction");
                _wTabProcurement = DeepQ<Button>(searchRoot, "WTabProcurement");
                _wTabResearch    = DeepQ<Button>(searchRoot, "WTabResearch");
                if (_wTabResearch != null) _wTabResearch.style.display = UnityEngine.UIElements.DisplayStyle.None;
                if (_wTabArsenal    != null) _wTabArsenal.clicked    += () => SwitchWeaponsTab(0);
                if (_wTabProduction != null) _wTabProduction.clicked += () => SwitchWeaponsTab(1);
                if (_wTabProcurement!= null) _wTabProcurement.clicked+= () => SwitchWeaponsTab(2);
                if (_wTabResearch   != null) _wTabResearch.clicked   += () => SwitchWeaponsTab(3);
            }

            bool gridsFound = _wGridArmy != null && _wGridAir != null && _wGridNavy != null;
            if (gridsFound) _weaponsWired = true;

            UnityEngine.Debug.Log($"[Weapons] EnsureWeaponsWired — searchRoot={searchRoot?.name} | Arsenal={_wPanelArsenal!=null} | Production={_wPanelProduction!=null} | Army={_wGridArmy!=null}, Air={_wGridAir!=null}, Navy={_wGridNavy!=null} | Wired={_weaponsWired}");
        }


        private void SwitchWeaponsTab(int index)
        {
            EnsureWeaponsWired();
            if (_wPanelArsenal    != null) _wPanelArsenal.style.display    = index == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_wPanelProduction != null) _wPanelProduction.style.display = index == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_wPanelProcurement!= null) _wPanelProcurement.style.display= index == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_wPanelResearch   != null) _wPanelResearch.style.display   = index == 3 ? DisplayStyle.Flex : DisplayStyle.None;

            if (_wTabArsenal    != null) { if (index == 0) _wTabArsenal.AddToClassList("active-tab");    else _wTabArsenal.RemoveFromClassList("active-tab"); }
            if (_wTabProduction != null) { if (index == 1) _wTabProduction.AddToClassList("active-tab"); else _wTabProduction.RemoveFromClassList("active-tab"); }
            if (_wTabProcurement!= null) { if (index == 2) _wTabProcurement.AddToClassList("active-tab");else _wTabProcurement.RemoveFromClassList("active-tab"); }
            if (_wTabResearch   != null) { if (index == 3) _wTabResearch.AddToClassList("active-tab");   else _wTabResearch.RemoveFromClassList("active-tab"); }

            // Populate sub-tab content on demand
            var country = GameManager.Instance?.playerCountry;
            if (country != null)
            {
                if (index == 1) PopulateProduction(country);
                else if (index == 2) PopulateProcurement(country);
                else if (index == 3) PopulateResearch(country);
            }
        }

        private void PopulateWeapons(CountryDataSO country)
        {
            UnityEngine.Debug.Log($"[Weapons] PopulateWeapons called for country: {country?.name}.");

            // Ensure inner elements are wired — deferred from Awake because
            // nested ui:Instance content is not in the visual tree until shown.
            EnsureWeaponsWired();

            // If grids are still null the weapons panel has not been shown yet;
            // rows will be populated by SwitchTab(2) once the user opens the tab.
            if (_wGridArmy == null && _wGridAir == null && _wGridNavy == null)
            {
                UnityEngine.Debug.Log("[Weapons] PopulateWeapons — grids not yet resolved (panel not shown). Skipping until tab is opened.");
                return;
            }

            if (_wGridArmy  != null) _wGridArmy.Clear();
            if (_wGridAir   != null) _wGridAir.Clear();
            if (_wGridNavy  != null) _wGridNavy.Clear();

            void AddRow(string title, string desc, int act, int oos, VisualElement grid, string img)
            {
                float pct = (act + oos) > 0 ? (float)act / (act + oos) : 0f;
                string condition = pct >= 0.85f ? "Excellent" : pct >= 0.70f ? "Good" : pct >= 0.40f ? "Fair" : "Poor";
                AddWeaponRow(title, desc, act, pct, condition, grid, img);
            }

            // Army
            AddRow("Assault Rifles",    "Standard Infantry Weapon",   country.unitRifles,    country.oosRifles,    _wGridArmy, "rifle1");
            AddRow("APCs",              "Armored Personnel Carriers", country.unitAPCs,      country.oosAPCs,      _wGridArmy, "Apcs");
            AddRow("Main Battle Tanks", "Heavy Armor",                country.unitTanks,     country.oosTanks,     _wGridArmy, "maintank");
            AddRow("Artillery Systems", "Towed & Self-Propelled",     country.unitArtillery, country.oosArtillery, _wGridArmy, "artilery");

            // Air Force
            AddRow("Fighter Jets",      "Multi-Role Combat Aircraft", country.unitFighters,   country.oosFighters,   _wGridAir, "fighter aircaft");
            AddRow("Strategic Bombers", "Heavy Payloads",             country.unitBombers,    country.oosBombers,    _wGridAir, "bomber1");
            AddRow("Helicopters",       "Attack & Utility Helicopters",country.unitAttackHelis,country.oosAttackHelis,_wGridAir, "helicopter1");
            AddRow("UAV Drones",        "Unmanned Aerial Vehicles",   country.unitUAVs,       country.oosUAVs,       _wGridAir, "uav");

            // Navy
            AddRow("Patrol Boats",  "Coastal Patrol Vessels",    country.unitPatrolBoats, country.oosPatrolBoats, _wGridNavy, "patrol boat");
            AddRow("Frigates",      "Multi-Role Frigates",        country.unitFrigates,    country.oosFrigates,    _wGridNavy, "frigate");
            AddRow("Submarines",    "Attack Submarines",          country.unitSubmarines,  country.oosSubmarines,  _wGridNavy, "submarine");
            AddRow("Destroyers",    "Guided Missile Destroyers",  country.unitDestroyers,  country.oosDestroyers,  _wGridNavy, "destroyer");

            // Populate the left and right sidebars with live data
            PopulateArsenalSidebars(country);

            // Refresh visible sub-tabs within Weapons
            if (_wPanelProduction != null && _wPanelProduction.style.display == DisplayStyle.Flex)
                PopulateProduction(country);
            if (_wPanelProcurement != null && _wPanelProcurement.style.display == DisplayStyle.Flex)
                PopulateProcurement(country);
            if (_wPanelResearch != null && _wPanelResearch.style.display == DisplayStyle.Flex)
                PopulateResearch(country);

            UnityEngine.Debug.Log("[Weapons] PopulateWeapons finished.");
        }

        private void PopulateArsenalSidebars(CountryDataSO country)
        {
            // ── Totals ───────────────────────────────────────────────────────
            int armyTotal  = country.unitRifles + country.unitAPCs + country.unitTanks
                           + country.unitArtillery;
            int airTotal   = country.unitFighters + country.unitBombers
                           + country.unitAttackHelis + country.unitUAVs;
            int navyTotal  = country.unitFrigates + country.unitDestroyers
                           + country.unitSubmarines + country.unitPatrolBoats;
            int grandTotal = armyTotal + airTotal + navyTotal;

            // Branch share percentages
            float armyPct  = grandTotal > 0 ? (armyTotal  / (float)grandTotal) * 100f : 0f;
            float airPct   = grandTotal > 0 ? (airTotal   / (float)grandTotal) * 100f : 0f;
            float navyPct  = grandTotal > 0 ? (navyTotal  / (float)grandTotal) * 100f : 0f;
            float otherPct = Mathf.Max(0f, 100f - armyPct - airPct - navyPct);

            // Average readiness across branches
            float avgReadiness = (country.armyStats.readiness
                                + country.airForceStats.readiness
                                + country.navyStats.readiness) / 3f;

            // Operational breakdown: operational = avg readiness %, maintenance = ~10% buffer, rest = out of service
            int operational  = Mathf.RoundToInt(grandTotal * (avgReadiness / 100f));
            int maintenance  = Mathf.RoundToInt(grandTotal * 0.10f);
            int outOfService = Mathf.Max(0, grandTotal - operational - maintenance);

            // Tech level: derive from armyStats/airForce readiness → 1-5 tiers
            float techScore = (country.armyStats.readiness + country.airForceStats.readiness) / 2f;
            string grade    = techScore >= 90f ? "A" : techScore >= 75f ? "B" : techScore >= 60f ? "C" : techScore >= 45f ? "D" : "F";
            string techLabel= grade switch { "A" => "COMBAT READY", "B" => "HIGH READINESS", "C" => "MODERATE", "D" => "LOW READINESS", _ => "CRITICAL DEFICIT" };
            float techXP    = techScore; // 0-100 used as XP %

            // Maintenance status label
            string maintLabel = avgReadiness >= 85f ? "Excellent" : avgReadiness >= 70f ? "Good"
                              : avgReadiness >= 50f ? "Fair" : "Poor";

            // ── Resolve elements from the Arsenal panel ──────────────────────
            if (_wPanelArsenal == null) return;

            void Set(string name, string val)
            {
                var el = DeepQ<Label>(_wPanelArsenal, name);
                if (el != null) el.text = val;
            }

            void SetBar(string name, float val, Color? fillColor = null)
            {
                var el = DeepQ<ProgressBar>(_wPanelArsenal, name);
                if (el != null)
                {
                    el.value = val;
                    if (fillColor.HasValue)
                    {
                        // Tint the progress bar fill using UI Toolkit's internal class
                        el.schedule.Execute(() => {
                            var progress = el.Q<VisualElement>(className: "unity-progress-bar__progress");
                            if (progress != null) progress.style.backgroundColor = fillColor.Value;
                        });
                    }
                }
            }

            // Tech Level Styling
            Color techColor = grade switch {
                "A" => new Color(0.1f, 0.5f, 0.9f),       // Blue
                "B" => new Color(0.9f, 0.6f, 0f),       // Orange
                "C" => new Color(0.9f, 0.6f, 0f),  // Yellow
                "D" => new Color(255/255f, 82/255f, 82/255f),   // Red
                _ => new Color(160/255f, 160/255f, 160/255f)  // Grey
            };

            var lblLevel = DeepQ<Label>(_wPanelArsenal, "WArsTechLevel");
            if (lblLevel != null)
            {
                lblLevel.style.color = techColor;
                var box = lblLevel.parent;
                if (box != null)
                {
                    box.style.borderTopColor = techColor;
                    box.style.borderBottomColor = techColor;
                    box.style.borderLeftColor = techColor;
                    box.style.borderRightColor = techColor;
                    box.style.backgroundColor = new Color(techColor.r, techColor.g, techColor.b, 0.1f);
                }
            }

            var lblLabel = DeepQ<Label>(_wPanelArsenal, "WArsTechLabel");
            if (lblLabel != null) lblLabel.style.color = techColor;

            // ── LEFT sidebar ─────────────────────────────────────────────────
            Set("WArsDonutTotal",  $"{grandTotal:N0}");
            Set("WArsLegArmy",     $"{armyPct:F0}%");
            Set("WArsLegAF",       $"{airPct:F0}%");
            Set("WArsLegNavy",     $"{navyPct:F0}%");
            Set("WArsLegOther",    $"{otherPct:F0}%");

            Set("WArsTechLevel",   $"{grade}");
            Set("WArsTechLabel",   techLabel);
            SetBar("WArsTechXPBar", techXP, techColor);
            Set("WArsTechXP",      $"{techScore:F0} / 100 %");

            var maintColor = avgReadiness >= 70f
                ? new Color(0f, 210/255f, 130/255f)
                : avgReadiness >= 50f
                    ? new Color(0.9f, 0.6f, 0f)
                    : new Color(255/255f, 82/255f, 82/255f);

            Set("WArsMaintLabel",  maintLabel);
            Set("WArsMaintPct",    $"{avgReadiness:F0}%");
            SetBar("WArsMaintBar", avgReadiness, maintColor);

            // Tint the maintenance label colour based on status
            var maintLabelEl = DeepQ<Label>(_wPanelArsenal, "WArsMaintLabel");
            var maintPctEl   = DeepQ<Label>(_wPanelArsenal, "WArsMaintPct");
            
            if (maintLabelEl != null) maintLabelEl.style.color = maintColor;
            if (maintPctEl   != null) maintPctEl.style.color   = maintColor;

            // ── RIGHT sidebar ─────────────────────────────────────────────────
            Set("WArsQSTotal",       $"{grandTotal:N0}");
            Set("WArsQSOperational", $"{operational:N0}");
            Set("WArsQSMaintenance", $"{maintenance:N0}");
            Set("WArsQSOutOfService",$"{outOfService:N0}");

            // Recently Acquired — from completed production log
            var recentList = DeepQ<ScrollView>(_wPanelArsenal, "WArsRecentList");
            if (recentList != null)
            {
                recentList.Clear();
                var items = country.completedProductions ?? new System.Collections.Generic.List<CompletedProductionItem>();
                if (items.Count == 0)
                {
                    var none = new Label("No recent acquisitions.");
                    none.style.color   = new Color(100/255f, 100/255f, 100/255f);
                    none.style.fontSize = 8;
                    recentList.Add(none);
                }
                else
                {
                    foreach (var item in items)
                    {
                        var row = new VisualElement();
                        row.style.flexDirection  = FlexDirection.Row;
                        row.style.justifyContent = Justify.SpaceBetween;
                        row.style.marginBottom   = 5;

                        var nameLbl = new Label(item.unitLabel);
                        nameLbl.style.color    = Color.white;
                        nameLbl.style.fontSize = 8;
                        nameLbl.style.flexGrow = 1;
                        row.Add(nameLbl);

                        var qtyLbl = new Label($"+{item.quantity:N0}");
                        qtyLbl.style.color  = new Color(0f, 210/255f, 130/255f);
                        qtyLbl.style.fontSize = 11;
                        qtyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                        row.Add(qtyLbl);

                        recentList.Add(row);
                    }
                }
            }

            // Low Stock Alerts — check units vs minimum thresholds
            var alertsList = DeepQ<ScrollView>(_wPanelArsenal, "WArsAlertsList");
            if (alertsList != null)
            {
                alertsList.Clear();
                var alerts = new System.Collections.Generic.List<(string label, string detail)>();

                if (country.unitRifles    < 10000) alerts.Add(("Assault Rifles",   $"Only {country.unitRifles:N0} in stock"));
                if (country.unitAPCs      < 50)    alerts.Add(("APCs",             $"Only {country.unitAPCs} in stock"));
                if (country.unitTanks     < 20)    alerts.Add(("Battle Tanks",     $"Only {country.unitTanks} in stock"));
                if (country.unitFighters  < 10)    alerts.Add(("Fighter Jets",     $"Only {country.unitFighters} in stock"));
                if (country.unitFrigates  < 2)     alerts.Add(("Frigates",         $"Only {country.unitFrigates} in stock"));
                if (country.reserveAmmo   < 30f)   alerts.Add(("Ammunition",       $"{country.reserveAmmo:F0} days reserve"));
                if (country.reserveFuel   < 30f)   alerts.Add(("Fuel Reserves",    $"{country.reserveFuel:F0} days reserve"));

                if (alerts.Count == 0)
                {
                    var ok = new Label("All stock levels nominal.");
                    ok.style.color    = new Color(0f, 210/255f, 130/255f);
                    ok.style.fontSize = 10;
                    alertsList.Add(ok);
                }
                else
                {
                    foreach (var (label, detail) in alerts)
                    {
                        var alertRow = new VisualElement();
                        alertRow.style.marginBottom = 6;

                        var alertLbl = new Label($"⚠  {label}");
                        alertLbl.style.color = new Color(255/255f, 82/255f, 82/255f);
                        alertLbl.style.fontSize = 11;
                        alertLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                        alertRow.Add(alertLbl);

                        var detailLbl = new Label(detail);
                        detailLbl.style.color   = new Color(140/255f, 140/255f, 140/255f);
                        detailLbl.style.fontSize = 10;
                        alertRow.Add(detailLbl);

                        alertsList.Add(alertRow);
                    }
                }
            }
        }


        // ════════════════════════════════════════════════════════════════════
        //  PRODUCTION TAB
        // ════════════════════════════════════════════════════════════════════


        private string GetImageNameForUnit(string label)
        {
            if (string.IsNullOrEmpty(label)) return null;
            label = label.ToLower();
            if (label.Contains("apc") || label.Contains("personnel carrier") || label.Contains("carrier") || label.Contains("ifv") || label.Contains("fighting vehicle") || label.Contains("stryker") || label.Contains("rooikat")) return "Apcs";
            if (label.Contains("tank") || label.Contains("armor") || label.Contains("leopard") || label.Contains("t-90") || label.Contains("m1a2")) return "maintank";
            if (label.Contains("artillery") || label.Contains("howitzer") || label.Contains("caesar") || label.Contains("m109")) return "artilery";
            if (label.Contains("fighter") || label.Contains("jet") || label.Contains("jf-17") || label.Contains("f-16") || label.Contains("su-35")) return "fighter aircaft";
            if (label.Contains("transport") || label.Contains("cargo") || label.Contains("c-130")) return "transport aircraft";
            if (label.Contains("bomber") || label.Contains("h-6") || label.Contains("b-2")) return "bomber1";
            if (label.Contains("uav") || label.Contains("drone") || label.Contains("tb2")) return "uav";
            if (label.Contains("helicopter") || label.Contains("ah-64") || label.Contains("apache")) return "helicopter1";
            if (label.Contains("submarine") || label.Contains("sub")) return "submarine";
            if (label.Contains("frigate") || label.Contains("corvette")) return "frigate";
            if (label.Contains("destroyer") || label.Contains("cruiser")) return "destroyer";
            if (label.Contains("patrol") || label.Contains("boat") || label.Contains("opv")) return "patrol boat";
            if (label.Contains("sam") || label.Contains("missile") || label.Contains("radar") || label.Contains("surface-to-air") || label.Contains("air defense")) return "artilery";
            if (label.Contains("rifle") || label.Contains("infantry") || label.Contains("ballistic") || label.Contains("ak-12")) return "rifle1";
            return null;
        }

        private void PopulateProduction(CountryDataSO country)
        {
            if (_wPanelProduction == null) return;
            _wPanelProduction.Clear();

            // Colour palette
            var cGreen  = new Color(0f,         210/255f, 130/255f);
            var cCyan   = new Color(0.1f, 0.5f, 0.9f);
            var cYellow = new Color(0.9f, 0.6f, 0f);
            var cOrange = new Color(0.9f, 0.6f, 0f);
            var cRed    = new Color(255/255f,   82/255f,  82/255f);
            var cDim    = new Color(140/255f,   140/255f, 140/255f);
            var cPanel  = new Color(1/255f,     15/255f,  30/255f,  0.6f);
            var cBorder = new Color(255/255f,   255/255f, 255/255f, 0.08f);

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;

            // Seed demo data if empty
            EnsureProductionData(country);

            // ── Scroll root ──────────────────────────────────────────────────
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; // Force hide horizontal bar
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 12;
            scroll.style.paddingTop = 8;
            scroll.style.paddingBottom = 8;
            _wPanelProduction.Add(scroll);

            var root = scroll.contentContainer;
            root.style.paddingRight = 20; // Add padding to the content to dodge the scrollbar!

            // (Two-column layout moved below Production Overview)

            // ════════════════════════════════════════════════════════════════
            // LEFT COLUMN
            // ════════════════════════════════════════════════════════════════

            // ── Section label helper ─────────────────────────────────────────
            void AddSectionLabel(VisualElement parent, string text)
            {
                var lbl = new Label(text);
                lbl.style.alignSelf = Align.FlexStart;
                lbl.style.color = cGreen;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                lbl.style.fontSize = 10;
                lbl.style.marginBottom = 8;
                lbl.style.marginTop = 4;
                lbl.style.borderBottomWidth = 1;
                lbl.style.borderBottomColor = new Color(0f, 210/255f, 130/255f, 0.3f);
                lbl.style.paddingBottom = 4;
                parent.Add(lbl);
            }

            // ── PRODUCTION OVERVIEW ──────────────────────────────────────────
            AddSectionLabel(root, "PRODUCTION OVERVIEW");

            int activeFactories = country.defenseFactories.Count;
            int totalFactories  = Mathf.Max(activeFactories, country.baseDefenseFactoryCount + activeFactories);
            float avgUtil       = activeFactories > 0
                ? country.defenseFactories.ConvertAll(f => f.utilizationPct).ToArray().Average()
                : 0f;
            float totalMonthlyCost  = 0f;
            float totalMonthlyValue = 0f;
            float avgCompletion     = 0f;

            if (country.productionOrders.Count > 0)
            {
                foreach (var o in country.productionOrders)
                {
                    totalMonthlyCost  += o.monthlyCostB;
                    totalMonthlyValue += o.monthlyOutput * GetUnitValueB(o.id);
                }
                avgCompletion = country.productionOrders.Count > 0
                    ? (float)country.productionOrders.Select(o => (float)o.monthsRemaining).Average()
                    : 0f;
            }

            var overviewRow = new VisualElement();
            overviewRow.style.flexDirection = FlexDirection.Row;
            overviewRow.style.flexWrap      = Wrap.Wrap;
            overviewRow.style.marginBottom  = 12;
            root.Add(overviewRow);

            void AddOverviewCard(VisualElement parent, string icon, string value, string title, string sub, Color valColor, float flexGrow = 1)
            {
                var card = new VisualElement();
                card.style.flexGrow        = flexGrow;
                card.style.backgroundColor = cPanel;
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
                SetBorderWidth(card.style, 1);
                SetBorderColor(card.style, cBorder);
                card.style.paddingTop    = 10;
                card.style.paddingBottom = 10;
                card.style.paddingLeft   = 12;
                card.style.paddingRight  = 12;
                card.style.marginRight   = 8;
                card.style.flexDirection = FlexDirection.Row;
                card.style.alignItems    = Align.Center;

                var iconLbl = new Label(icon);
                iconLbl.AddToClassList("fa-icon");
                iconLbl.style.fontSize   = 18;
                iconLbl.style.marginRight = 10;
                iconLbl.style.color      = cCyan;
                card.Add(iconLbl);

                var textCol = new VisualElement();
                textCol.style.flexDirection = FlexDirection.Column;
                card.Add(textCol);

                var valLbl = new Label(value);
                valLbl.style.fontSize = 16;
                valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                valLbl.style.color = valColor;
                textCol.Add(valLbl);

                var titleLbl = new Label(title);
                titleLbl.style.fontSize = 11;
                titleLbl.style.color = Color.white;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                textCol.Add(titleLbl);

                var subLbl = new Label(sub);
                subLbl.style.fontSize = 7;
                subLbl.style.color = cDim;
                textCol.Add(subLbl);

                parent.Add(card);
            }

            AddOverviewCard(overviewRow, "\uf275", $"{activeFactories}", "ACTIVE FACTORIES", $"of {totalFactories} Total", Color.white);
            AddOverviewCard(overviewRow, "\uf013", $"{avgUtil:F0}%", "FACTORY UTILIZATION", "Across all facilities", cGreen);
            AddOverviewCard(overviewRow, "\uf155", UiFormat.Money(totalMonthlyCost * country.exchangeRate, sym), "MONTHLY PROD. COST", "Operating expenses", cYellow);
            AddOverviewCard(overviewRow, "\uf1b2", UiFormat.Money(totalMonthlyValue * country.exchangeRate, sym), "MONTHLY OUTPUT VALUE", "Total production value", cGreen);
            AddOverviewCard(overviewRow, "\uf017", $"{avgCompletion:F1}", "AVG. COMPLETION TIME", "Months", cCyan, 0.8f);

            // ── Two-column layout: left=main, right=sidebar ──────────────────
            var twoCols = new VisualElement();
            twoCols.style.flexDirection = FlexDirection.Row;
            twoCols.style.flexGrow = 1;
            twoCols.style.width = Length.Percent(100);
            root.Add(twoCols);

            var leftCol = new VisualElement();
            leftCol.style.flexGrow = 1;
            leftCol.style.marginRight = 10;
            leftCol.style.minWidth = 0; // CRITICAL: Prevents children from forcing leftCol to overlap rightCol
            leftCol.style.overflow = Overflow.Hidden; // Failsafe to strictly prevent horizontal overflow
            twoCols.Add(leftCol);

            var rightCol = new VisualElement();
            rightCol.style.width = 340; // Widened to prevent squishing the order forms
            rightCol.style.minWidth = 340; 
            rightCol.style.flexShrink = 0;
            twoCols.Add(rightCol);

            // ── ACTIVE PRODUCTION ────────────────────────────────────────────
            AddSectionLabel(leftCol, "ACTIVE PRODUCTION");

            if (country.productionOrders.Count == 0)
            {
                var emptyLbl = new Label("No active production orders. Create one using the panel →");
                emptyLbl.style.color     = cDim;
                emptyLbl.style.fontSize  = 12;
                emptyLbl.style.marginBottom = 12;
                leftCol.Add(emptyLbl);
            }
            else
            {
                // 2-column grid for facility cards
                var facilityGrid = new VisualElement();
                facilityGrid.style.flexDirection = FlexDirection.Row;
                facilityGrid.style.flexWrap      = Wrap.Wrap;
                facilityGrid.style.marginBottom  = 12;
                leftCol.Add(facilityGrid);

                foreach (var order in country.productionOrders)
                    AddProductionFacilityCard(facilityGrid, order, country, sym, cGreen, cCyan, cYellow, cOrange, cRed, cDim, cPanel, cBorder, GetImageNameForUnit(order.unitLabel));
            }

            // ── RECENTLY COMPLETED ───────────────────────────────────────────
            if (country.completedProductions != null && country.completedProductions.Count > 0)
            {
                AddSectionLabel(leftCol, "RECENTLY COMPLETED");

                var completedRow = new VisualElement();
                completedRow.style.flexDirection = FlexDirection.Row;
                completedRow.style.alignItems    = Align.Center;
                completedRow.style.marginBottom  = 10;
                leftCol.Add(completedRow);

                // Prev arrow
                int completedPage = 0;
                var prevArrow = new Button() { text = "‹" };
                prevArrow.style.fontSize          = 18;
                prevArrow.style.width             = 22;
                prevArrow.style.height            = 60;
                prevArrow.style.backgroundColor   = new Color(0,0,0,0.3f);
                prevArrow.style.color             = cDim;
                prevArrow.style.marginRight       = 6;
                completedRow.Add(prevArrow);

                // Scrollable completed items (show up to 4)
                var completedList = new VisualElement();
                completedList.style.flexDirection = FlexDirection.Row;
                completedList.style.flexGrow      = 1;
                completedRow.Add(completedList);

                var nextArrow = new Button() { text = "›" };
                nextArrow.style.fontSize        = 18;
                nextArrow.style.width           = 22;
                nextArrow.style.height          = 60;
                nextArrow.style.backgroundColor = new Color(0,0,0,0.3f);
                nextArrow.style.color           = cDim;
                nextArrow.style.marginLeft      = 6;
                completedRow.Add(nextArrow);

                void RenderCompletedPage()
                {
                    completedList.Clear();
                    int start = completedPage * 4;
                    for (int i = start; i < Mathf.Min(start + 4, country.completedProductions.Count); i++)
                    {
                        var item = country.completedProductions[i];
                        var card = new VisualElement();
                        card.style.flexGrow        = 1;
                        card.style.backgroundColor = cPanel;
                        card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
                        card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
                        SetBorderWidth(card.style, 1);
                        SetBorderColor(card.style, cBorder);
                        card.style.paddingTop    = 8;
                        card.style.paddingBottom = 8;
                        card.style.paddingLeft   = 10;
                        card.style.paddingRight  = 10;
                        card.style.marginRight   = 6;
                        card.style.alignItems    = Align.FlexStart;

                        // Image placeholder
                                                var img = new VisualElement();
                        img.style.width  = 50;
                        img.style.height = 32;
                        img.style.backgroundColor = new Color(0, 0, 0, 0.3f);
                        img.style.borderTopLeftRadius = img.style.borderTopRightRadius =
                        img.style.borderBottomLeftRadius = img.style.borderBottomRightRadius = 3;
                        img.style.marginBottom = 5;
                        img.style.alignSelf    = Align.Center;

                        string imageName = GetImageNameForUnit(item.unitLabel);
                        if (!string.IsNullOrEmpty(imageName))
                        {
                            var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                            if (tex != null)
                            {
                                img.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
#pragma warning disable CS0618
                                img.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                                img.style.backgroundColor = UnityEngine.Color.clear;
                            }
                        }

                        card.Add(img);

                        var nameLbl = new Label(item.unitLabel);
                        nameLbl.style.color = Color.white;
                        nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                        nameLbl.style.fontSize = 9;
                        card.Add(nameLbl);

                        var qtyLbl = new Label($"{item.quantity:N0} Units");
                        qtyLbl.style.color   = Color.white;
                        qtyLbl.style.fontSize = 11;
                        card.Add(qtyLbl);

                        var statusLbl = new Label("Completed");
                        statusLbl.style.color   = cGreen;
                        statusLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                        statusLbl.style.fontSize = 11;
                        card.Add(statusLbl);

                        var dateLbl = new Label(item.completedDate);
                        dateLbl.style.color  = cDim;
                        dateLbl.style.fontSize = 7;
                        card.Add(dateLbl);

                        completedList.Add(card);
                    }
                }

                RenderCompletedPage();
                int totalPages = Mathf.CeilToInt(country.completedProductions.Count / 4f);
                prevArrow.clicked += () => { completedPage = Mathf.Max(0, completedPage - 1); RenderCompletedPage(); };
                nextArrow.clicked += () => { completedPage = Mathf.Min(totalPages - 1, completedPage + 1); RenderCompletedPage(); };
            }

            // ════════════════════════════════════════════════════════════════
            // RIGHT COLUMN
            // ════════════════════════════════════════════════════════════════

            // ── NEW PRODUCTION ORDER ─────────────────────────────────────────
            var ordSection = new Label("NEW PRODUCTION ORDER");
            ordSection.style.alignSelf = Align.FlexStart;
            ordSection.style.color    = cGreen;
            ordSection.style.unityFontStyleAndWeight = FontStyle.Bold;
            ordSection.style.fontSize = 10;
            ordSection.style.marginBottom = 8;
            ordSection.style.borderBottomWidth = 1;
            ordSection.style.borderBottomColor = new Color(0f, 210/255f, 130/255f, 0.3f);
            ordSection.style.paddingBottom = 4;
            rightCol.Add(ordSection);

            // Order forms
            var orderForms = new VisualElement();
            orderForms.style.backgroundColor = cPanel;
            SetBorderWidth(orderForms.style, 1);
            SetBorderColor(orderForms.style, cBorder);
            orderForms.style.borderTopLeftRadius = orderForms.style.borderTopRightRadius =
            orderForms.style.borderBottomLeftRadius = orderForms.style.borderBottomRightRadius = 4;
            orderForms.style.paddingTop    = 10;
            orderForms.style.paddingBottom = 10;
            orderForms.style.paddingLeft   = 12;
            orderForms.style.paddingRight  = 12;
            orderForms.style.marginBottom  = 10;
            rightCol.Add(orderForms);

            // Full list of available weapons to produce
            var orderDefs = new (string cat, string item, string id, int qty)[]
            {
                // Army
                ("Army Equipment", "Assault Rifles",   "RIFLE",  1000),
                ("Army Equipment", "Artillery",        "ARTILLERY", 10),
                ("Vehicles",       "APC",               "APC",     50),
                ("Vehicles",       "Battle Tanks",      "TNK",     20),
                
                // Aviation
                ("Aviation",       "UAVs",              "UAV",     10),
                ("Aviation",       "Attack Helicopters","HEL",     5),
                ("Aviation",       "Fighter Jets",      "FTR",     5),
                ("Aviation",       "Bombers",           "BOMBER",  1),
                
                // Naval
                ("Naval Vessels",  "Patrol Boats",      "PATROL_BOAT", 5),
                ("Naval Vessels",  "Frigates",          "FRG",     2),
                ("Naval Vessels",  "Submarines",        "SUB",     1),
                ("Naval Vessels",  "Destroyers",        "DESTROYER", 1)
            };

            foreach (var (cat, item, id, qty) in orderDefs)
                AddNewOrderForm(orderForms, cat, item, id, qty, country, sym, cGreen, cCyan, cDim, cBorder, cPanel);



            // ── PRODUCTION FACILITIES ────────────────────────────────────────
            var facSection = new Label("PRODUCTION FACILITIES");
            facSection.style.alignSelf = Align.FlexStart;
            facSection.style.color    = cGreen;
            facSection.style.unityFontStyleAndWeight = FontStyle.Bold;
            facSection.style.fontSize = 10;
            facSection.style.marginBottom = 8;
            facSection.style.borderBottomWidth = 1;
            facSection.style.borderBottomColor = new Color(0f, 210/255f, 130/255f, 0.3f);
            facSection.style.paddingBottom = 4;
            rightCol.Add(facSection);

            var facList = new VisualElement();
            facList.style.backgroundColor = cPanel;
            SetBorderWidth(facList.style, 1);
            SetBorderColor(facList.style, cBorder);
            facList.style.borderTopLeftRadius = facList.style.borderTopRightRadius =
            facList.style.borderBottomLeftRadius = facList.style.borderBottomRightRadius = 4;
            facList.style.paddingTop    = 8;
            facList.style.paddingBottom = 8;
            facList.style.paddingLeft   = 12;
            facList.style.paddingRight  = 12;
            facList.style.marginBottom  = 10;
            rightCol.Add(facList);

            foreach (var fac in country.defenseFactories)
            {
                var row = new VisualElement();
                row.style.flexDirection  = FlexDirection.Row;
                row.style.alignItems     = Align.Center;
                row.style.marginBottom   = 8;

                var icon = new Label("🏭");
                icon.style.fontSize   = 11;
                icon.style.width      = 18;
                icon.style.marginRight = 6;
                icon.style.color      = cCyan;
                row.Add(icon);

                var nameCol = new VisualElement();
                nameCol.style.flexGrow = 1;
                var nameLbl = new Label(fac.factoryName);
                nameLbl.style.color   = Color.white;
                nameLbl.style.fontSize = 8;
                nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                nameCol.Add(nameLbl);
                var levelLbl = new Label($"Level {fac.level}");
                levelLbl.style.color   = cDim;
                levelLbl.style.fontSize = 7;
                nameCol.Add(levelLbl);
                row.Add(nameCol);

                var barContainer = new VisualElement();
                barContainer.style.width  = 90;
                barContainer.style.height = 4;
                barContainer.style.backgroundColor = new Color(1,1,1,0.08f);
                barContainer.style.borderTopLeftRadius = barContainer.style.borderTopRightRadius =
                barContainer.style.borderBottomLeftRadius = barContainer.style.borderBottomRightRadius = 2;
                barContainer.style.marginRight = 6;

                var barFill = new VisualElement();
                barFill.style.height = Length.Percent(100);
                barFill.style.width  = Length.Percent(fac.utilizationPct);
                barFill.style.borderTopLeftRadius = barFill.style.borderTopRightRadius =
                barFill.style.borderBottomLeftRadius = barFill.style.borderBottomRightRadius = 2;
                barFill.style.backgroundColor = fac.utilizationPct >= 70f ? cGreen : fac.utilizationPct >= 40f ? cYellow : cRed;
                barContainer.Add(barFill);
                row.Add(barContainer);

                var pctLbl = new Label($"{fac.utilizationPct:F0}%");
                pctLbl.style.color    = fac.utilizationPct >= 70f ? cGreen : fac.utilizationPct >= 40f ? cYellow : cRed;
                pctLbl.style.fontSize = 8;
                pctLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                pctLbl.style.width    = 30;
                pctLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                row.Add(pctLbl);

                facList.Add(row);
            }

            // Manage Facilities button
            var manageFacBtn = new Button();
            manageFacBtn.text = "⚙  MANAGE FACILITIES";
            manageFacBtn.style.width           = Length.Percent(100);
            manageFacBtn.style.height          = 28;
            manageFacBtn.style.fontSize        = 9;
            manageFacBtn.style.color           = Color.white;
            manageFacBtn.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
            SetBorderWidth(manageFacBtn.style, 1);
            SetBorderColor(manageFacBtn.style, cBorder);
            manageFacBtn.style.borderTopLeftRadius = manageFacBtn.style.borderTopRightRadius =
            manageFacBtn.style.borderBottomLeftRadius = manageFacBtn.style.borderBottomRightRadius = 3;
            rightCol.Add(manageFacBtn);
            manageFacBtn.clicked += () =>
            {
                SwitchTab(3); // Switch to the Bases tab
            };
        }

        private void AddProductionFacilityCard(VisualElement grid, ProductionOrder order, CountryDataSO country,
            string sym, Color cGreen, Color cCyan, Color cYellow, Color cOrange, Color cRed, Color cDim, Color cPanel, Color cBorder, string imageName = null)
        {
            var card = new VisualElement();
            card.style.width         = Length.Percent(48);
            card.style.marginBottom  = 10;
            card.style.marginRight   = 8;
            card.style.backgroundColor = cPanel;
            card.style.minHeight     = 115;
            SetBorderWidth(card.style, 1);
            SetBorderColor(card.style, cBorder);
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 5;
            card.style.paddingTop    = 10;
            card.style.paddingBottom = 10;
            card.style.paddingLeft   = 12;
            card.style.paddingRight  = 12;

            card.RegisterCallback<MouseEnterEvent>(e =>
            {
                SetBorderColor(card.style, new Color(0f, 210/255f, 255/255f, 0.35f));
            });
            card.RegisterCallback<MouseLeaveEvent>(e =>
            {
                SetBorderColor(card.style, cBorder);
            });

            // Top row: facility name + icon
            var topRow = new VisualElement();
            topRow.style.flexDirection  = FlexDirection.Row;
            topRow.style.justifyContent = Justify.SpaceBetween;
            topRow.style.alignItems     = Align.Center;
            topRow.style.marginBottom   = 6;
            card.Add(topRow);

            var facName = new Label(order.facilityName.ToUpper());
            facName.style.color = Color.white;
            facName.style.unityFontStyleAndWeight = FontStyle.Bold;
            facName.style.fontSize = 10;
            topRow.Add(facName);

            var settingsIcon = new Label("⚙");
            settingsIcon.style.color   = cDim;
            settingsIcon.style.fontSize = 10;
            topRow.Add(settingsIcon);

            // Body Row to hold image on left and stats on right
            var bodyRow = new VisualElement();
            bodyRow.style.flexDirection = FlexDirection.Row;
            bodyRow.style.marginBottom  = 6;
            card.Add(bodyRow);

            // Image placeholder
            var imgBox = new VisualElement();
            imgBox.style.width           = 80;
            imgBox.style.height          = 55;
            imgBox.style.flexShrink      = 0;
            imgBox.style.marginRight     = 10;
            imgBox.style.backgroundColor = new Color(0, 0, 0, 0.35f);
            imgBox.style.borderTopLeftRadius = imgBox.style.borderTopRightRadius =

            imgBox.style.borderBottomLeftRadius = imgBox.style.borderBottomRightRadius = 3;
            if (!string.IsNullOrEmpty(imageName))
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                if (tex != null)
                {
                    imgBox.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
#pragma warning disable CS0618
                    imgBox.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                    imgBox.style.backgroundColor = UnityEngine.Color.clear;
                }
            }
            bodyRow.Add(imgBox);

            // Stats Column
            var statsCol = new VisualElement();
            statsCol.style.flexGrow = 1;
            statsCol.style.flexDirection = FlexDirection.Column;
            bodyRow.Add(statsCol);

            // Unit type subtitle
            var unitTypeLbl = new Label(order.unitLabel);
            unitTypeLbl.style.color   = cDim;
            unitTypeLbl.style.fontSize = 11;
            unitTypeLbl.style.marginBottom = 6;
            statsCol.Add(unitTypeLbl);

            // Order quantity
            var qtyHeader = new Label("ORDER QUANTITY");
            qtyHeader.style.color    = cDim;
            qtyHeader.style.fontSize = 10;
            qtyHeader.style.letterSpacing = 1;
            statsCol.Add(qtyHeader);

            string lastWord = order.unitLabel.Split(' ')[^1];
            string unitPlural = lastWord.EndsWith("s") || lastWord.EndsWith("y") ? (lastWord.EndsWith("y") && lastWord != "Artillery" ? lastWord.Substring(0, lastWord.Length - 1) + "ies" : lastWord) : lastWord + "s";
            if (lastWord == "Artillery") unitPlural = "Artillery";
            var qtyLbl = new Label($"{order.quantity:N0} {unitPlural}");
            qtyLbl.style.color = Color.white;
            qtyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            qtyLbl.style.fontSize = 15;
            qtyLbl.style.marginBottom = 4;
            statsCol.Add(qtyLbl);

            // Progress
            var progHeader = new Label("PROGRESS");
            progHeader.style.color    = cDim;
            progHeader.style.fontSize = 10;
            statsCol.Add(progHeader);

            var ts = GameManager.Instance?.TurnSystem as TurnSystem;
            float pct = order.progressPct;
            if (ts != null && order.monthsTotal > 0 && order.monthsRemaining > 0)
            {
                int[] daysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                int mIdx = Mathf.Max(0, ts.CurrentTurn - 1) % 12;
                float currentMonthProgress = (float)ts.currentDay / daysInMonth[mIdx];
                
                float completedMonths = order.monthsTotal - order.monthsRemaining;
                pct = ((completedMonths + currentMonthProgress) / (float)order.monthsTotal) * 100f;
            }
            pct = Mathf.Clamp(pct, 0f, 100f);
            UnityEngine.Debug.Log($"[UI] Drawing order {order.id}. Current progress: {pct:F1}% (monthsTotal: {order.monthsTotal}, monthsRemaining: {order.monthsRemaining}, currentDay: {ts?.currentDay})");

            Color progColor = pct >= 60f ? cGreen : pct >= 30f ? cYellow : cOrange;

            var progRow = new VisualElement();
            progRow.style.flexDirection  = FlexDirection.Row;
            progRow.style.alignItems     = Align.Center;
            progRow.style.marginBottom   = 6;
            statsCol.Add(progRow);

            var pctLbl = new Label($"{pct:F0}%");
            pctLbl.style.color = progColor;
            pctLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            pctLbl.style.fontSize = 14;
            pctLbl.style.marginRight = 8;
            progRow.Add(pctLbl);

            var barBg = new VisualElement();
            barBg.style.flexGrow        = 1;
            barBg.style.height          = 5;
            barBg.style.backgroundColor = new Color(1,1,1,0.08f);
            barBg.style.borderTopLeftRadius = barBg.style.borderTopRightRadius =
            barBg.style.borderBottomLeftRadius = barBg.style.borderBottomRightRadius = 2;

            var barFill = new VisualElement();
            barFill.style.height = Length.Percent(100);
            barFill.style.width  = Length.Percent(pct);
            barFill.style.backgroundColor = progColor;
            barFill.style.borderTopLeftRadius = barFill.style.borderTopRightRadius =
            barFill.style.borderBottomLeftRadius = barFill.style.borderBottomRightRadius = 2;
            barBg.Add(barFill);
            progRow.Add(barBg);

            // ETA and Monthly Output
            var statsRow = new VisualElement();
            statsRow.style.flexDirection  = FlexDirection.Row;
            statsRow.style.justifyContent = Justify.SpaceBetween;
            statsCol.Add(statsRow);

            void AddMiniStat(VisualElement parent, string label, string value)
            {
                var col = new VisualElement();
                var lbl = new Label(label);
                lbl.style.color   = cDim;
                lbl.style.fontSize = 7;
                col.Add(lbl);
                var val = new Label(value);
                val.style.color   = Color.white;
                val.style.unityFontStyleAndWeight = FontStyle.Bold;
                val.style.fontSize = 9;
                col.Add(val);
                parent.Add(col);
            }

            AddMiniStat(statsRow, "ETA",           $"{order.monthsRemaining} Months");
            AddMiniStat(statsRow, "MONTHLY OUTPUT", $"{order.monthlyOutput:F0} {unitPlural}");

            // View Details button
            var detailsBtn = new Button();
            detailsBtn.text = "VIEW DETAILS";
            detailsBtn.style.width           = Length.Percent(100);
            detailsBtn.style.height          = 24;
            detailsBtn.style.fontSize        = 8;
            detailsBtn.style.color           = cCyan;
            detailsBtn.style.backgroundColor = new Color(0f, 210/255f, 255/255f, 0.06f);
            SetBorderWidth(detailsBtn.style, 1);
            SetBorderColor(detailsBtn.style, new Color(0f, 210/255f, 255/255f, 0.2f));
            detailsBtn.style.borderTopLeftRadius = detailsBtn.style.borderTopRightRadius =
            detailsBtn.style.borderBottomLeftRadius = detailsBtn.style.borderBottomRightRadius = 3;
            card.Add(detailsBtn);
            detailsBtn.clicked += () =>
                NotificationManager.Instance?.ShowAlert($"{order.facilityName.ToUpper()} — ORDER DETAILS",
                    $"Unit: {order.unitLabel}\nOrdered: {order.quantity:N0} units\nProgress: {pct:F1}%\nETA: {order.monthsRemaining} months\nMonthly Output: {order.monthlyOutput:F0} units\nMonthly Cost: {UiFormat.Money(order.monthlyCostB * country.exchangeRate, string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol)}", false);

            grid.Add(card);
        }

        private void AddNewOrderForm(VisualElement parent, string category, string item, string id, int defaultQty,
            CountryDataSO country, string sym,
            Color cGreen, Color cCyan, Color cDim, Color cBorder, Color cPanel)
        {
            var form = new VisualElement();
            form.style.borderBottomWidth = 1;
            form.style.borderBottomColor = new Color(1,1,1,0.06f);
            form.style.paddingBottom = 10;
            form.style.marginBottom  = 10;

            // Image thumbnail + controls side by side
            var row = new VisualElement();
            row.style.flexDirection  = FlexDirection.Row;
            row.style.alignItems     = Align.Center;
            form.Add(row);

            // Thumbnail
            var thumb = new VisualElement();
            thumb.style.width           = 52;
            thumb.style.height          = 38;
            thumb.style.backgroundColor = new Color(0, 0, 0, 0.35f);
            thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius =
            thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 3;
            thumb.style.flexShrink     = 0;
            thumb.style.marginRight    = 10;

            string imgName = GetImageNameForUnit(item);
            if (!string.IsNullOrEmpty(imgName))
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imgName);
                if (tex != null)
                {
                    thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
#pragma warning disable CS0618
                    thumb.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                    thumb.style.backgroundColor = UnityEngine.Color.clear;
                }
            }

            row.Add(thumb);

            // Controls
            var controls = new VisualElement();
            controls.style.flexGrow = 1;
            row.Add(controls);

            // Category row
            var catRow = new VisualElement();
            catRow.style.flexDirection  = FlexDirection.Row;
            catRow.style.alignItems     = Align.Center;
            catRow.style.marginBottom   = 4;
            controls.Add(catRow);

            var catLbl = new Label("Category");
            catLbl.style.color   = cDim;
            catLbl.style.fontSize = 10;
            catLbl.style.width    = 55;
            catRow.Add(catLbl);

            var catField = new Label(category);
            catField.style.flexGrow = 1;
            catField.style.color    = Color.white;
            catField.style.fontSize = 11;
            catField.style.backgroundColor = new Color(0,0,0,0.3f);
            catField.style.borderTopLeftRadius = catField.style.borderTopRightRadius =
            catField.style.borderBottomLeftRadius = catField.style.borderBottomRightRadius = 2;
            SetBorderWidth(catField.style, 1);
            SetBorderColor(catField.style, new Color(1,1,1,0.1f));
            catField.style.paddingLeft  = 6;
            catField.style.paddingRight = 6;
            catField.style.paddingTop   = 2;
            catField.style.paddingBottom = 2;
            catRow.Add(catField);

            var qtyCaption = new Label("Quantity");
            qtyCaption.style.color   = cDim;
            qtyCaption.style.fontSize = 10;
            qtyCaption.style.marginLeft = 8;
            catRow.Add(qtyCaption);

            // Item + quantity stepper row
            var itemRow = new VisualElement();
            itemRow.style.flexDirection = FlexDirection.Row;
            itemRow.style.alignItems    = Align.Center;
            controls.Add(itemRow);

            var itemLbl = new Label("Item");
            itemLbl.style.color   = cDim;
            itemLbl.style.fontSize = 12;
            itemLbl.style.width   = 55;
            itemRow.Add(itemLbl);

            var itemField = new Label(item);
            itemField.style.flexGrow = 1;
            itemField.style.color    = Color.white;
            itemField.style.fontSize = 11;
            itemField.style.backgroundColor = new Color(0,0,0,0.3f);
            itemField.style.borderTopLeftRadius = itemField.style.borderTopRightRadius =
            itemField.style.borderBottomLeftRadius = itemField.style.borderBottomRightRadius = 2;
            SetBorderWidth(itemField.style, 1);
            SetBorderColor(itemField.style, new Color(1,1,1,0.1f));
            itemField.style.paddingLeft  = 6;
            itemField.style.paddingRight = 6;
            itemField.style.paddingTop   = 2;
            itemField.style.paddingBottom = 2;
            itemRow.Add(itemField);

            // Quantity stepper
            int[] qtyRef = { defaultQty };
            var qtyBox = new VisualElement();
            qtyBox.style.flexDirection  = FlexDirection.Row;
            qtyBox.style.alignItems     = Align.Center;
            qtyBox.style.marginLeft     = 8;
            qtyBox.style.backgroundColor = new Color(0,0,0,0.3f);
            SetBorderWidth(qtyBox.style, 1);
            SetBorderColor(qtyBox.style, new Color(1,1,1,0.12f));
            qtyBox.style.borderTopLeftRadius = qtyBox.style.borderTopRightRadius =
            qtyBox.style.borderBottomLeftRadius = qtyBox.style.borderBottomRightRadius = 2;
            itemRow.Add(qtyBox);

            var qtyDisplay = new Label($"{qtyRef[0]:N0}");
            qtyDisplay.style.color   = Color.white;
            qtyDisplay.style.fontSize = 12;
            qtyDisplay.style.width   = 36;
            qtyDisplay.style.unityTextAlign = TextAnchor.MiddleCenter;
            qtyDisplay.style.unityFontStyleAndWeight = FontStyle.Bold;

            var costLbl = new Label();
            costLbl.style.color = cDim;
            costLbl.style.fontSize = 9;
            costLbl.style.marginTop = 6;
            costLbl.style.unityTextAlign = UnityEngine.TextAnchor.MiddleRight;
            controls.Add(costLbl);

            void UpdateCost()
            {
                float costPerMonth = GetUnitCostB(id) * qtyRef[0] / 12f;
                int months         = GetUnitProductionMonths(id, qtyRef[0], country);
                float totalCost    = costPerMonth * months;
                
                if (months >= 999) 
                {
                    costLbl.text = $"<color=#FF3333>Requires {GetRequiredFactoryType(id)} Factory</color>";
                }
                else
                {
                    costLbl.text = $"Est. Cost: <color=#00D2FF>{UiFormat.Money(totalCost * country.exchangeRate, sym)}</color> ({months} months)";
                }
            }
            UpdateCost();

            var minusBtn = new Button() { text = "-" };
            minusBtn.style.width           = 16;
            minusBtn.style.height          = 18;
            minusBtn.style.fontSize        = 12;
            minusBtn.style.color           = Color.white;
            minusBtn.style.backgroundColor = Color.clear;
            minusBtn.style.paddingLeft     = 0;
            minusBtn.style.paddingRight    = 0;
            minusBtn.style.marginLeft      = 0;
            minusBtn.style.marginRight     = 0;
            minusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Max(step, qtyRef[0] - step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
                UpdateCost();
            };

            var plusBtn = new Button() { text = "+" };
            plusBtn.style.width           = 16;
            plusBtn.style.height          = 18;
            plusBtn.style.fontSize        = 12;
            plusBtn.style.color           = Color.white;
            plusBtn.style.backgroundColor = Color.clear;
            plusBtn.style.paddingLeft     = 0;
            plusBtn.style.paddingRight    = 0;
            plusBtn.style.marginLeft      = 0;
            plusBtn.style.marginRight     = 0;
            plusBtn.clicked += () =>
            {
                int step = id == "RIFLE" ? 100 : id == "APC" || id == "TNK" ? 5 : 1;
                qtyRef[0] = Mathf.Min(10000, qtyRef[0] + step);
                qtyDisplay.text = $"{qtyRef[0]:N0}";
                UpdateCost();
            };

            qtyBox.Add(minusBtn);
            qtyBox.Add(qtyDisplay);
            qtyBox.Add(plusBtn);



            // START PRODUCTION button
            var startBtn = new Button();
            startBtn.text = "START PRODUCTION";
            startBtn.style.width  = Length.Percent(100);
            startBtn.style.height = 24;
            startBtn.style.fontSize = 12;
            startBtn.style.marginTop = 6;
            startBtn.style.color = new Color(0, 0.05f, 0.1f, 1f);
            startBtn.style.backgroundColor = cGreen;
            startBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            startBtn.style.borderTopLeftRadius = startBtn.style.borderTopRightRadius =
            startBtn.style.borderBottomLeftRadius = startBtn.style.borderBottomRightRadius = 3;
            form.Add(startBtn);
            startBtn.clicked += () =>
            {
                var gm = GameManager.Instance;
                if (gm == null) return;
                
                int months = GetUnitProductionMonths(id, qtyRef[0], country);
                if (months >= 999)
                {
                    string reqCategory = GetRequiredFactoryType(id);
                    NotificationManager.Instance?.ShowAlert("PRODUCTION BLOCKED",
                        $"You do not have an operational {reqCategory} Defense Factory. Construct one from the Bases tab first.", true);
                    return;
                }
                
                float costPerMonth = GetUnitCostB(id) * qtyRef[0] / 12f;
                float totalCost    = costPerMonth * months;

                if (gm.playerBudget < totalCost * 0.5f)
                {
                    UnityEngine.Debug.Log($"[Production] Insufficient funds! Have: {gm.playerBudget}, Need: {totalCost * 0.5f}");
                    NotificationManager.Instance?.ShowAlert("INSUFFICIENT FUNDS",
                        $"Need at least {UiFormat.Money(totalCost * 0.5f * country.exchangeRate, sym)} to start production.", true);
                    return;
                }
                UnityEngine.Debug.Log($"[Production] Success! Adding {qtyRef[0]} {item} to queue. Cost: {totalCost}");

                // Create order
                string facName = GetFacilityName(id);
                var order = new ProductionOrder
                {
                    id           = id,
                    unitLabel    = item,
                    facilityName = facName,
                    category     = category,
                    quantity     = qtyRef[0],
                    monthsTotal  = months,
                    monthsRemaining = months,
                    monthlyCostB = costPerMonth,
                    monthlyOutput = qtyRef[0] / (float)Mathf.Max(1, months),
                };
                country.productionOrders.Insert(0, order);

                // Deduct first month upfront
                gm.playerBudget -= costPerMonth;
                country.treasury = gm.playerBudget;

                NotificationManager.Instance?.AddNotification(
                    $"PRODUCTION STARTED: {qtyRef[0]:N0} {item} — {months} months estimated. Monthly cost: {UiFormat.Money(costPerMonth * country.exchangeRate, sym)}", Color.cyan);

                RefreshMilitaryData();
                SwitchWeaponsTab(1); // Refresh Production tab
            };

            parent.Add(form);
        }

        // ── Production helper utilities ───────────────────────────────────────────

        private static float GetUnitCostB(string id)
        {
            return id switch
            {
                "RIFLE"       => 0.0002f,
                "APC"         => 0.08f,
                "TNK"         => 0.12f,
                "ARTILLERY"   => 0.05f,
                "UAV"         => 0.03f,
                "FTR"         => 0.8f,
                "BOMBER"      => 1.5f,
                "HEL"         => 0.4f,
                "PATROL_BOAT" => 0.1f,
                "FRG"         => 1.2f,
                "SUB"         => 2.5f,
                "DESTROYER"   => 3.0f,
                _             => 0.1f,
            };
        }

        private static float GetUnitValueB(string id)
        {
            return GetUnitCostB(id) * 1.2f; // Produced value is 20% above cost
        }

        private static string GetRequiredFactoryType(string id)
        {
            return id switch
            {
                "RIFLE" => "Small Arms",
                "APC" or "TNK" => "Armored Vehicles",
                "ARTILLERY" => "Artillery Systems",
                "FTR" or "BOMBER" or "HEL" or "UAV" => "Aircraft",
                "PATROL_BOAT" or "FRG" or "SUB" or "DESTROYER" => "Naval Vessels",
                _ => "Small Arms"
            };
        }

        private static int GetUnitProductionMonths(string id, int qty, CountryDataSO country)
        {
            string reqCategory = GetRequiredFactoryType(id);
            int totalLevel = 0;
            if (country.defenseFactories != null)
            {
                foreach (var fac in country.defenseFactories)
                {
                    if (fac.level > 0 && fac.produceType == reqCategory)
                    {
                        totalLevel += fac.level;
                    }
                }
            }
            
            if (totalLevel == 0) return 999;

            int baseMonths = id switch
            {
                "RIFLE"       => Mathf.CeilToInt(qty / 5000f),
                "APC"         => Mathf.CeilToInt(qty / 8f) + 1,
                "TNK"         => Mathf.CeilToInt(qty / 5f) + 2,
                "ARTILLERY"   => Mathf.CeilToInt(qty / 6f) + 1,
                "UAV"         => Mathf.CeilToInt(qty / 20f) + 1,
                "FTR"         => Mathf.CeilToInt(qty / 2f) + 4,
                "BOMBER"      => Mathf.CeilToInt(qty / 1f) + 6,
                "HEL"         => Mathf.CeilToInt(qty / 3f) + 2,
                "PATROL_BOAT" => Mathf.CeilToInt(qty / 1f) + 2,
                "FRG"         => Mathf.CeilToInt(qty / 1f) + 8,
                _             => Mathf.CeilToInt(qty / 3f) + 3,
            };
            float factoryBonus = 1f - Mathf.Clamp(totalLevel * 0.20f, 0f, 0.8f);
            return Mathf.Max(1, Mathf.RoundToInt(baseMonths * factoryBonus));
        }

        private static string GetFacilityName(string id)
        {
            return id switch
            {
                "RIFLE" or "APC" or "ARTILLERY" => "Army Production Line",
                "TNK"                            => "Tank Factory",
                "UAV" or "FTR" or "BOMBER"       => "Aviation Facility",
                "HEL"                            => "Helicopter Works",
                "PATROL_BOAT" or "FRG" or "SUB" or "DESTROYER" => "Naval Shipyard",
                _                                => "Defense Factory",
            };
        }

        private void EnsureProductionData(CountryDataSO country)
        {
            if (country.defenseFactories != null) country.defenseFactories.RemoveAll(f => f.produceType == "UAV");
            if (country.productionOrders != null) country.productionOrders.RemoveAll(o => o.id == "UAV");
            if (country.completedProductions != null) country.completedProductions.RemoveAll(c => c.unitLabel == "Military Trucks" || c.unitLabel == "Ammunition");

            if (country.defenseFactories == null)
                country.defenseFactories = new System.Collections.Generic.List<DefenseFactory>();
            if (country.productionOrders == null)
                country.productionOrders = new System.Collections.Generic.List<ProductionOrder>();
            if (country.completedProductions == null)
                country.completedProductions = new System.Collections.Generic.List<CompletedProductionItem>();

            // Seed default factories from base counts if list is empty
            if (country.defenseFactories.Count == 0 && country.baseDefenseFactoryCount >= 0)
            {
                if (country.unitTanks > 0 || country.unitAPCs > 0)
                    country.defenseFactories.Add(new DefenseFactory { id = "FAC_TANK",  factoryName = "Tank Factory",       produceType = "TNK",  level = 2, utilizationPct = 85f });
                if (country.unitAPCs > 0)
                    country.defenseFactories.Add(new DefenseFactory { id = "FAC_APC",   factoryName = "APC Production Line", produceType = "APC",  level = 2, utilizationPct = 78f });
                if (country.unitUAVs > 0 || country.unitFighters > 0)
                    country.defenseFactories.Add(new DefenseFactory { id = "FAC_FIGHTER", factoryName = "Aviation Plant",      produceType = "FTR",  level = 1, utilizationPct = 65f });
                if (country.unitArtillery > 0)
                    country.defenseFactories.Add(new DefenseFactory { id = "FAC_ART",   factoryName = "Artillery Works",     produceType = "ARTILLERY", level = 1, utilizationPct = 59f });
            }

            // Demo seed data for production orders and completed items has been removed.
        }

        private void EnsureProcurementData(CountryDataSO country)
        {
            if (country.availableOffers == null)
                country.availableOffers = new System.Collections.Generic.List<ProcurementOffer>();
            if (country.activeContracts == null)
                country.activeContracts = new System.Collections.Generic.List<ProcurementContract>();

            country.availableOffers.Clear();
            StateCraft.Systems.ProcurementMarketSystem.RefreshMarket(country);

            // No more mock active contracts, use live data.
            // Purge any lingering mock data that got saved into the ScriptableObject asset:
            if (country.activeContracts != null)
            {
                country.activeContracts.RemoveAll(c => c.id == "CON_CAESAR" || c.id == "CON_C130" || c.id == "CON_RADAR");
            }
        }

        private void EnsureResearchData(CountryDataSO country)
        {
            if (country.researchProjects != null) country.researchProjects.RemoveAll(r => r.id == "RES_DRONE");

            if (country.researchProjects == null)
                country.researchProjects = new System.Collections.Generic.List<ResearchProject>();

            if (country.researchProjects.Count == 0)
            {
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_FIGHTER", projectName = "Next-Gen Fighter Aircraft", category = "Aviation Tech",
                    monthsTotal = 14, monthsRemaining = 5, status = "In Progress", completedDate = ""
                });
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_ARMOR", projectName = "Next-Gen Composite Armor", category = "Materials",
                    monthsTotal = 18, monthsRemaining = 12, status = "In Progress", completedDate = ""
                });
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_CYBER", projectName = "Cyber Warfare Defenses Level 3", category = "Cyber Security",
                    monthsTotal = 8, monthsRemaining = 8, status = "Available", completedDate = ""
                });
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_BAL", projectName = "Improved Ballistics", category = "Infantry Weapons",
                    monthsTotal = 6, monthsRemaining = 0, status = "Completed", completedDate = "14 Jan 2026"
                });
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_RADAR", projectName = "Early Warning Radar", category = "Defense Systems",
                    monthsTotal = 12, monthsRemaining = 0, status = "Completed", completedDate = "02 Nov 2025"
                });
                country.researchProjects.Add(new ResearchProject {
                    id = "RES_LOG", projectName = "Mechanized Logistics", category = "Support",
                    monthsTotal = 8, monthsRemaining = 0, status = "Completed", completedDate = "28 Aug 2025"
                });
            }
        }



        private void AddWeaponRow(string name, string weaponClass, int quantity, float readinessPct, string condition, VisualElement grid, string imageName = null)
        {
            if (grid == null) return;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingTop = 12;
            row.style.paddingBottom = 12;
            row.style.paddingLeft = 10;
            row.style.paddingRight = 10;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(255/255f, 255/255f, 255/255f, 0.05f);

            row.RegisterCallback<MouseEnterEvent>(e => row.style.backgroundColor = new Color(255/255f, 255/255f, 255/255f, 0.03f));
            row.RegisterCallback<MouseLeaveEvent>(e => row.style.backgroundColor = Color.clear);

            // 1. Image placeholder
            var imgCol = new VisualElement();
            imgCol.style.width = 90;
            imgCol.style.height = 52;
            imgCol.style.flexShrink = 0;
            imgCol.style.marginRight = 10;
            imgCol.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            imgCol.style.borderTopLeftRadius = imgCol.style.borderTopRightRadius =
            imgCol.style.borderBottomLeftRadius = imgCol.style.borderBottomRightRadius = 3;
            
            if (!string.IsNullOrEmpty(imageName))
            {
                var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>($"EquipmentPhotos/{imageName}");
                if (tex != null)
                {
                    imgCol.style.backgroundImage = new StyleBackground(tex);
#pragma warning disable CS0618
                    imgCol.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                }
            }

            row.Add(imgCol);

            // 2. Name / Class
            var nameCol = new VisualElement();
            nameCol.style.flexGrow = 1;
            nameCol.style.flexDirection = FlexDirection.Column;
            nameCol.style.justifyContent = Justify.Center;

            var nameLbl = new Label(name);
            nameLbl.style.color = Color.white;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.fontSize = 18;
            nameCol.Add(nameLbl);

            var classLbl = new Label(weaponClass);
            classLbl.style.color = new Color(140/255f, 140/255f, 140/255f);
            classLbl.style.fontSize = 13;
            classLbl.style.marginTop = 1;
            nameCol.Add(classLbl);
            row.Add(nameCol);

            // 3. Quantity
            var qtyLbl = new Label(quantity == 0 ? "0" : quantity.ToString("N0"));
            qtyLbl.style.width = 68;
            qtyLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            qtyLbl.style.color = quantity == 0 ? new Color(80/255f, 80/255f, 80/255f) : Color.white;
            qtyLbl.style.fontSize = 18;
            row.Add(qtyLbl);

            // 4. Readiness
            var readinessCol = new VisualElement();
            readinessCol.style.width = 135;
            readinessCol.style.flexDirection = FlexDirection.Row;
            readinessCol.style.alignItems = Align.Center;
            readinessCol.style.justifyContent = Justify.Center;
            
            if (quantity > 0)
            {
                var readPctLbl = new Label($"{(int)(readinessPct * 100)}%");
                readPctLbl.style.width = 35;
                readPctLbl.style.fontSize = 16;
                readPctLbl.style.color = Color.white;
                readPctLbl.style.unityTextAlign = TextAnchor.MiddleRight;
                readPctLbl.style.marginRight = 8;
                readinessCol.Add(readPctLbl);

                var readBarContainer = new VisualElement();
                readBarContainer.style.width = 65;
                readBarContainer.style.height = 3;
                readBarContainer.style.backgroundColor = new Color(255/255f, 255/255f, 255/255f, 0.1f);
                readBarContainer.style.borderTopLeftRadius = readBarContainer.style.borderTopRightRadius =
                readBarContainer.style.borderBottomLeftRadius = readBarContainer.style.borderBottomRightRadius = 1;
                
                var readBarFill = new VisualElement();
                readBarFill.style.height = Length.Percent(100);
                readBarFill.style.width = Length.Percent(readinessPct * 100);
                readBarFill.style.borderTopLeftRadius = readBarFill.style.borderTopRightRadius =
                readBarFill.style.borderBottomLeftRadius = readBarFill.style.borderBottomRightRadius = 1;
                
                if      (readinessPct >= 0.8f) readBarFill.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
                else if (readinessPct >= 0.5f) readBarFill.style.backgroundColor = new Color(255/255f,   180/255f, 0);
                else                           readBarFill.style.backgroundColor = new Color(255/255f,   82/255f,  82/255f);
                
                readBarContainer.Add(readBarFill);
                readinessCol.Add(readBarContainer);
            }
            else
            {
                var nullLbl = new Label("—");
                nullLbl.style.color = new Color(120/255f, 120/255f, 120/255f);
                nullLbl.style.fontSize = 13;
                readinessCol.Add(nullLbl);
            }
            row.Add(readinessCol);

            // 5. Condition
            var condCol = new VisualElement();
            condCol.style.width = 72;
            condCol.style.flexDirection = FlexDirection.Row;
            condCol.style.alignItems = Align.Center;
            condCol.style.justifyContent = Justify.FlexEnd;

            if (quantity > 0)
            {
                var condDot = new VisualElement();
                condDot.style.width = 8;
                condDot.style.height = 8;
                condDot.style.borderTopLeftRadius = condDot.style.borderTopRightRadius =
                condDot.style.borderBottomLeftRadius = condDot.style.borderBottomRightRadius = 4;
                condDot.style.marginRight = 4;
                
                Color condColor;
                if      (condition == "Excellent") condColor = new Color(0f, 0.9f, 0.6f);
                else if (condition == "Good")      condColor = new Color(0.14f, 0.93f, 0.34f);
                else if (condition == "Fair")      condColor = new Color(255/255f, 180/255f, 0);
                else                               condColor = new Color(255/255f, 82/255f,  82/255f);
                
                condDot.style.backgroundColor = condColor;
                condCol.Add(condDot);

                var condLbl = new Label(condition);
                condLbl.style.color = condColor;
                condLbl.style.fontSize = 16;
                condCol.Add(condLbl);
            }
            else
            {
                var nullLbl = new Label("—");
                nullLbl.style.color = new Color(120/255f, 120/255f, 120/255f);
                nullLbl.style.fontSize = 13;
                condCol.Add(nullLbl);
            }
            row.Add(condCol);

            grid.Add(row);
        }

        private void ShowPurchaseModal(string name, string id, float baseCostB, int batchSize, bool isPersonnel, bool isProduce, float baseMineralsCost)
        {
            var country = GameManager.Instance.playerCountry;
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            
            var modal = new VisualElement() { style = { width = 450, backgroundColor = new Color(0.06f, 0.08f, 0.1f, 1f), borderTopLeftRadius=12, borderTopRightRadius=12, borderBottomLeftRadius=12, borderBottomRightRadius=12, overflow = Overflow.Hidden } };
            SetBorderColor(modal.style, new Color(0, 230/255f, 118/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var header = new VisualElement() { style = { backgroundColor = new Color(0.1f, 0.13f, 0.17f, 1f), paddingLeft = 24, paddingRight = 24, paddingTop = 16, paddingBottom = 16, borderBottomWidth = 1, borderBottomColor = new Color(1, 1, 1, 0.1f) } };
            string actionType = isPersonnel ? "RECRUIT" : (isProduce ? "PRODUCE" : "IMPORT");
            header.Add(new Label($"{actionType}: {name.ToUpper()}") { style = { color = Color.white, fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1 } });
            modal.Add(header);

            var content = new VisualElement() { style = { paddingLeft = 24, paddingRight = 24, paddingTop = 20, paddingBottom = 20 } };

            var sliderLabel = new Label();
            sliderLabel.style.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            sliderLabel.style.fontSize = 11;
            sliderLabel.style.marginBottom = 10;
            sliderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            content.Add(sliderLabel);

            int maxBatches = 100;
            
            if (isPersonnel)
            {
                int trainingCapacity = 0;
                int currentTrainees = GetTotalInTraining(country, id);
                int housingCapacity = 0;
                int currentTotal = 0;

                switch (id)
                {
                    case "INF":
                        trainingCapacity = 5000 + (country.baseArmyCount * 15000);
                        currentTotal = country.armyStats.personnel;
                        foreach (var b in country.militaryBases) if (b.branch == "ARMY") housingCapacity += b.capacity;
                        break;
                    case "POLICE":
                        trainingCapacity = 5000 + (country.baseArmyCount * 5000);
                        currentTotal = country.nationalPoliceSize;
                        foreach (var b in country.militaryBases) if (b.branch == "ARMY") housingCapacity += b.capacity;
                        break;
                    case "NAVY_PERS":
                        trainingCapacity = 1000 + (country.baseNavyCount * 3000);
                        currentTotal = country.navyStats.personnel;
                        foreach (var b in country.militaryBases) if (b.branch == "NAVY") housingCapacity += b.capacity;
                        break;
                    case "AIR_PERS":
                        trainingCapacity = 500 + (country.baseAirForceCount * 2000);
                        currentTotal = country.airForceStats.personnel;
                        foreach (var b in country.militaryBases) if (b.branch == "AIR" || b.branch == "AIR FORCE") housingCapacity += b.capacity;
                        break;
                    case "RDF_PERS":
                        trainingCapacity = 200 + (country.baseRDFCount * 1000);
                        currentTotal = country.rdfStats.personnel;
                        foreach (var b in country.militaryBases) if (b.branch == "RDF") housingCapacity += b.capacity;
                        break;
                }
                
                int availableSpace = Mathf.Max(0, trainingCapacity - currentTrainees);
                maxBatches = Mathf.Max(1, availableSpace / batchSize);

                var housingLabel = new Label($"Base Housing Capacity: {currentTotal:N0} active / {housingCapacity:N0} limit") { style = { color = new Color(0.8f, 0.8f, 0.8f, 1f), fontSize = 10, marginTop = -5, marginBottom = 15 } };
                if (currentTotal > housingCapacity) housingLabel.style.color = new Color(1f, 0.3f, 0.3f);
                content.Add(housingLabel);
            }

            SliderInt amountSlider = new SliderInt(1, maxBatches) { style = { marginBottom = 20 } };
            
            var totalCostLabel = new Label();
            totalCostLabel.style.color = new Color(0, 230/255f, 118/255f, 1f);
            totalCostLabel.style.fontSize = 13;
            totalCostLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            totalCostLabel.style.marginBottom = 24;
            totalCostLabel.style.marginTop = 10;
            totalCostLabel.style.paddingTop = 15;
            totalCostLabel.style.borderTopWidth = 1;
            totalCostLabel.style.borderTopColor = new Color(1, 1, 1, 0.1f);
            content.Add(totalCostLabel);

            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "$" : country.currencySymbol;

            // Local helper — also invoked immediately so quantity/cost show on open
            System.Action<int> updateLabels = batches => {
                int totalUnits = batches * batchSize;
                float totalCost = batches * baseCostB;
                float totalMinerals = batches * baseMineralsCost;
                sliderLabel.text = $"QUANTITY: {totalUnits:N0} units  ({batches} batch{(batches == 1 ? "" : "es")} × {batchSize:N0} per batch)";
                string costText = $"Total Cost:  {UiFormat.Money(totalCost * country.exchangeRate, sym)}";
                if (isProduce && totalMinerals > 0) costText += $"   |   Minerals: {totalMinerals:N0} T";
                totalCostLabel.text = costText;
            };

            amountSlider.RegisterValueChangedCallback(evt => updateLabels(evt.newValue));
            amountSlider.value = 1;
            updateLabels(1); // populate immediately — setting value=1 won't fire callback
            content.Insert(1, amountSlider);


            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd, marginTop = 10 } };
            
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            cancelBtn.style.color = Color.white;
            cancelBtn.style.width = 120;
            cancelBtn.style.height = 36;
            cancelBtn.style.marginRight = 12;
            cancelBtn.style.borderTopLeftRadius = 6; cancelBtn.style.borderTopRightRadius = 6; cancelBtn.style.borderBottomLeftRadius = 6; cancelBtn.style.borderBottomRightRadius = 6;
            cancelBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionRow.Add(cancelBtn);

            // Button label: RECRUIT for personnel, PRODUCE for local, IMPORT ORDER for weapons
            string confirmLabel = isPersonnel ? "RECRUIT" : (isProduce ? "PRODUCE" : "IMPORT ORDER");
            Color confirmColor = isPersonnel
                ? new Color(36/255f, 174/255f, 96/255f)   // green for recruit
                : isProduce
                ? new Color(0f, 140/255f, 220/255f)       // blue for produce
                : new Color(25/255f, 127/255f, 229/255f); // blue for import
            var confirmBtn = new Button() { text = confirmLabel };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = confirmColor;
            confirmBtn.style.color = Color.white;
            confirmBtn.style.width = 160;
            confirmBtn.style.height = 36;
            confirmBtn.style.borderTopLeftRadius = 6; confirmBtn.style.borderTopRightRadius = 6; confirmBtn.style.borderBottomLeftRadius = 6; confirmBtn.style.borderBottomRightRadius = 6;
            confirmBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            confirmBtn.clicked += () => {
                float localCost = baseCostB * country.exchangeRate * amountSlider.value;
                float mineralsCost = baseMineralsCost * amountSlider.value;
                int amount = batchSize * amountSlider.value;
                RecruitUnit(id, localCost, amount, isPersonnel, isProduce, mineralsCost);
                overlay.parent?.Remove(overlay);
            };
            
            // Hover effect for Confirm Button
            Color confirmHover = isPersonnel
                ? new Color(46/255f, 194/255f, 116/255f)
                : new Color(35/255f, 147/255f, 249/255f);
            confirmBtn.RegisterCallback<MouseEnterEvent>(e => confirmBtn.style.backgroundColor = confirmHover);
            confirmBtn.RegisterCallback<MouseLeaveEvent>(e => confirmBtn.style.backgroundColor = confirmColor);
            
            // Hover effect for Cancel Button
            cancelBtn.RegisterCallback<MouseEnterEvent>(e => cancelBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f));
            cancelBtn.RegisterCallback<MouseLeaveEvent>(e => cancelBtn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f));

            actionRow.Add(confirmBtn);
            
            content.Add(actionRow);
            modal.Add(content);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private void ShowNegotiationModal(ProcurementOffer offer, System.Action onConfirm)
        {
            var overlay = new VisualElement() { style = { position = Position.Absolute, left = 0, top = 0, right = 0, bottom = 0, backgroundColor = new Color(0, 0, 0, 0.85f), justifyContent = Justify.Center, alignItems = Align.Center } };
            
            var modal = new VisualElement() { style = { width = 450, backgroundColor = new Color(0.06f, 0.08f, 0.1f, 1f), borderTopLeftRadius=12, borderTopRightRadius=12, borderBottomLeftRadius=12, borderBottomRightRadius=12, overflow = Overflow.Hidden } };
            SetBorderColor(modal.style, new Color(0, 210/255f, 130/255f, 0.5f));
            SetBorderWidth(modal.style, 1);
            overlay.Add(modal);

            var header = new VisualElement() { style = { backgroundColor = new Color(0.1f, 0.13f, 0.17f, 1f), paddingLeft = 24, paddingRight = 24, paddingTop = 16, paddingBottom = 16, borderBottomWidth = 1, borderBottomColor = new Color(1, 1, 1, 0.1f) } };
            header.Add(new Label($"NEGOTIATE CONTRACT") { style = { color = Color.white, fontSize = 15, unityFontStyleAndWeight = FontStyle.Bold, letterSpacing = 1 } });
            modal.Add(header);

            var content = new VisualElement() { style = { paddingLeft = 24, paddingRight = 24, paddingTop = 20, paddingBottom = 20 } };

            var itemLbl = new Label(offer.title) { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } };
            content.Add(itemLbl);
            
            var supLbl = new Label($"{offer.supplier}, {offer.country} {offer.flagEmoji}") { style = { color = new Color(0.6f, 0.6f, 0.6f), fontSize = 12, marginBottom = 20 } };
            content.Add(supLbl);

            float monthly = offer.totalCostB / offer.deliveryMonths;
            var detailsBox = new VisualElement() { style = { backgroundColor = new Color(0,0,0,0.3f), paddingLeft=12, paddingRight=12, paddingTop=12, paddingBottom=12, borderTopLeftRadius=6, borderTopRightRadius=6, borderBottomLeftRadius=6, borderBottomRightRadius=6, marginBottom = 20 } };
            
            detailsBox.Add(new Label($"Total Cost: {offer.totalCost}") { style = { color = Color.white, fontSize = 12, marginBottom = 4 } });
            detailsBox.Add(new Label($"Delivery Time: {offer.deliveryTime}") { style = { color = Color.white, fontSize = 12, marginBottom = 12 } });
            
            detailsBox.Add(new Label($"MONTHLY INSTALLMENT:") { style = { color = new Color(0f, 210/255f, 130/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 } });
            detailsBox.Add(new Label($"₦{monthly:F1} Billion / month for {offer.deliveryMonths} months") { style = { color = Color.white, fontSize = 14, unityFontStyleAndWeight = FontStyle.Bold } });
            content.Add(detailsBox);

            var actionRow = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.FlexEnd } };
            
            var cancelBtn = new Button(() => overlay.parent?.Remove(overlay)) { text = "CANCEL" };
            cancelBtn.AddToClassList("button-premium");
            cancelBtn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            cancelBtn.style.color = Color.white;
            cancelBtn.style.width = 120;
            cancelBtn.style.height = 36;
            cancelBtn.style.marginRight = 12;
            cancelBtn.style.borderTopLeftRadius = 6; cancelBtn.style.borderTopRightRadius = 6; cancelBtn.style.borderBottomLeftRadius = 6; cancelBtn.style.borderBottomRightRadius = 6;
            cancelBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionRow.Add(cancelBtn);

            var confirmBtn = new Button() { text = "SIGN CONTRACT" };
            confirmBtn.AddToClassList("button-premium");
            confirmBtn.style.backgroundColor = new Color(0, 210/255f, 130/255f, 1f);
            confirmBtn.style.color = Color.black;
            confirmBtn.style.width = 160;
            confirmBtn.style.height = 36;
            confirmBtn.style.borderTopLeftRadius = 6; confirmBtn.style.borderTopRightRadius = 6; confirmBtn.style.borderBottomLeftRadius = 6; confirmBtn.style.borderBottomRightRadius = 6;
            confirmBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            confirmBtn.clicked += () => {
                onConfirm?.Invoke();
                overlay.parent?.Remove(overlay);
            };
            
            confirmBtn.RegisterCallback<MouseEnterEvent>(e => confirmBtn.style.backgroundColor = new Color(0, 255/255f, 150/255f, 1f));
            confirmBtn.RegisterCallback<MouseLeaveEvent>(e => confirmBtn.style.backgroundColor = new Color(0, 210/255f, 130/255f, 1f));
            
            cancelBtn.RegisterCallback<MouseEnterEvent>(e => cancelBtn.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f));
            cancelBtn.RegisterCallback<MouseLeaveEvent>(e => cancelBtn.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f));

            actionRow.Add(confirmBtn);
            content.Add(actionRow);
            modal.Add(content);

            var dashboard = _root?.Q<VisualElement>("MilitaryDashboard");
            if (dashboard != null) dashboard.Add(overlay);
            else _root?.Add(overlay);
        }

        private void PopulateProcurement(CountryDataSO country)
        {
            if (_wPanelProcurement == null) return;
            _wPanelProcurement.Clear();
            _wPanelProcurement.style.flexDirection = FlexDirection.Column;
            _wPanelProcurement.style.flexGrow = 1;

            Color cGreen  = new Color(0f, 210/255f, 130/255f);
            Color cCyan   = new Color(0.1f, 0.5f, 0.9f);
            Color cYellow = new Color(1f, 210/255f, 0f);
            Color cOrange = new Color(1f, 130/255f, 0f);
            Color cRed    = new Color(1f, 60/255f, 60/255f);
            Color cDim    = new Color(1, 1, 1, 0.5f);
            Color cPanel  = new Color(0, 0, 0, 0.35f);
            Color cBorder = new Color(1, 1, 1, 0.08f);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 12;
            scroll.style.paddingTop = 8;
            scroll.style.paddingBottom = 8;
            _wPanelProcurement.Add(scroll);

            var root = scroll.contentContainer;
            root.style.paddingRight = 20;

            // ── HEADER ───────────────────────────────────────────────────────
            EnsureProcurementData(country);
            var headerLbl = new Label("DEFENSE PROCUREMENT");
            headerLbl.style.color = Color.white;
            headerLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLbl.style.fontSize = 14;
            root.Add(headerLbl);

            var subLbl = new Label("Acquire advanced military equipment from trusted international partners.");
            subLbl.style.color = cDim;
            subLbl.style.fontSize = 9;
            subLbl.style.marginBottom = 16;
            root.Add(subLbl);

            // ── KPIs ─────────────────────────────────────────────────────────
            var kpiRow = new VisualElement();
            kpiRow.style.flexDirection = FlexDirection.Row;
            kpiRow.style.flexWrap = Wrap.Wrap;
            kpiRow.style.marginBottom = 16;
            root.Add(kpiRow);

            void AddKpi(string title, string value, string subtitle, string icon)
            {
                var card = new VisualElement();
                card.style.flexGrow = 1;
                card.style.flexBasis = Length.Percent(18); // ~5 items per row
                card.style.backgroundColor = cPanel;
                SetBorderWidth(card.style, 1);
                SetBorderColor(card.style, cBorder);
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
                card.style.paddingTop = 10;
                card.style.paddingBottom = 10;
                card.style.paddingLeft = 12;
                card.style.paddingRight = 12;
                card.style.marginRight = 8;
                card.style.marginBottom = 8;

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                card.Add(row);

                var iconLbl = new Label(icon);
                iconLbl.AddToClassList("fa-icon");
                iconLbl.style.fontSize = 18;
                iconLbl.style.color = cDim;
                iconLbl.style.marginRight = 12;
                iconLbl.style.width = 24;
                iconLbl.style.unityTextAlign = TextAnchor.MiddleCenter;
                row.Add(iconLbl);

                var textCol = new VisualElement();
                textCol.style.flexDirection = FlexDirection.Column;
                row.Add(textCol);

                var titleLbl = new Label(title);
                titleLbl.style.color = cDim;
                titleLbl.style.fontSize = 7;
                textCol.Add(titleLbl);

                var valLbl = new Label(value);
                valLbl.style.color = Color.white;
                valLbl.style.fontSize = 12;
                valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                valLbl.style.marginTop = 2;
                textCol.Add(valLbl);

                var subLbl2 = new Label(subtitle);
                subLbl2.style.color = cDim;
                subLbl2.style.fontSize = 7;
                subLbl2.style.marginTop = 2;
                textCol.Add(subLbl2);

                kpiRow.Add(card);
            }

            float totalCommitments = 0f;
            int pendingDeliveries = 0;
            float totalDeliveryMonths = 0f;
            foreach (var c in country.activeContracts)
            {
                totalCommitments += c.totalCostB;
                pendingDeliveries++;
                totalDeliveryMonths += c.monthsTotal;
            }
            float avgDelivery = pendingDeliveries > 0 ? totalDeliveryMonths / pendingDeliveries : 0f;

            AddKpi("PROCUREMENT BUDGET", "₦412.6B", "Available for spending", "\uf155");
            AddKpi("ACTIVE NEGOTIATIONS", country.activeContracts.Count.ToString(), "In progress", "\uf2b5");
            AddKpi("PENDING DELIVERIES", pendingDeliveries.ToString(), "Scheduled shipments", "\uf0d1");
            AddKpi("TOTAL COMMITMENTS", $"₦{totalCommitments:F1}B", "Value of all contracts", "\uf15c");
            AddKpi("AVERAGE DELIVERY TIME", $"{avgDelivery:F1}", "Months", "\uf017");

            // ── 3 COLUMN LAYOUT ─────────────────────────────────────────────
            var columns = new VisualElement();
            columns.style.flexDirection = FlexDirection.Row;
            columns.style.flexGrow = 1;
            columns.style.width = Length.Percent(100);
            root.Add(columns);

            var leftCol = new VisualElement();
            leftCol.style.width = 200;
            leftCol.style.minWidth = 200;
            leftCol.style.flexShrink = 0;
            leftCol.style.marginRight = 12;
            columns.Add(leftCol);

            var midCol = new VisualElement();
            midCol.style.flexGrow = 1;
            midCol.style.minWidth = 0;
            midCol.style.marginRight = 12;
            columns.Add(midCol);

            var rightCol = new VisualElement();
            rightCol.style.width = 300;
            rightCol.style.minWidth = 300;
            rightCol.style.flexShrink = 0;
            columns.Add(rightCol);

            // ── LEFT COL (Filters) ──────────────────────────────────────────
            var filterHeader = new VisualElement();
            filterHeader.style.flexDirection = FlexDirection.Row;
            filterHeader.style.justifyContent = Justify.SpaceBetween;
            filterHeader.style.marginBottom = 10;
            leftCol.Add(filterHeader);
            
            var filterLbl = new Label("FILTERS");
            filterLbl.style.color = cDim;
            filterLbl.style.fontSize = 9;
            filterLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            filterHeader.Add(filterLbl);

            var resetLbl = new Label("Reset");
            resetLbl.style.color = cDim;
            resetLbl.style.fontSize = 8;
            resetLbl.RegisterCallback<ClickEvent>(e => {
                _procurementCategoryFilter = "All Categories";
                PopulateProcurement(country);
            });
            filterHeader.Add(resetLbl);

            void AddFilterSection(string title)
            {
                var lbl = new Label(title);
                lbl.style.color = Color.white;
                lbl.style.fontSize = 8;
                lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                lbl.style.marginTop = 12;
                lbl.style.marginBottom = 8;
                leftCol.Add(lbl);
            }

            AddFilterSection("CATEGORY");
            string[] categories = { "All Categories", "Army Equipment", "Air Force Equipment", "Naval Equipment" };
            foreach(var c in categories) {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 6;
                var box = new VisualElement();
                box.style.width = 12; box.style.height = 12;
                box.style.borderTopLeftRadius = box.style.borderTopRightRadius = 
                box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 2;
                box.style.alignItems = Align.Center;
                box.style.justifyContent = Justify.Center;
                SetBorderWidth(box.style, 1);
                SetBorderColor(box.style, c == _procurementCategoryFilter ? cGreen : cDim);
                
                if (c == _procurementCategoryFilter)
                {
                    box.style.backgroundColor = new Color(0f, 210/255f, 130/255f, 0.15f);
                    var checkLbl = new Label("\uf00c");
                    checkLbl.AddToClassList("fa-icon");
                    checkLbl.style.fontSize = 9;
                    checkLbl.style.color = cGreen;
                    checkLbl.style.marginTop = 1;
                    box.Add(checkLbl);
                }
                row.Add(box);
                var l = new Label(c);
                l.style.color = c == _procurementCategoryFilter ? Color.white : cDim;
                l.style.fontSize = 8;
                l.style.marginLeft = 6;
                row.Add(l);
                
                string loopCat = c;
                row.RegisterCallback<ClickEvent>(e => {
                    _procurementCategoryFilter = loopCat;
                    PopulateProcurement(country);
                });
                
                leftCol.Add(row);
            }



            // ── MID COL (Available Offers) ──────────────────────────────────
            var offersHeader = new Label("AVAILABLE OFFERS");
            offersHeader.style.color = Color.white;
            offersHeader.style.fontSize = 12;
            offersHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            offersHeader.style.marginBottom = 10;
            midCol.Add(offersHeader);

            var offersList = new VisualElement();
            offersList.style.flexDirection = FlexDirection.Column;
            midCol.Add(offersList);

            void AddOffer(ProcurementOffer offer, string imageName = null)
            {
                var title = offer.title;
                var sub = offer.subtitle;
                var tags = offer.tags;
                var sup = offer.supplier;
                var ctry = offer.country;
                var unitC = offer.unitCost;
                var totC = offer.totalCost;
                var qty = offer.quantity;
                var deliv = offer.deliveryTime;
                var flag = offer.flagEmoji;
                var card = new VisualElement();
                card.style.flexDirection = FlexDirection.Row;
                card.style.backgroundColor = cPanel;
                SetBorderWidth(card.style, 1);
                SetBorderColor(card.style, cBorder);
                card.style.borderTopLeftRadius = card.style.borderTopRightRadius = 
                card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
                card.style.paddingTop = card.style.paddingBottom = 10;
                card.style.paddingLeft = card.style.paddingRight = 10;
                card.style.marginBottom = 8;

                // Thumb Placeholder
                var thumb = new VisualElement();
                thumb.style.width = 100;
                thumb.style.height = 70;
                thumb.style.backgroundColor = new Color(0,0,0,0.5f);
                thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = 

                thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;
                thumb.style.marginRight = 12;
                if (!string.IsNullOrEmpty(imageName))
                {
                    var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                    if (tex != null)
                    {
                        thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
#pragma warning disable CS0618
                        thumb.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                        thumb.style.backgroundColor = UnityEngine.Color.clear;
                    }
                }
                var flagLbl = new Label(flag);
                flagLbl.style.position = Position.Absolute;
                flagLbl.style.top = 4; flagLbl.style.left = 4;
                flagLbl.style.fontSize = 12;
                thumb.Add(flagLbl);
                card.Add(thumb);

                // Info Col
                var infoCol = new VisualElement();
                infoCol.style.flexGrow = 1;
                infoCol.style.justifyContent = Justify.SpaceBetween;
                card.Add(infoCol);

                var titleLbl = new Label(title);
                titleLbl.style.color = Color.white;
                titleLbl.style.fontSize = 20;
                titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
                infoCol.Add(titleLbl);

                var subLblText = new Label(sub);
                subLblText.style.color = cDim;
                subLblText.style.fontSize = 14;
                subLblText.style.marginBottom = 6;
                infoCol.Add(subLblText);

                var tagsRow = new VisualElement();
                tagsRow.style.flexDirection = FlexDirection.Row;
                tagsRow.style.marginBottom = 6;
                foreach(var t in tags) {
                    var tl = new Label(t);
                    tl.style.color = cGreen;
                    tl.style.fontSize = 13;
                    tl.style.backgroundColor = new Color(0, 210/255f, 130/255f, 0.1f);
                    SetBorderWidth(tl.style, 1);
                    SetBorderColor(tl.style, new Color(0, 210/255f, 130/255f, 0.3f));
                    tl.style.paddingLeft = tl.style.paddingRight = 4;
                    tl.style.paddingTop = tl.style.paddingBottom = 2;
                    tl.style.marginRight = 4;
                    tl.style.borderTopLeftRadius = tl.style.borderTopRightRadius = 
                    tl.style.borderBottomLeftRadius = tl.style.borderBottomRightRadius = 2;
                    tagsRow.Add(tl);
                }
                infoCol.Add(tagsRow);

                var supRow = new VisualElement();
                supRow.style.flexDirection = FlexDirection.Row;
                var supL1 = new Label("Supplier:"); supL1.style.color = cDim; supL1.style.fontSize = 14; supL1.style.width = 65;
                var supL2 = new Label(sup); supL2.style.color = Color.white; supL2.style.fontSize = 14;
                supRow.Add(supL1); supRow.Add(supL2);
                infoCol.Add(supRow);

                var ctryRow = new VisualElement();
                ctryRow.style.flexDirection = FlexDirection.Row;
                var ctryL1 = new Label("Country:"); ctryL1.style.color = cDim; ctryL1.style.fontSize = 14; ctryL1.style.width = 65;
                var ctryL2 = new Label(ctry); ctryL2.style.color = Color.white; ctryL2.style.fontSize = 14;
                ctryRow.Add(ctryL1); ctryRow.Add(ctryL2);
                infoCol.Add(ctryRow);

                // Stats Col
                var statsCol = new VisualElement();
                statsCol.style.width = 240;
                statsCol.style.marginLeft = 20;
                statsCol.style.justifyContent = Justify.SpaceBetween;
                card.Add(statsCol);

                var sRow1 = new VisualElement(); sRow1.style.flexDirection = FlexDirection.Row; sRow1.style.justifyContent = Justify.SpaceBetween;
                var sCol1 = new VisualElement();
                var sc1l1 = new Label("UNIT COST"); sc1l1.style.color = cDim; sc1l1.style.fontSize = 12; sCol1.Add(sc1l1);
                var sc1l2 = new Label(unitC); sc1l2.style.color = Color.white; sc1l2.style.fontSize = 18; sc1l2.style.unityFontStyleAndWeight = FontStyle.Bold; sCol1.Add(sc1l2);
                var sCol2 = new VisualElement();
                var sc2l1 = new Label("QUANTITY"); sc2l1.style.color = cDim; sc2l1.style.fontSize = 12; sCol2.Add(sc2l1);
                var sc2l2 = new Label(qty); sc2l2.style.color = Color.white; sc2l2.style.fontSize = 18; sc2l2.style.unityFontStyleAndWeight = FontStyle.Bold; sCol2.Add(sc2l2);
                sRow1.Add(sCol1); sRow1.Add(sCol2);
                statsCol.Add(sRow1);

                var sRow2 = new VisualElement(); sRow2.style.flexDirection = FlexDirection.Row; sRow2.style.justifyContent = Justify.SpaceBetween;
                var sCol3 = new VisualElement();
                var sc3l1 = new Label("TOTAL COST"); sc3l1.style.color = cDim; sc3l1.style.fontSize = 12; sCol3.Add(sc3l1);
                var sc3l2 = new Label(totC); sc3l2.style.color = Color.white; sc3l2.style.fontSize = 22; sc3l2.style.unityFontStyleAndWeight = FontStyle.Bold; sCol3.Add(sc3l2);
                var sCol4 = new VisualElement();
                var sc4l1 = new Label("DELIVERY TIME"); sc4l1.style.color = cDim; sc4l1.style.fontSize = 12; sCol4.Add(sc4l1);
                var sc4l2 = new Label(deliv); sc4l2.style.color = Color.white; sc4l2.style.fontSize = 18; sc4l2.style.unityFontStyleAndWeight = FontStyle.Bold; sCol4.Add(sc4l2);
                sRow2.Add(sCol3); sRow2.Add(sCol4);
                statsCol.Add(sRow2);

                var btnRow = new VisualElement(); btnRow.style.flexDirection = FlexDirection.Row;
                var negBtn = new Button() { text = "NEGOTIATE" };
                negBtn.style.flexGrow = 1;
                negBtn.style.height = 22;
                negBtn.style.backgroundColor = new Color(0, 210/255f, 130/255f, 0.1f);
                negBtn.style.color = cGreen;
                negBtn.style.fontSize = 11;
                negBtn.style.marginRight = 4;
                SetBorderWidth(negBtn.style, 1);
                SetBorderColor(negBtn.style, new Color(0, 210/255f, 130/255f, 0.35f));

                negBtn.clicked += () => {
                    ShowNegotiationModal(offer, () => {
                        var c = GameManager.Instance.playerCountry;
                        float monthly = offer.totalCostB / offer.deliveryMonths;
                        UnityEngine.Debug.Log($"[Procurement] treasury={c.treasury}, monthly={monthly}, playerBudget={GameManager.Instance.playerBudget}");
                        
                        // Treat costs as Local Currency, while Treasury is in USD.
                        // So the required USD is monthly / exchangeRate.
                        float monthlyUSD = monthly / c.exchangeRate;

                        if (GameManager.Instance.playerBudget >= monthlyUSD)
                        {
                            var contract = new ProcurementContract {
                                id = "CON_" + offer.id,
                                unitLabel = offer.title,
                                supplier = offer.supplier + ", " + offer.country,
                                category = offer.subtitle,
                                quantity = offer.unitQuantity,
                                monthsTotal = offer.deliveryMonths,
                                monthsRemaining = offer.deliveryMonths,
                                totalCostB = offer.totalCostB,
                                monthlyCostB = monthlyUSD,
                                status = "Technical Evaluation",
                                produceType = offer.produceType,
                                unitQuantity = offer.unitQuantity
                            };
                            c.activeContracts.Add(contract);
                            c.availableOffers.Remove(offer);
                            PopulateProcurement(c);
                        }
                        else
                        {
                            UI.NotificationManager.Instance?.AddNotification($"INSUFFICIENT FUNDS: Need ₦{monthly:F1}B (USD {monthlyUSD:F2}B) for first installment of {offer.title}.", UnityEngine.Color.red);
                        }
                    });
                };

                btnRow.Add(negBtn);
                statsCol.Add(btnRow);

                offersList.Add(card);
            }

            foreach(var offer in country.availableOffers.ToArray())
            {
                bool pass = true;
                if (_procurementCategoryFilter != "All Categories")
                {
                    if (_procurementCategoryFilter == "Army Equipment" && offer.produceType != "APC" && offer.produceType != "TNK" && offer.produceType != "RIFLE" && offer.produceType != "ARTILLERY") pass = false;
                    if (_procurementCategoryFilter == "Air Force Equipment" && offer.produceType != "FTR" && offer.produceType != "BOMBER" && offer.produceType != "HEL" && offer.produceType != "UAV") pass = false;
                    if (_procurementCategoryFilter == "Naval Equipment" && offer.produceType != "PATROL_BOAT" && offer.produceType != "FRG" && offer.produceType != "SUB" && offer.produceType != "DESTROYER") pass = false;
                }
                
                if (pass)
                {
                    AddOffer(offer, GetImageNameForUnit(offer.title + " " + offer.subtitle));
                }
            }

            // ── RIGHT COL (Negotiations & Summary) ──────────────────────────
            var rightTopRow = new VisualElement(); rightTopRow.style.flexDirection = FlexDirection.Row; rightTopRow.style.justifyContent = Justify.SpaceBetween; rightTopRow.style.marginBottom = 10;
            var anLbl = new Label("ACTIVE NEGOTIATIONS"); anLbl.style.color = Color.white; anLbl.style.fontSize = 12; anLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            rightTopRow.Add(anLbl);
            rightCol.Add(rightTopRow);

            var anList = new VisualElement();
            anList.style.backgroundColor = cPanel;
            SetBorderWidth(anList.style, 1);
            SetBorderColor(anList.style, cBorder);
            anList.style.borderTopLeftRadius = anList.style.borderTopRightRadius = 
            anList.style.borderBottomLeftRadius = anList.style.borderBottomRightRadius = 4;
            anList.style.paddingTop = anList.style.paddingBottom = 8;
            anList.style.paddingLeft = anList.style.paddingRight = 10;
            anList.style.marginBottom = 12;
            rightCol.Add(anList);

            void AddNegItem(string flag, string title, string sub, string val, string status, Color statColor)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 16;
                var flg = new Label(flag); flg.AddToClassList("fa-icon"); flg.style.color = cDim; flg.style.fontSize = 18; flg.style.marginRight = 12; row.Add(flg);
                var c1 = new VisualElement();
                c1.style.flexGrow = 1;
                var t = new Label(title); t.style.color = Color.white; t.style.fontSize = 12; t.style.unityFontStyleAndWeight = FontStyle.Bold; t.style.marginBottom = 4; c1.Add(t);
                var s = new Label(sub); s.style.color = cDim; s.style.fontSize = 10; s.style.marginBottom = 8; c1.Add(s);
                var vRow = new VisualElement(); vRow.style.flexDirection = FlexDirection.Row; vRow.style.marginTop = 4;
                var vL1 = new Label("Value: "); vL1.style.color = cDim; vL1.style.fontSize = 10; vL1.style.width = 45;
                var vL2 = new Label(val); vL2.style.color = Color.white; vL2.style.fontSize = 10; vL2.style.unityFontStyleAndWeight = FontStyle.Bold;
                vRow.Add(vL1); vRow.Add(vL2); c1.Add(vRow);
                var sRow = new VisualElement(); sRow.style.flexDirection = FlexDirection.Row; sRow.style.marginTop = 4;
                var sL1 = new Label("Status: "); sL1.style.color = cDim; sL1.style.fontSize = 10; sL1.style.width = 45;
                var sL2 = new Label(status); sL2.style.color = statColor; sL2.style.fontSize = 10;
                sRow.Add(sL1); sRow.Add(sL2); c1.Add(sRow);
                row.Add(c1);
                anList.Add(row);
            }

            foreach(var contract in country.activeContracts)
            {
                Color statColor = cYellow;
                if (contract.status == "Technical Evaluation") statColor = cCyan;
                else if (contract.status == "Contract Drafting") statColor = new Color(200/255f, 100/255f, 255/255f);
                else if (contract.status == "In Transit") statColor = cGreen;

                string icon = "\uf013"; // default gear
                if (contract.produceType == "RIFLE" || contract.produceType == "APC" || contract.produceType == "TNK") icon = "\uf51e"; // crosshairs
                else if (contract.produceType == "ARTILLERY") icon = "\uf1e2"; // bomb
                else if (contract.produceType == "FTR" || contract.produceType == "BOMBER" || contract.produceType == "HEL" || contract.produceType == "UAV") icon = "\uf0fb"; // fighter jet
                else if (contract.produceType == "PATROL_BOAT" || contract.produceType == "FRG" || contract.produceType == "SUB" || contract.produceType == "DESTROYER") icon = "\uf21a"; // ship

                AddNegItem(icon, contract.unitLabel, contract.supplier, $"₦{contract.totalCostB:F1}B", contract.status, statColor);
            }

            var sumLbl = new Label("PROCUREMENT SUMMARY");
            sumLbl.style.color = Color.white;
            sumLbl.style.fontSize = 14;
            sumLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            sumLbl.style.marginBottom = 10;
            rightCol.Add(sumLbl);

            var sumBox = new VisualElement();
            sumBox.style.backgroundColor = cPanel;
            SetBorderWidth(sumBox.style, 1);
            SetBorderColor(sumBox.style, cBorder);
            sumBox.style.borderTopLeftRadius = sumBox.style.borderTopRightRadius = 
            sumBox.style.borderBottomLeftRadius = sumBox.style.borderBottomRightRadius = 4;
            sumBox.style.paddingTop = sumBox.style.paddingBottom = 12;
            sumBox.style.paddingLeft = sumBox.style.paddingRight = 12;
            sumBox.style.marginBottom = 12;
            rightCol.Add(sumBox);

            // Placeholder for donut chart: A stacked horizontal bar chart
            var chartRow = new VisualElement(); chartRow.style.flexDirection = FlexDirection.Row; chartRow.style.height = 18; chartRow.style.marginBottom = 16;
            chartRow.style.borderTopLeftRadius = chartRow.style.borderTopRightRadius = chartRow.style.borderBottomLeftRadius = chartRow.style.borderBottomRightRadius = 6;
            chartRow.style.overflow = Overflow.Hidden;
            
            var cb1 = new VisualElement(); cb1.style.flexBasis = Length.Percent(45); cb1.style.backgroundColor = cGreen; chartRow.Add(cb1);
            var cb2 = new VisualElement(); cb2.style.flexBasis = Length.Percent(30); cb2.style.backgroundColor = cCyan; chartRow.Add(cb2);
            var cb3 = new VisualElement(); cb3.style.flexBasis = Length.Percent(15); cb3.style.backgroundColor = new Color(0, 150/255f, 255/255f); chartRow.Add(cb3);
            var cb4 = new VisualElement(); cb4.style.flexBasis = Length.Percent(10); cb4.style.backgroundColor = cOrange; chartRow.Add(cb4);
            sumBox.Add(chartRow);

            void AddLegend(string title, string pct, string val, Color col)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 10;
                var dot = new VisualElement(); dot.style.width = 12; dot.style.height = 12; dot.style.backgroundColor = col; dot.style.marginRight = 10; dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 6; row.Add(dot);
                var t = new Label(title); t.style.color = Color.white; t.style.fontSize = 12; t.style.width = 100; row.Add(t);
                var p = new Label(pct); p.style.color = cDim; p.style.fontSize = 12; p.style.width = 40; row.Add(p);
                var v = new Label(val); v.style.color = cDim; v.style.fontSize = 12; row.Add(v);
                sumBox.Add(row);
            }

            AddLegend("Army", "45%", "(₦402.7B)", cGreen);
            AddLegend("Air Force", "30%", "(₦268.8B)", cCyan);
            AddLegend("Navy", "15%", "(₦134.4B)", new Color(0, 150/255f, 255/255f));
            AddLegend("Defense Systems", "10%", "(₦90.3B)", cOrange);


        }
        private void PopulateResearch(CountryDataSO country)
        {
            if (_wPanelResearch == null) return;
            _wPanelResearch.Clear();
            _wPanelResearch.style.flexDirection = FlexDirection.Column;
            _wPanelResearch.style.flexGrow = 1;

            Color cGreen  = new Color(0f, 210/255f, 130/255f);
            Color cCyan   = new Color(0.1f, 0.5f, 0.9f);
            Color cYellow = new Color(1f, 210/255f, 0f);
            Color cOrange = new Color(1f, 130/255f, 0f);
            Color cPurple = new Color(150/255f, 80/255f, 255/255f);
            Color cDim    = new Color(1, 1, 1, 0.5f);
            Color cPanel  = new Color(0, 0, 0, 0.35f);
            Color cBorder = new Color(1, 1, 1, 0.08f);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1;
            scroll.style.paddingLeft = 12;
            scroll.style.paddingTop = 8;
            scroll.style.paddingBottom = 8;
            _wPanelResearch.Add(scroll);

            var root = scroll.contentContainer;
            root.style.paddingRight = 20;

            // ── 3 COLUMN LAYOUT ─────────────────────────────────────────────
            var columns = new VisualElement();
            columns.style.flexDirection = FlexDirection.Row;
            columns.style.flexGrow = 1;
            columns.style.width = Length.Percent(100);
            root.Add(columns);

            var leftCol = new VisualElement();
            leftCol.style.width = 200;
            leftCol.style.minWidth = 200;
            leftCol.style.flexShrink = 0;
            leftCol.style.marginRight = 16;
            columns.Add(leftCol);

            var midCol = new VisualElement();
            midCol.style.flexGrow = 1;
            midCol.style.minWidth = 0;
            midCol.style.marginRight = 16;
            columns.Add(midCol);

            var rightCol = new VisualElement();
            rightCol.style.width = 240;
            rightCol.style.minWidth = 240;
            rightCol.style.flexShrink = 0;
            columns.Add(rightCol);

            EnsureResearchData(country);
            // ================= LEFT COL =================
            var overviewHeader = new Label("RESEARCH OVERVIEW");
            overviewHeader.style.color = cGreen;
            overviewHeader.style.fontSize = 9;
            overviewHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            overviewHeader.style.marginBottom = 10;
            leftCol.Add(overviewHeader);

            void AddOverviewBox(string title, string val, string sub, string iconStr, Color subColor)
            {
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginBottom = 16;
                var icon = new Label(iconStr); icon.style.fontSize = 18; icon.style.color = cDim; icon.style.width = 24; icon.style.marginRight = 8; row.Add(icon);
                var col = new VisualElement();
                var t = new Label(title); t.style.color = cDim; t.style.fontSize = 7; col.Add(t);
                var v = new Label(val); v.style.color = Color.white; v.style.fontSize = 14; v.style.unityFontStyleAndWeight = FontStyle.Bold; col.Add(v);
                var s = new Label(sub); s.style.color = subColor; s.style.fontSize = 8; col.Add(s);
                row.Add(col);
                leftCol.Add(row);
            }

            AddOverviewBox("RESEARCH POINTS", "2,450", "+120 per month", "🔬", cGreen);
            
            var budgetRow = new VisualElement(); budgetRow.style.flexDirection = FlexDirection.Row; budgetRow.style.marginBottom = 8;
            var bIcon = new Label("⚙"); bIcon.style.fontSize = 18; bIcon.style.color = cDim; bIcon.style.width = 24; bIcon.style.marginRight = 8; budgetRow.Add(bIcon);
            var bCol = new VisualElement();
            var bt = new Label("SCIENCE BUDGET"); bt.style.color = cDim; bt.style.fontSize = 7; bCol.Add(bt);
            var bvRow = new VisualElement(); bvRow.style.flexDirection = FlexDirection.Row;
            var bv1 = new Label("₦18.3B"); bv1.style.color = Color.white; bv1.style.fontSize = 14; bv1.style.unityFontStyleAndWeight = FontStyle.Bold; bvRow.Add(bv1);
            var bv2 = new Label(" / month"); bv2.style.color = cDim; bv2.style.fontSize = 9; bv2.style.marginTop = 5; bvRow.Add(bv2);
            bCol.Add(bvRow);
            var bs = new Label("12% of Defense Budget"); bs.style.color = cDim; bs.style.fontSize = 8; bCol.Add(bs);
            budgetRow.Add(bCol);
            leftCol.Add(budgetRow);

            var adjBtn = new Button() { text = "ADJUST BUDGET" };
            adjBtn.style.backgroundColor = cPanel; adjBtn.style.color = cDim; adjBtn.style.fontSize = 8; adjBtn.style.height = 24; adjBtn.style.marginBottom = 16;
            SetBorderWidth(adjBtn.style, 1); SetBorderColor(adjBtn.style, cBorder);
            adjBtn.style.borderTopLeftRadius = adjBtn.style.borderTopRightRadius = adjBtn.style.borderBottomLeftRadius = adjBtn.style.borderBottomRightRadius = 3;
            leftCol.Add(adjBtn);

            AddOverviewBox("RESEARCH CAPACITY", "85%", "Efficiency: High", "⚛", cGreen);
            AddOverviewBox("ACTIVE SCIENTISTS", "1,250", "Across 8 Institutions", "👥", cDim);

            var queueHeader = new Label("RESEARCH QUEUE (2/3)");
            queueHeader.style.color = cGreen;
            queueHeader.style.fontSize = 9;
            queueHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            queueHeader.style.marginTop = 10;
            queueHeader.style.marginBottom = 10;
            leftCol.Add(queueHeader);

            void AddQueueItem(string iconStr, string title, string sub, string pct, string eta)
            {
                var box = new VisualElement(); box.style.flexDirection = FlexDirection.Row; box.style.backgroundColor = cPanel; box.style.marginBottom = 6; box.style.paddingTop = box.style.paddingBottom = 8; box.style.paddingLeft = box.style.paddingRight = 8;
                SetBorderWidth(box.style, 1); SetBorderColor(box.style, cBorder); box.style.borderTopLeftRadius = box.style.borderTopRightRadius = box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 4;
                var icon = new Label(iconStr); icon.style.fontSize = 18; icon.style.color = Color.white; icon.style.marginRight = 8; box.Add(icon);
                var c = new VisualElement(); c.style.flexGrow = 1;
                var t = new Label(title); t.style.color = Color.white; t.style.fontSize = 9; t.style.unityFontStyleAndWeight = FontStyle.Bold; c.Add(t);
                var s = new Label(sub); s.style.color = cDim; s.style.fontSize = 7; s.style.marginBottom = 6; c.Add(s);
                var pRow = new VisualElement(); pRow.style.flexDirection = FlexDirection.Row; pRow.style.alignItems = Align.Center; pRow.style.marginBottom = 4;
                var pBarBg = new VisualElement(); pBarBg.style.flexGrow = 1; pBarBg.style.height = 2; pBarBg.style.backgroundColor = cDim; pBarBg.style.marginRight = 6;
                var pBarFill = new VisualElement(); pBarFill.style.width = Length.Percent(float.Parse(pct)); pBarFill.style.height = 2; pBarFill.style.backgroundColor = cGreen; pBarBg.Add(pBarFill);
                pRow.Add(pBarBg);
                var pLbl = new Label(pct + "%"); pLbl.style.color = Color.white; pLbl.style.fontSize = 7; pRow.Add(pLbl);
                c.Add(pRow);
                var e = new Label("ETA: " + eta); e.style.color = cDim; e.style.fontSize = 7; c.Add(e);
                box.Add(c);
                var xBtn = new Button() { text = "✕" }; xBtn.style.width = 16; xBtn.style.height = 16; xBtn.style.fontSize = 8; xBtn.style.color = cDim; xBtn.style.backgroundColor = Color.clear; SetBorderWidth(xBtn.style, 1); SetBorderColor(xBtn.style, cBorder); box.Add(xBtn);
                leftCol.Add(box);
            }

            foreach (var project in country.researchProjects)
            {
                if (project.status == "In Progress")
                {
                    string pct = "0";
                    if (project.monthsTotal > 0)
                        pct = (100 - (100 * project.monthsRemaining / project.monthsTotal)).ToString();
                    AddQueueItem("🔬", project.projectName, project.category, pct, $"{project.monthsRemaining * 30} Days");
                }
            }

            var emptyBox = new VisualElement(); emptyBox.style.flexDirection = FlexDirection.Row; emptyBox.style.alignItems = Align.Center; emptyBox.style.backgroundColor = cPanel; emptyBox.style.marginBottom = 6; emptyBox.style.paddingTop = emptyBox.style.paddingBottom = 8; emptyBox.style.paddingLeft = emptyBox.style.paddingRight = 8;
            SetBorderWidth(emptyBox.style, 1); SetBorderColor(emptyBox.style, cBorder); emptyBox.style.borderTopLeftRadius = emptyBox.style.borderTopRightRadius = emptyBox.style.borderBottomLeftRadius = emptyBox.style.borderBottomRightRadius = 4;
            var eIcon = new Label("🔬"); eIcon.style.fontSize = 18; eIcon.style.color = cDim; eIcon.style.marginRight = 8; emptyBox.Add(eIcon);
            var ec = new VisualElement();
            var et = new Label("Empty Slot"); et.style.color = cDim; et.style.fontSize = 9; et.style.unityFontStyleAndWeight = FontStyle.Bold; ec.Add(et);
            var es = new Label("Select a research item"); es.style.color = cDim; es.style.fontSize = 7; ec.Add(es);
            emptyBox.Add(ec);
            leftCol.Add(emptyBox);

            var addBtn = new Button() { text = "ADD TO QUEUE" };
            addBtn.style.backgroundColor = cPanel; addBtn.style.color = cDim; addBtn.style.fontSize = 8; addBtn.style.height = 24; addBtn.style.marginTop = 6;
            SetBorderWidth(addBtn.style, 1); SetBorderColor(addBtn.style, cBorder); addBtn.style.borderTopLeftRadius = addBtn.style.borderTopRightRadius = addBtn.style.borderBottomLeftRadius = addBtn.style.borderBottomRightRadius = 3;
            leftCol.Add(addBtn);

            // ================= MID COL =================
            var midHeaderRow = new VisualElement(); midHeaderRow.style.flexDirection = FlexDirection.Row; midHeaderRow.style.justifyContent = Justify.SpaceBetween; midHeaderRow.style.alignItems = Align.FlexEnd; midHeaderRow.style.marginBottom = 12;
            var mLbl = new Label("AVAILABLE RESEARCH"); mLbl.style.color = cGreen; mLbl.style.fontSize = 9; mLbl.style.unityFontStyleAndWeight = FontStyle.Bold; midHeaderRow.Add(mLbl);
            midCol.Add(midHeaderRow);

            var filterRow = new VisualElement(); filterRow.style.flexDirection = FlexDirection.Row; filterRow.style.justifyContent = Justify.SpaceBetween; filterRow.style.marginBottom = 16;
            var tabsRow = new VisualElement(); tabsRow.style.flexDirection = FlexDirection.Row;
            string[] rTabs = { "All", "Army", "Air Force", "Navy", "Defense Systems", "Support" };
            foreach(var t in rTabs) {
                var tb = new Button() { text = t };
                tb.style.backgroundColor = t == "All" ? new Color(0, 210/255f, 130/255f, 0.15f) : cPanel;
                tb.style.color = t == "All" ? Color.white : cDim;
                tb.style.fontSize = 8; tb.style.height = 20; tb.style.paddingLeft = tb.style.paddingRight = 10; tb.style.marginRight = 4;
                SetBorderWidth(tb.style, 1); SetBorderColor(tb.style, t == "All" ? cGreen : cBorder);
                tb.style.borderTopLeftRadius = tb.style.borderTopRightRadius = tb.style.borderBottomLeftRadius = tb.style.borderBottomRightRadius = 3;
                tabsRow.Add(tb);
            }
            filterRow.Add(tabsRow);
            var sortBtn = new Button() { text = "Sort by: Priority ▼" }; sortBtn.style.backgroundColor = cPanel; sortBtn.style.color = cDim; sortBtn.style.fontSize = 8; sortBtn.style.height = 20; SetBorderWidth(sortBtn.style, 1); SetBorderColor(sortBtn.style, cBorder); sortBtn.style.borderTopLeftRadius = sortBtn.style.borderTopRightRadius = sortBtn.style.borderBottomLeftRadius = sortBtn.style.borderBottomRightRadius = 3;
            filterRow.Add(sortBtn);
            midCol.Add(filterRow);

            var grid = new VisualElement(); grid.style.flexDirection = FlexDirection.Row; grid.style.flexWrap = Wrap.Wrap; grid.style.justifyContent = Justify.SpaceBetween;
            midCol.Add(grid);

            void AddResCard(string icon, string title, string sub, string level, string desc, string[] benefits, string days, string cost, Color cColor, bool isStarred, string imageName = null)
            {
                var c = new VisualElement(); c.style.width = Length.Percent(48.5f); c.style.backgroundColor = cPanel; c.style.marginBottom = 12; c.style.paddingTop = c.style.paddingBottom = c.style.paddingLeft = c.style.paddingRight = 10;
                SetBorderWidth(c.style, 1); SetBorderColor(c.style, cBorder); c.style.borderTopLeftRadius = c.style.borderTopRightRadius = c.style.borderBottomLeftRadius = c.style.borderBottomRightRadius = 4;
                
                var top = new VisualElement(); top.style.flexDirection = FlexDirection.Row; top.style.justifyContent = Justify.SpaceBetween; top.style.marginBottom = 8;
                var tl = new VisualElement(); tl.style.flexDirection = FlexDirection.Row;
                var icn = new Label(icon); icn.style.color = cColor; icn.style.fontSize = 16; icn.style.marginRight = 6; tl.Add(icn);
                var tt = new VisualElement();
                var tLbl = new Label(title); tLbl.style.color = Color.white; tLbl.style.fontSize = 9; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold; tt.Add(tLbl);
                var sLbl = new Label(sub); sLbl.style.color = cDim; sLbl.style.fontSize = 7; tt.Add(sLbl);
                tl.Add(tt); top.Add(tl);
                var star = new Label(isStarred ? "★" : "☆"); star.style.color = isStarred ? cYellow : cDim; star.style.fontSize = 12; top.Add(star);
                c.Add(top);


                var thumb = new VisualElement(); thumb.style.height = 70; thumb.style.backgroundColor = new Color(0,0,0,0.5f); thumb.style.marginBottom = 8; thumb.style.borderTopLeftRadius = thumb.style.borderTopRightRadius = thumb.style.borderBottomLeftRadius = thumb.style.borderBottomRightRadius = 4;
                if (!string.IsNullOrEmpty(imageName))
                {
                    var tex = UnityEngine.Resources.Load<UnityEngine.Texture2D>("EquipmentPhotos/" + imageName);
                    if (tex != null)
                    {
                        thumb.style.backgroundImage = new UnityEngine.UIElements.StyleBackground(tex);
#pragma warning disable CS0618
                        thumb.style.unityBackgroundScaleMode = UnityEngine.ScaleMode.ScaleToFit;
#pragma warning restore CS0618
                        thumb.style.backgroundColor = UnityEngine.Color.clear;
                    }
                }
                c.Add(thumb);

                var lvl = new Label(level); lvl.style.color = Color.white; lvl.style.fontSize = 8; lvl.style.unityFontStyleAndWeight = FontStyle.Bold; lvl.style.marginBottom = 4; c.Add(lvl);
                var dsc = new Label(desc); dsc.style.color = cDim; dsc.style.fontSize = 7; dsc.style.whiteSpace = WhiteSpace.Normal; dsc.style.marginBottom = 8; c.Add(dsc);
                
                var bLbl = new Label("Benefits"); bLbl.style.color = cGreen; bLbl.style.fontSize = 7; bLbl.style.marginBottom = 4; c.Add(bLbl);
                foreach(var b in benefits) {
                    var br = new VisualElement(); br.style.flexDirection = FlexDirection.Row; br.style.marginBottom = 2;
                    var bd = new Label("• "); bd.style.color = cGreen; bd.style.fontSize = 7; br.Add(bd);
                    var bt = new Label(b); bt.style.color = Color.white; bt.style.fontSize = 7; br.Add(bt);
                    c.Add(br);
                }

                var bot = new VisualElement(); bot.style.flexDirection = FlexDirection.Row; bot.style.justifyContent = Justify.SpaceBetween; bot.style.marginTop = 12; bot.style.paddingTop = 8;
                bot.style.borderTopWidth = 1; bot.style.borderTopColor = cBorder;
                var dRow = new VisualElement(); dRow.style.flexDirection = FlexDirection.Row;
                var dIc = new Label("🕒"); dIc.style.color = cDim; dIc.style.fontSize = 10; dIc.style.marginRight = 4; dRow.Add(dIc);
                var dTx = new Label(days); dTx.style.color = cDim; dTx.style.fontSize = 8; dRow.Add(dTx);
                bot.Add(dRow);
                var costBox = new VisualElement(); costBox.style.flexDirection = FlexDirection.Row; costBox.style.paddingLeft = costBox.style.paddingRight = 6; costBox.style.paddingTop = costBox.style.paddingBottom = 2; costBox.style.backgroundColor = new Color(0, 210/255f, 130/255f, 0.1f); costBox.style.borderTopLeftRadius = costBox.style.borderTopRightRadius = costBox.style.borderBottomLeftRadius = costBox.style.borderBottomRightRadius = 2; SetBorderWidth(costBox.style, 1); SetBorderColor(costBox.style, new Color(0, 210/255f, 130/255f, 0.3f));
                var cIc = new Label("🔬"); cIc.style.color = cGreen; cIc.style.fontSize = 8; cIc.style.marginRight = 4; costBox.Add(cIc);
                var cTx = new Label(cost); cTx.style.color = cGreen; cTx.style.fontSize = 8; cTx.style.unityFontStyleAndWeight = FontStyle.Bold; costBox.Add(cTx);
                bot.Add(costBox);
                c.Add(bot);

                grid.Add(c);
            }

            foreach (var project in country.researchProjects)
            {
                if (project.status == "Available" || project.status == "In Progress")
                {
                    Color cc = cGreen;
                    if (project.category.Contains("Tech")) cc = cCyan;
                    if (project.category.Contains("Defense")) cc = cOrange;
                    AddResCard("🔬", project.projectName, project.category, "Level 1 / 5", "Research to unlock new capabilities.", new string[] { "+10% Efficiency" }, $"{project.monthsTotal * 30} days", "1,500", cc, false);
                }
            }

            var viewBtn = new Button() { text = "🔒 VIEW LOCKED RESEARCH" };
            viewBtn.style.backgroundColor = cPanel; viewBtn.style.color = cDim; viewBtn.style.fontSize = 8; viewBtn.style.height = 30; viewBtn.style.marginTop = 10;
            SetBorderWidth(viewBtn.style, 1); SetBorderColor(viewBtn.style, cBorder); viewBtn.style.borderTopLeftRadius = viewBtn.style.borderTopRightRadius = viewBtn.style.borderBottomLeftRadius = viewBtn.style.borderBottomRightRadius = 3;
            midCol.Add(viewBtn);

            // ================= RIGHT COL =================
            var statHeader = new Label("RESEARCH STATS");
            statHeader.style.color = cGreen; statHeader.style.fontSize = 9; statHeader.style.unityFontStyleAndWeight = FontStyle.Bold; statHeader.style.marginBottom = 10;
            rightCol.Add(statHeader);

            void AddStatRow(string icon, string title, string val, Color iColor)
            {
                var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.justifyContent = Justify.SpaceBetween; r.style.marginBottom = 6;
                var left = new VisualElement(); left.style.flexDirection = FlexDirection.Row;
                var ic = new Label(icon); ic.style.color = iColor; ic.style.fontSize = 10; ic.style.width = 16; left.Add(ic);
                var t = new Label(title); t.style.color = cDim; t.style.fontSize = 8; left.Add(t);
                r.Add(left);
                var v = new Label(val); v.style.color = Color.white; v.style.fontSize = 8; v.style.unityFontStyleAndWeight = FontStyle.Bold; r.Add(v);
                rightCol.Add(r);
            }

            int countTotal = country.researchProjects.Count;
            int countCompleted = 0;
            int countInProgress = 0;
            int countNotStarted = 0;

            foreach (var r in country.researchProjects)
            {
                if (r.status == "Completed") countCompleted++;
                else if (r.status == "In Progress") countInProgress++;
                else countNotStarted++;
            }

            AddStatRow("🔬", "Total Technologies", countTotal.ToString(), cDim);
            AddStatRow("✓", "Completed", countCompleted.ToString(), cGreen);
            AddStatRow("↻", "In Progress", countInProgress.ToString(), cCyan);
            AddStatRow("◯", "Not Started", countNotStarted.ToString(), cDim);
            
            var div = new VisualElement(); div.style.height = 1; div.style.backgroundColor = cBorder; div.style.marginTop = 6; div.style.marginBottom = 8; rightCol.Add(div);

            AddStatRow("🕒", "Average Research Time", "132 Days", cDim);
            AddStatRow("⚙", "Technologies Researched", "56", cDim);
            AddStatRow("⚡", "Breakthroughs", "3", cDim);

            var treeHeaderRow = new VisualElement(); treeHeaderRow.style.flexDirection = FlexDirection.Row; treeHeaderRow.style.justifyContent = Justify.SpaceBetween; treeHeaderRow.style.marginTop = 20; treeHeaderRow.style.marginBottom = 10;
            var tLbl = new Label("TECHNOLOGY TREE"); tLbl.style.color = cGreen; tLbl.style.fontSize = 9; tLbl.style.unityFontStyleAndWeight = FontStyle.Bold; treeHeaderRow.Add(tLbl);
            var vLbl = new Label("View Full Tree"); vLbl.style.color = cCyan; vLbl.style.fontSize = 7; treeHeaderRow.Add(vLbl);
            rightCol.Add(treeHeaderRow);

            void AddTreeProgress(string icon, string title, Color col, int filled, int total)
            {
                var r = new VisualElement(); r.style.flexDirection = FlexDirection.Row; r.style.alignItems = Align.Center; r.style.marginBottom = 10;
                var ic = new Label(icon); ic.style.color = col; ic.style.fontSize = 14; ic.style.width = 20; r.Add(ic);
                var c = new VisualElement(); c.style.flexGrow = 1;
                var t = new Label(title); t.style.color = cDim; t.style.fontSize = 7; t.style.marginBottom = 4; c.Add(t);
                var dots = new VisualElement(); dots.style.flexDirection = FlexDirection.Row; dots.style.alignItems = Align.Center;
                for(int i=0; i<total; i++) {
                    var dot = new VisualElement(); dot.style.width = 6; dot.style.height = 6; dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 3;
                    dot.style.backgroundColor = (i < filled) ? col : Color.clear;
                    SetBorderWidth(dot.style, 1); SetBorderColor(dot.style, (i < filled) ? col : cDim);
                    dots.Add(dot);
                    if (i < total - 1) {
                        var line = new VisualElement(); line.style.height = 1; line.style.width = 12; line.style.backgroundColor = (i < filled - 1) ? col : cDim; dots.Add(line);
                    }
                }
                c.Add(dots);
                r.Add(c);
                rightCol.Add(r);
            }

            AddTreeProgress("🪖", "Infantry Warfare", cGreen, 3, 5);
            AddTreeProgress("🛡", "Armor Technology", cCyan, 2, 5);
            AddTreeProgress("✈", "Aviation Technology", cCyan, 1, 5);
            AddTreeProgress("⚓", "Naval Technology", cCyan, 2, 5);
            AddTreeProgress("🚀", "Missile Technology", cPurple, 1, 5);
            AddTreeProgress("💻", "Cyber Warfare", cPurple, 2, 5);

            var recHeader = new Label("RECENTLY COMPLETED"); recHeader.style.color = cGreen; recHeader.style.fontSize = 9; recHeader.style.unityFontStyleAndWeight = FontStyle.Bold; recHeader.style.marginTop = 16; recHeader.style.marginBottom = 10; rightCol.Add(recHeader);

            foreach (var project in country.researchProjects)
            {
                if (project.status == "Completed")
                {
                    var recBox = new VisualElement(); recBox.style.flexDirection = FlexDirection.Row; recBox.style.alignItems = Align.Center; recBox.style.backgroundColor = cPanel; recBox.style.paddingTop = recBox.style.paddingBottom = 8; recBox.style.paddingLeft = recBox.style.paddingRight = 10; SetBorderWidth(recBox.style, 1); SetBorderColor(recBox.style, cBorder); recBox.style.borderTopLeftRadius = recBox.style.borderTopRightRadius = recBox.style.borderBottomLeftRadius = recBox.style.borderBottomRightRadius = 4;
                    recBox.style.marginBottom = 6;
                    var rIc = new Label("🎯"); rIc.style.color = Color.white; rIc.style.fontSize = 18; rIc.style.marginRight = 10; recBox.Add(rIc);
                    var rC = new VisualElement(); rC.style.flexGrow = 1;
                    var rT = new Label(project.projectName); rT.style.color = Color.white; rT.style.fontSize = 8; rT.style.unityFontStyleAndWeight = FontStyle.Bold; rC.Add(rT);
                    var rS = new Label(project.category); rS.style.color = cDim; rS.style.fontSize = 7; rC.Add(rS);
                    var rD = new Label("Completed on " + project.completedDate); rD.style.color = cGreen; rD.style.fontSize = 6; rD.style.marginTop = 4; rC.Add(rD);
                    recBox.Add(rC);
                    var rBtn = new Button() { text = "+" }; rBtn.style.width = 20; rBtn.style.height = 20; rBtn.style.backgroundColor = new Color(0, 210/255f, 130/255f, 0.2f); rBtn.style.color = cGreen; SetBorderWidth(rBtn.style, 1); SetBorderColor(rBtn.style, cGreen); rBtn.style.borderTopLeftRadius = rBtn.style.borderTopRightRadius = rBtn.style.borderBottomLeftRadius = rBtn.style.borderBottomRightRadius = 10; recBox.Add(rBtn);
                    rightCol.Add(recBox);
                }
            }
        }
    }
}