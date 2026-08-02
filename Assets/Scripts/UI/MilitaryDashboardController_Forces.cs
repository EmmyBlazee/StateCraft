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
        private void PopulateGenerals(CountryDataSO country)
        {
            if (_generalsContainer == null) return;
            _generalsContainer.Clear();

            if (country.assignedGenerals == null || country.assignedGenerals.Count == 0)
            {
                _generalsContainer.Add(new Label("No Active Generals in Chain of Command.") { style = { color = Color.grey, fontSize = 10, unityFontStyleAndWeight = FontStyle.Italic } });
                return;
            }

            foreach (var gen in country.assignedGenerals)
            {
                var row = new VisualElement() { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 6, paddingBottom = 4, borderBottomWidth = 1, borderBottomColor = new Color(1,1,1,0.1f) } };
                string stars = new string('★', gen.rank);
                
                var topRow = new VisualElement();
                topRow.Add(new Label($"{stars} Gen. {gen.generalName}") { style = { color = Color.white, fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold } });
                topRow.Add(new Label($"Specialty: {gen.specialty}") { style = { color = new Color(0.9f, 0.6f, 0f), fontSize = 9 } });

                var botRow = new VisualElement() { style = { alignItems = Align.FlexEnd } };
                botRow.Add(new Label($"Bonus: +{gen.effectivenessBonus:P0} DMG | +{gen.moraleBonus:P0} Morale") { style = { color = new Color(0.14f, 0.93f, 0.34f), fontSize = 9 } });
                string status = gen.isDeployed ? $"Deployed to {gen.assignedOperation}" : "In Reserve";
                botRow.Add(new Label(status) { style = { color = gen.isDeployed ? Color.white : Color.grey, fontSize = 9, unityFontStyleAndWeight = FontStyle.Italic } });

                row.Add(topRow);
                row.Add(botRow);
                _generalsContainer.Add(row);
            }
        }

        private void PopulateGlobalPower(CountryDataSO country)
        {
            if (_globalPowerList == null || GameManager.Instance == null) return;
            _globalPowerList.Clear();

            var gm = GameManager.Instance;
            if (gm.allCountries == null || gm.allCountries.Count == 0) return;

            // Sort all countries by their military firepower score
            var sortedCountries = new List<CountryDataSO>(gm.allCountries);
            sortedCountries.Sort((a, b) => b.GetMilitaryFirepowerScore().CompareTo(a.GetMilitaryFirepowerScore()));

            float maxScore = sortedCountries.Count > 0 ? sortedCountries[0].GetMilitaryFirepowerScore() : 1f;
            if (maxScore <= 0f) maxScore = 1f;

            for (int i = 0; i < sortedCountries.Count; i++)
            {
                var c = sortedCountries[i];
                float score = c.GetMilitaryFirepowerScore();

                var row = new VisualElement();
                row.AddToClassList("sub-panel");
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.marginBottom = 6;
                row.style.paddingTop = 8;
                row.style.paddingBottom = 8;
                row.style.paddingLeft = 15;
                row.style.paddingRight = 15;

                // Left element: Rank & Country Name
                var leftGroup = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var rankLabel = new Label($"#{i + 1}") 
                { 
                    style = { 
                        color = i == 0 ? new Color(255/255f, 215/255f, 0) : Color.grey, 
                        unityFontStyleAndWeight = FontStyle.Bold, 
                        fontSize = 11, 
                        marginRight = 15,
                        width = 25
                    } 
                };
                leftGroup.Add(rankLabel);

                var nameLabel = new Label(c.countryName.ToUpper()) 
                { 
                    style = { 
                        color = c == country ? new Color(0.1f, 0.5f, 0.9f) : Color.white, 
                        unityFontStyleAndWeight = c == country ? FontStyle.Bold : FontStyle.Normal,
                        fontSize = 10,
                        width = 120
                    } 
                };
                leftGroup.Add(nameLabel);
                row.Add(leftGroup);

                // Middle: Visual comparison bar
                var barContainer = new VisualElement() { style = { flexGrow = 1, height = 8, backgroundColor = new Color(0, 0, 0, 0.4f), marginLeft = 20, marginRight = 20 } };
                barContainer.style.borderTopLeftRadius = 4;
                barContainer.style.borderTopRightRadius = 4;
                barContainer.style.borderBottomLeftRadius = 4;
                barContainer.style.borderBottomRightRadius = 4;

                float percent = (score / maxScore) * 100f;
                var fillBar = new VisualElement() 
                { 
                    style = { 
                        width = Length.Percent(percent), 
                        height = Length.Percent(100), 
                        backgroundColor = c == country ? new Color(0.1f, 0.5f, 0.9f) : new Color(255/255f, 82/255f, 82/255f)
                    } 
                };
                fillBar.style.borderTopLeftRadius = 4;
                fillBar.style.borderTopRightRadius = 4;
                fillBar.style.borderBottomLeftRadius = 4;
                fillBar.style.borderBottomRightRadius = 4;

                barContainer.Add(fillBar);
                row.Add(barContainer);

                // Right element: Firepower Index & Threat Tag
                var rightGroup = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                
                var scoreLabel = new Label($"{score:N0} MPI") 
                { 
                    style = { 
                        color = Color.white, 
                        unityFontStyleAndWeight = FontStyle.Bold, 
                        fontSize = 10,
                        marginRight = 15,
                        width = 80,
                        unityTextAlign = TextAnchor.MiddleRight
                    } 
                };
                rightGroup.Add(scoreLabel);

                var badge = new Label();
                badge.style.fontSize = 8;
                badge.style.unityFontStyleAndWeight = FontStyle.Bold;
                badge.style.paddingTop = 2;
                badge.style.paddingBottom = 2;
                badge.style.paddingLeft = 6;
                badge.style.paddingRight = 6;
                badge.style.borderTopLeftRadius = 3;
                badge.style.borderTopRightRadius = 3;
                badge.style.borderBottomLeftRadius = 3;
                badge.style.borderBottomRightRadius = 3;
                badge.style.width = 110;
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;

                if (c == country)
                {
                    badge.text = "YOUR NATION";
                    badge.style.color = new Color(0.1f, 0.5f, 0.9f);
                    badge.style.backgroundColor = new Color(0, 210/255f, 255/255f, 0.1f);
                }
                else if (score > country.GetMilitaryFirepowerScore())
                {
                    badge.text = "SUPERIOR FORCE";
                    badge.style.color = new Color(255/255f, 82/255f, 82/255f);
                    badge.style.backgroundColor = new Color(255/255f, 82/255f, 82/255f, 0.1f);
                }
                else
                {
                    badge.text = "MATCHED / INFERIOR";
                    badge.style.color = Color.grey;
                    badge.style.backgroundColor = new Color(128/255f, 128/255f, 128/255f, 0.1f);
                }
                rightGroup.Add(badge);
                row.Add(rightGroup);

                _globalPowerList.Add(row);
            }
        }

        private void InjectForcesImages(VisualElement root)
        {
            var branchImgArmy = root.Q<VisualElement>("BranchImgArmy");
            if (branchImgArmy != null) branchImgArmy.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/army (2)"));

            var branchImgNavy = root.Q<VisualElement>("BranchImgNavy");
            if (branchImgNavy != null) branchImgNavy.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/navy"));

            var branchImgAF = root.Q<VisualElement>("BranchImgAF");
            if (branchImgAF != null) branchImgAF.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/airforce"));

            var branchImgRDF = root.Q<VisualElement>("BranchImgRDF");
            if (branchImgRDF != null) branchImgRDF.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/rdf"));

            // Inject the same images into the Recruitment academy cards
            var recruitImgArmy = root.Q<VisualElement>("RecruitImgArmy");
            if (recruitImgArmy != null) recruitImgArmy.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/army (2)"));

            var recruitImgNavy = root.Q<VisualElement>("RecruitImgNavy");
            if (recruitImgNavy != null) recruitImgNavy.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/navy"));

            var recruitImgAF = root.Q<VisualElement>("RecruitImgAF");
            if (recruitImgAF != null) recruitImgAF.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/airforce"));

            var recruitImgRDF = root.Q<VisualElement>("RecruitImgRDF");
            if (recruitImgRDF != null) recruitImgRDF.style.backgroundImage = new StyleBackground(Resources.Load<Texture2D>("BranchImages/rdf"));
        }

        private void PopulateBranches(CountryDataSO country)
        {
            if (_root == null || country == null) return;
            
            Debug.Log($"[Military Dashboard] FORCES Tab wired! Loaded {country.countryName} data: Army={country.armyStats.personnel:N0}, Navy={country.navyStats.personnel:N0}, AF={country.airForceStats.personnel:N0}");

            UpdateBranchUI("Army", country.armyStats);
            UpdateBranchUI("Navy", country.navyStats);
            UpdateBranchUI("AF", country.airForceStats);
            UpdateBranchUI("RDF", country.rdfStats);

            // -- ROW 1: FORCE READINESS OVERVIEW --
            if (_root.Q<ProgressBar>("ReadinessArmyBar") is ProgressBar rArmy) rArmy.value = country.armyStats.readiness;
            if (_root.Q<Label>("ReadinessArmyPct") is Label lArmy) lArmy.text = $"{country.armyStats.readiness:F0}%";

            if (_root.Q<ProgressBar>("ReadinessNavyBar") is ProgressBar rNavy) rNavy.value = country.navyStats.readiness;
            if (_root.Q<Label>("ReadinessNavyPct") is Label lNavy) lNavy.text = $"{country.navyStats.readiness:F0}%";

            if (_root.Q<ProgressBar>("ReadinessAFBar") is ProgressBar rAF) rAF.value = country.airForceStats.readiness;
            if (_root.Q<Label>("ReadinessAFPct") is Label lAF) lAF.text = $"{country.airForceStats.readiness:F0}%";

            if (_root.Q<ProgressBar>("ReadinessRDFBar") is ProgressBar rRDF) rRDF.value = country.rdfStats.readiness;
            if (_root.Q<Label>("ReadinessRDFPct") is Label lRDF) lRDF.text = $"{country.rdfStats.readiness:F0}%";

            // -- ROW 1: MILITARY SUMMARY --
            float totalPersonnel = country.armyStats.personnel + country.navyStats.personnel + country.airForceStats.personnel + country.rdfStats.personnel;
            float totalEquipment = country.armyStats.equipment + country.navyStats.equipment + country.airForceStats.equipment + country.rdfStats.equipment;
            
            string sym = string.IsNullOrEmpty(country.currencySymbol) ? "₦" : country.currencySymbol;
            float annualBudget = country.militaryBudgetUSD * country.exchangeRate * 12f;
            float defenceGDP = (country.militaryBudgetUSD * 12f) / Mathf.Max(1f, country.gdp) * 100f;

            if (_root.Q<Label>("SumPersonnel") is Label sumPers) sumPers.text = totalPersonnel.ToString("N0");
            if (_root.Q<Label>("SumEquipment") is Label sumEq) sumEq.text = totalEquipment.ToString("N0");
            if (_root.Q<Label>("SumAnnualBudget") is Label sumBud) sumBud.text = UiFormat.Money(annualBudget, sym);
            if (_root.Q<Label>("SumDefenceGDP") is Label sumGDP) sumGDP.text = $"{defenceGDP:F1}% of GDP";
            if (_root.Q<Label>("SumVeterans") is Label sumVet) sumVet.text = country.totalWounded.ToString("N0"); // Simple approximation for now

            PopulateBottomSection(country);
        }

        private void PopulateBottomSection(CountryDataSO country)
        {
            if (_root == null || country == null) return;

            // 1. DEFENCE READINESS FACTORS
            // Approximating from available stats
            float avgReadiness = (country.armyStats.readiness + country.navyStats.readiness + country.airForceStats.readiness + country.rdfStats.readiness) / 4f;
            
            UpdateFactor("FactorEquip", avgReadiness + 5f);
            UpdateFactor("FactorTrain", country.militaryReadiness);
            UpdateFactor("FactorIntel", country.intelligenceLevel * 10f);
            UpdateFactor("FactorLog", Mathf.Clamp(avgReadiness - 15f, 0, 100));
            UpdateFactor("FactorWelf", country.publicApproval);

            // 2. RECENT OPERATIONS
            var opsList = _root.Q<VisualElement>("RecentOpsList");
            if (opsList != null)
            {
                var activeOps = country.activeOperations ?? new List<MilitaryOperation>();
                
                Debug.Log($"[Military Dashboard] FORCES Tab -> PopulateBottomSection -> ActiveOps Count: {activeOps.Count}");

                // Get ALL direct children of RecentOpsList — one per op row
                var rows = opsList.Children().ToList();

                for (int i = 0; i < 3; i++)
                {
                    // The row container is the i-th child of RecentOpsList
                    VisualElement row = i < rows.Count ? rows[i] : null;

                    var opName   = _root.Q<Label>($"RecentOp{i + 1}Name");
                    var opDesc   = _root.Q<Label>($"RecentOp{i + 1}Desc");
                    var opDate   = _root.Q<Label>($"RecentOp{i + 1}Date");
                    var opStatus = _root.Q<Label>($"RecentOp{i + 1}Status");

                    if (opName == null) continue;

                    if (i < activeOps.Count)
                    {
                        var op = activeOps[i];
                        opName.text = op.opName;
                        if (opDesc   != null) opDesc.text   = $"{op.type} · {op.regionName}";
                        if (opDate   != null) opDate.text   = $"Turn {op.turnsActive}";
                        if (opStatus != null) { opStatus.text = "ONGOING"; opStatus.style.color = new Color(0f, 0.9f, 0.46f); }
                        if (row != null) row.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        // Blank AND hide the entire row so UXML placeholders don't show
                        opName.text = "";
                        if (opDesc   != null) opDesc.text   = "";
                        if (opDate   != null) opDate.text   = "";
                        if (opStatus != null) opStatus.text = "";
                        if (row != null) row.style.display = DisplayStyle.None;
                    }
                }

                // If no ops at all, show a small "No active operations" message
                if (activeOps.Count == 0)
                {
                    var noOpsMsg = opsList.Q<Label>("NoOpsMessage");
                    if (noOpsMsg == null)
                    {
                        noOpsMsg = new Label("No active operations — launch one from the OPERATIONS tab.");
                        noOpsMsg.name = "NoOpsMessage";
                        noOpsMsg.style.color = new Color(0.42f, 0.50f, 0.60f);
                        noOpsMsg.style.fontSize = 10;
                        noOpsMsg.style.marginTop = 6;
                        noOpsMsg.style.unityTextAlign = TextAnchor.MiddleCenter;
                        opsList.Add(noOpsMsg);
                    }
                    noOpsMsg.style.display = DisplayStyle.Flex;
                }
                else
                {
                    var noOpsMsg = opsList.Q<Label>("NoOpsMessage");
                    if (noOpsMsg != null) noOpsMsg.style.display = DisplayStyle.None;
                }
            }

            // Calculate personnel in training
            int personnelInTraining = 0;
            if (country.trainingPipelines != null)
            {
                foreach (var p in country.trainingPipelines)
                {
                    if (p.isPersonnel) personnelInTraining += p.amount;
                }
            }

            // 3. FORCE STATUS
            if (_root.Q<Label>("ForceStatusDeployments") is Label lDeploy) 
                lDeploy.text = (country.activeOperations != null) ? country.activeOperations.Count.ToString() : "0";
                
            if (_root.Q<Label>("ForceStatusTraining") is Label lTrain) 
                lTrain.text = personnelInTraining.ToString("N0");
                
            if (_root.Q<Label>("ForceStatusMaintenance") is Label lMaint) 
                lMaint.text = $"{Mathf.FloorToInt(avgReadiness)}%";
                
            if (_root.Q<Label>("ForceStatusMedEvac") is Label lMed) 
                lMed.text = $"{Mathf.Min(100f, country.militaryReadiness + 25f):F0}%";
                
            if (_root.Q<Label>("ForceStatusMobTime") is Label lMob) 
            {
                lMob.text = "48 Hours";
            }
        }

        private void UpdateFactor(string prefix, float value)
        {
            value = Mathf.Clamp(value, 0f, 100f);
            var bar = _root.Q<ProgressBar>($"{prefix}Bar");
            if (bar != null) bar.value = value;
            
            var label = _root.Q<Label>($"{prefix}Label");
            if (label != null)
            {
                if (value >= 80f) { label.text = "GOOD"; label.style.color = new Color(0, 230/255f, 118/255f); }
                else if (value >= 50f) { label.text = "MEDIUM"; label.style.color = new Color(255/255f, 180/255f, 0); }
                else { label.text = "POOR"; label.style.color = new Color(255/255f, 82/255f, 82/255f); }
            }
        }

        private void UpdateBranchUI(string prefix, BranchStats stats)
        {
            var personnelLbl = _root.Q<Label>($"{prefix}Personnel");
            Debug.Log($"[UpdateBranchUI] Prefix={prefix}, Found PersonnelLabel? {personnelLbl != null}. Setting to {stats.personnel}");
            if (personnelLbl != null) { personnelLbl.text = stats.personnel.ToString("N0"); }

            var equipmentLbl = _root.Q<Label>($"{prefix}Equipment");
            if (equipmentLbl != null) equipmentLbl.text = stats.equipment.ToString("N0");

            var readinessBar = _root.Q<ProgressBar>($"{prefix}ReadinessBar");
            if (readinessBar != null) readinessBar.value = stats.readiness;

            var readinessPct = _root.Q<Label>($"{prefix}ReadinessPct");
            if (readinessPct != null) readinessPct.text = $"{stats.readiness:F0}%";

            var moraleBar = _root.Q<ProgressBar>($"{prefix}MoraleBar");
            if (moraleBar != null) moraleBar.value = stats.morale;

            var moralePct = _root.Q<Label>($"{prefix}MoralePct");
            if (moralePct != null) moralePct.text = $"{stats.morale:F0}%";
            
            // Commanders could be updated here if stats contained commander info
        }
        private void PopulateWMD(CountryDataSO country)
        {
            if (_wmdProgressBar == null || _wmdStatusLabel == null || _wmdOperationalBadge == null) return;

            _wmdProgressBar.value = country.wmdDevelopmentProgress; // 0-100

            if (country.hasNuclearWeapon)
            {
                _wmdProgressBar.style.display = DisplayStyle.None;
                _wmdStatusLabel.style.display = DisplayStyle.None;
                _wmdOperationalBadge.style.display = DisplayStyle.Flex;
                _wmdOperationalBadge.text = "NUCLEAR CAPABILITY SECURED (MAXIMUM DETERRENCE ONSITE)";
            }
            else
            {
                _wmdProgressBar.style.display = DisplayStyle.Flex;
                _wmdStatusLabel.style.display = DisplayStyle.Flex;
                _wmdOperationalBadge.style.display = DisplayStyle.None;

                if (country.wmdDevelopmentProgress == 0)
                {
                    _wmdStatusLabel.text = "Program Offline — Requires Funding Directive";
                    _wmdStatusLabel.style.color = new Color(0.9f, 0.6f, 0f);
                }
                else
                {
                    _wmdStatusLabel.text = country.wmdProgramDiscovered 
                        ? $"Active Enrichment (Risk Critical) - {country.wmdDevelopmentProgress}%" 
                        : $"Covert Enrichment Active - {country.wmdDevelopmentProgress}%";
                    
                    _wmdStatusLabel.style.color = country.wmdProgramDiscovered 
                        ? new Color(255/255f, 82/255f, 82/255f) 
                        : new Color(0.14f, 0.93f, 0.34f);
                }
            }
        }

        private static string BuildMilBriefing(CountryDataSO c, GameManager gm, WarStage stage)
        {
            if (c == null) return "No military briefing available.";
            int regionCount = c.regions != null ? c.regions.Count : 0;
            if (c.unitRifles < c.armyStats.personnel)
                return $"CRITICAL WEAPONS SHORTAGE: Insufficient service rifles ({c.unitRifles:N0}) to fully equip active army ({c.armyStats.personnel:N0}). Tactical operations running at 50% efficiency! Procure rifles under the WEAPONS tab.";
            if (stage == WarStage.Conflict)
                return $"Nation at war. Readiness {c.militaryReadiness:F0}% — prioritize logistics and COIN taskings.";
            if (c.reserveAmmo < 50f || c.reserveFuel < 50f)
                return "Supply lines strained. Convoy escorts and reserve replenishment recommended.";
            if (c.regions != null && c.regions.Exists(r => r != null && r.security < 40f))
                return "Internal security deteriorating in at least one theatre. Consider COIN posture.";
            return $"Peacetime posture holding. Joint readiness {c.militaryReadiness:F0}% across {regionCount} regions.";
        }

        private void PopulatePolice(CountryDataSO country)
        {
            if (_regionPoliceList == null || country == null || country.regions == null) return;
            _regionPoliceList.Clear();

            int validCount = country.regions.Count(r => r != null);
            foreach (var region in country.regions)
            {
                if (region == null) continue;
                var item = new VisualElement();
                item.AddToClassList("sub-panel");
                item.style.paddingTop = 10;
                item.style.paddingBottom = 10;
                item.style.paddingLeft = 12;
                item.style.paddingRight = 12;
                item.style.width = Length.Percent(32);
                item.style.marginRight = 5;
                item.style.marginBottom = 10;

                int localPolice = validCount > 0 ? Mathf.RoundToInt(country.nationalPoliceSize / (float)validCount) : 0;
                
                item.Add(new Label(region.regionName.ToUpper()) { style = { color = new Color(255/255f, 235/255f, 59/255f), fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold } });
                item.Add(new Label($"Local Police: {localPolice:N0} Officers") { style = { color = Color.white, fontSize = 9, marginTop = 4 } });
                item.Add(new Label($"Security: {region.security:F0}%") { style = { color = Color.grey, fontSize = 8, marginTop = 2 } });
                _regionPoliceList.Add(item);
            }
        }



        private void UpdateIntelAssessment(CountryDataSO country)
        {
            if (country == null || country.regions == null || country.regions.Count == 0) return;
            var validRegions = country.regions.Where(r => r != null).ToList();
            if (validRegions.Count == 0) return;
            var r = validRegions[0];
            foreach (var reg in validRegions)
            {
                if (reg.security < r.security) r = reg;
            }

            var root = _root;
            if (root == null) return;

            string threatLevel = r.security < 50f ? "HIGH" : (r.security < 75f ? "MEDIUM" : "LOW");
            int minEnemies = Mathf.Max(500, (int)((100f - r.security) * 80));
            int maxEnemies = (int)(minEnemies * 1.4f);

            var lblRegion = root.Q<Label>("lbl-intel-region");
            if (lblRegion != null) lblRegion.text = r.regionName.ToUpper();
            
            var lblThreat = root.Q<Label>("lbl-threat-level");
            if (lblThreat != null) lblThreat.text = threatLevel;
            
            var lblStr = root.Q<Label>("lbl-enemy-strength");
            if (lblStr != null) lblStr.text = $"{minEnemies:N0} - {maxEnemies:N0}";
            
            var lblConf = root.Q<Label>("lbl-confidence");
            if (lblConf != null) lblConf.text = $"{r.stability:F0}%";
            
            var lblSrc = root.Q<Label>("lbl-intel-source");
            if (lblSrc != null) lblSrc.text = "Signals Intelligence";
            
            var lblUpd = root.Q<Label>("lbl-last-updated");
            if (lblUpd != null) lblUpd.text = "Live";
        }

        private void PopulateReadinessList(CountryDataSO country)
        {
            if (_readinessList == null) return;
            _readinessList.Clear();

            var branches = new List<System.Tuple<string, string, int>>()
            {
                new System.Tuple<string, string, int>("ARMY",      "A", Mathf.Clamp((int)country.armyStats.readiness,    0, 100)),
                new System.Tuple<string, string, int>("RDF",       "R", Mathf.Clamp((int)country.rdfStats.readiness,     0, 100)),
                new System.Tuple<string, string, int>("AIR FORCE", "F", Mathf.Clamp((int)country.airForceStats.readiness,0, 100)),
                new System.Tuple<string, string, int>("NAVY",      "N", Mathf.Clamp((int)country.navyStats.readiness,    0, 100))
            };

            foreach (var r in branches)
            {
                var row = new VisualElement();
                row.AddToClassList("readiness-row");

                var icon = new Label(r.Item2);
                icon.AddToClassList("readiness-row__icon");
                row.Add(icon);

                var name = new Label(r.Item1);
                name.AddToClassList("readiness-row__name");
                row.Add(name);

                var barBg = new VisualElement();
                barBg.AddToClassList("readiness-row__bar-bg");
                var barFill = new VisualElement();
                barFill.AddToClassList("readiness-row__bar-fill");
                barFill.style.width = Length.Percent(r.Item3);
                barBg.Add(barFill);
                row.Add(barBg);

                var pct = new Label($"{r.Item3}%");
                pct.AddToClassList("readiness-row__pct");
                row.Add(pct);

                _readinessList.Add(row);
            }
        }

    }
}
