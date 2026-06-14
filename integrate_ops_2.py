import re

file_path = r"C:\Users\Tobi\StateCraft\Assets\Scripts\UI\MilitaryDashboardController.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Add fields
fields = """        // NEW OPERATIONS DASHBOARD FIELDS
        private ScrollView _threatList;
        private ScrollView _opsList;
        private ScrollView _feedList;
        private VisualElement _opTypesGrid;
        private VisualElement _readinessList;

"""
content = content.replace("private VisualElement _theatreStatusList;", fields + "        private VisualElement _theatreStatusList;")

# 2. Update InitializeElements
init_replace = """            _threatList = root.Q<ScrollView>("threat-list");
            _opsList = root.Q<ScrollView>("ops-list");
            _feedList = root.Q<ScrollView>("feed-list");
            _opTypesGrid = root.Q<VisualElement>("op-types-grid");
            _readinessList = root.Q<VisualElement>("readiness-list");

            _theatreStatusList = root.Q<VisualElement>("TheatreStatusList");"""
content = content.replace('_theatreStatusList = root.Q<VisualElement>("TheatreStatusList");', init_replace)

# 3. Update RefreshMilitaryData
refresh_old = """            PopulateWMD(country);
            PopulateTheatre(country, gm);
            PopulateOperations(country, gm);
            PopulateProjects(country);"""

refresh_new = """            PopulateWMD(country);
            PopulateThreatList(country);
            PopulateActiveOpsList(country);
            PopulateOperationsFeed(country, gm);
            PopulateOpTypesGrid(country);
            PopulateReadinessList(country);
            UpdateIntelAssessment(country);
            PopulateProjects(country);"""

content = content.replace(refresh_old, refresh_new)

# 4. Insert the new Populate methods before the end of the class
methods = """
        // ══════════════════════════════════════════════════════════════
        // NEW OPERATIONS TAB DATA POPULATION
        // ══════════════════════════════════════════════════════════════

        private void PopulateThreatList(CountryDataSO country)
        {
            if (_threatList == null) return;
            _threatList.Clear();
            foreach (var r in country.regions)
            {
                var row = new VisualElement();
                row.AddToClassList("threat-row");

                string threatType = "Stable";
                MilitaryOperationsController.ThreatLevel lvl = MilitaryOperationsController.ThreatLevel.Low;

                if (r.security < 50f)
                {
                    threatType = "Terrorist Presence";
                    lvl = MilitaryOperationsController.ThreatLevel.High;
                }
                else if (r.security < 75f)
                {
                    threatType = "Civil Unrest";
                    lvl = MilitaryOperationsController.ThreatLevel.Medium;
                }

                var icon = new VisualElement();
                icon.AddToClassList("threat-row__icon");
                icon.AddToClassList(MilitaryOperationsController.ThreatIconClass(lvl));
                var glyph = new Label(MilitaryOperationsController.ThreatGlyph(lvl));
                glyph.AddToClassList("threat-row__icon-glyph");
                glyph.style.color = MilitaryOperationsController.ThreatColour(lvl);
                icon.Add(glyph);
                row.Add(icon);

                var info = new VisualElement();
                info.AddToClassList("threat-row__info");
                var name = new Label(r.regionName);
                name.AddToClassList("threat-row__name");
                var sub = new Label(threatType);
                sub.AddToClassList("threat-row__sub");
                info.Add(name); info.Add(sub);
                row.Add(info);

                var badge = new VisualElement();
                badge.AddToClassList("threat-row__badge-col");
                var lvlLabel = new Label("THREAT LEVEL");
                lvlLabel.AddToClassList("threat-row__level-label");
                var lvlObj = new Label(lvl.ToString().ToUpper());
                lvlObj.AddToClassList("threat-row__level");
                lvlObj.AddToClassList(MilitaryOperationsController.ThreatLevelClass(lvl));
                badge.Add(lvlLabel); badge.Add(lvlObj);
                row.Add(badge);

                _threatList.Add(row);
            }
        }

        private void UpdateIntelAssessment(CountryDataSO country)
        {
            if (country.regions.Count == 0) return;
            var r = country.regions[0]; // Example: grab the first region for the assessment or the lowest security one
            foreach (var reg in country.regions)
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
            if (lblConf != null) lblConf.text = $"{r.infrastructure:F0}%";
            
            var lblSrc = root.Q<Label>("lbl-intel-source");
            if (lblSrc != null) lblSrc.text = "Signals Intelligence";
            
            var lblUpd = root.Q<Label>("lbl-last-updated");
            if (lblUpd != null) lblUpd.text = "Live";
        }

        private void PopulateActiveOpsList(CountryDataSO country)
        {
            if (_opsList == null) return;
            _opsList.Clear();
            
            if (country.activeOperations == null || country.activeOperations.Count == 0)
            {
                _opsList.Add(new Label("No Active Operations") { style = { color = Color.grey, marginLeft = 10, marginTop = 10 }});
                return;
            }

            foreach (var op in country.activeOperations)
            {
                MilitaryOperationsController.OpType typeEnum = MilitaryOperationsController.OpType.Peacekeeping;
                if (op.type.Contains("TERROR")) typeEnum = MilitaryOperationsController.OpType.CounterTerror;
                else if (op.type.Contains("BORDER")) typeEnum = MilitaryOperationsController.OpType.BorderPatrol;
                else if (op.type.Contains("RECON")) typeEnum = MilitaryOperationsController.OpType.AirRecon;
                else if (op.type.Contains("MARITIME") || op.type.Contains("NAVAL")) typeEnum = MilitaryOperationsController.OpType.Maritime;
                else if (op.type.Contains("HVT") || op.type.Contains("STRIKE")) typeEnum = MilitaryOperationsController.OpType.HvtCapture;

                int progress = 100;
                if (op.turnsToDeploy > 0) progress = 10;
                else if (op.durationMonths > 0) progress = (int)Mathf.Clamp01((float)op.monthsActive / op.durationMonths) * 100;
                else progress = 50;

                int successChance = 80;

                var card = new VisualElement();
                card.AddToClassList("op-card");
                card.AddToClassList(MilitaryOperationsController.OpTypeCardClass(typeEnum));

                var iconEl = new VisualElement();
                iconEl.AddToClassList("op-card__icon");
                var iconGlyph = new Label(MilitaryOperationsController.OpTypeGlyph(typeEnum));
                iconGlyph.AddToClassList("op-card__icon-glyph");
                iconEl.Add(iconGlyph);
                card.Add(iconEl);

                var info = new VisualElement();
                info.AddToClassList("op-card__info");
                var nameLabel = new Label(op.opName);
                nameLabel.AddToClassList("op-card__name");
                var meta = new Label($"Type: {op.type}    Region: {op.regionName}");
                meta.AddToClassList("op-card__meta");
                var obj = new Label($"Objective: Secure {op.regionName} and suppress hostilities.");
                obj.AddToClassList("op-card__objective");
                info.Add(nameLabel); info.Add(meta); info.Add(obj);
                card.Add(info);

                var progStat = new VisualElement();
                progStat.AddToClassList("op-card__stats");
                var progLbl = new Label("PROGRESS");
                progLbl.AddToClassList("op-card__stat-label");
                var progVal = new Label($"{progress}%");
                progVal.AddToClassList("op-card__stat-value");
                var barBg = new VisualElement();
                barBg.AddToClassList("op-card__progress-bar");
                var barFill = new VisualElement();
                barFill.AddToClassList("op-card__progress-fill");
                barFill.AddToClassList(MilitaryOperationsController.ProgressFillClass(progress));
                barFill.style.width = Length.Percent(progress);
                barBg.Add(barFill);
                progStat.Add(progLbl); progStat.Add(progVal); progStat.Add(barBg);
                card.Add(progStat);

                var successStat = new VisualElement();
                successStat.AddToClassList("op-card__stats");
                var successLbl = new Label("SUCCESS\\nCHANCE");
                successLbl.AddToClassList("op-card__stat-label");
                var successVal = new Label($"{successChance}%");
                successVal.AddToClassList("op-card__stat-value");
                successStat.Add(successLbl); successStat.Add(successVal);
                card.Add(successStat);

                var etaEl = new VisualElement();
                etaEl.AddToClassList("op-card__eta");
                var etaLbl = new Label("ETA");
                etaLbl.AddToClassList("op-card__eta-label");
                string etaStr = op.turnsToDeploy > 0 ? $"{op.turnsToDeploy} Mo" : "Deployed";
                var etaVal = new Label(etaStr);
                etaVal.AddToClassList("op-card__eta-value");
                etaEl.Add(etaLbl); etaEl.Add(etaVal);
                card.Add(etaEl);
                
                var viewBtn = new Button(() => { /* End mission logic */ });
                viewBtn.text = "ABORT";
                viewBtn.AddToClassList("op-card__view-btn");
                viewBtn.style.backgroundColor = new Color(244/255f, 67/255f, 54/255f, 0.4f);
                viewBtn.clicked += () => {
                        country.activeOperations.Remove(op);
                        country.unitInfantry += op.deployedTroops;
                        country.rdfStats.personnel += op.deployedRDF;
                        country.unitTanks += op.deployedTanks;
                        country.unitAPCs += op.deployedAPCs;
                        country.unitFighters += op.deployedFighters;
                        country.unitAttackHelis += op.deployedHelis;
                        RefreshMilitaryData();
                };
                card.Add(viewBtn);

                _opsList.Add(card);
            }
        }

        private void PopulateOperationsFeed(CountryDataSO country, GameManager gm)
        {
            if (_feedList == null) return;
            _feedList.Clear();
            var feedData = DashboardLiveContent.BuildMilitaryFeed(country, gm);
            if (feedData == null || feedData.Count == 0) return;

            foreach (var f in feedData)
            {
                var item = new VisualElement();
                item.AddToClassList("feed-item");

                MilitaryOperationsController.FeedColour col = MilitaryOperationsController.FeedColour.Green;
                if (f.Contains("Critical") || f.Contains("WAR") || f.Contains("KIA") || f.Contains("SHORTAGE")) col = MilitaryOperationsController.FeedColour.Red;
                else if (f.Contains("Alert") || f.Contains("Warning")) col = MilitaryOperationsController.FeedColour.Yellow;

                var dot = new VisualElement();
                dot.AddToClassList("feed-item__dot");
                dot.AddToClassList(MilitaryOperationsController.FeedDotClass(col));
                item.Add(dot);

                var dateCol = new VisualElement();
                dateCol.AddToClassList("feed-item__date-col");
                var day  = new Label("LIVE");
                day.AddToClassList("feed-item__day");
                var date = new Label($"{gm.TurnSystem.CurrentYear}/{gm.TurnSystem.CurrentMonth}");
                date.AddToClassList("feed-item__date");
                dateCol.Add(day); dateCol.Add(date);
                item.Add(dateCol);

                var text = new Label(f);
                text.AddToClassList("feed-item__text");
                item.Add(text);

                _feedList.Add(item);
            }
        }

        private void PopulateOpTypesGrid(CountryDataSO country)
        {
            if (_opTypesGrid == null) return;
            _opTypesGrid.Clear();
            
            var opTypes = new List<MilitaryOperationsController.OpTypeEntry>()
            {
                new MilitaryOperationsController.OpTypeEntry("COUNTER TERROR", "Eliminate terrorist cells and safe havens.", "RDF, Air Support", "₦50B", MilitaryOperationsController.OpType.CounterTerror),
                new MilitaryOperationsController.OpTypeEntry("BORDER PATROL", "Secure borders and prevent infiltration.", "Infantry, Air Recon", "₦15B", MilitaryOperationsController.OpType.BorderPatrol),
                new MilitaryOperationsController.OpTypeEntry("PEACEKEEPING", "Maintain stability in unstable regions.", "Infantry, Logistics", "₦35B", MilitaryOperationsController.OpType.Peacekeeping),
                new MilitaryOperationsController.OpTypeEntry("AIR RECON", "Gather intelligence and monitor threats.", "Air Wing", "₦25B", MilitaryOperationsController.OpType.AirRecon),
                new MilitaryOperationsController.OpTypeEntry("MARITIME PATROL", "Protect territorial waters and trade.", "Navy, Air Support", "₦20B", MilitaryOperationsController.OpType.Maritime),
                new MilitaryOperationsController.OpTypeEntry("HVT CAPTURE", "Capture or neutralise high value targets.", "RDF, Intelligence", "₦40B", MilitaryOperationsController.OpType.HvtCapture),
            };

            foreach (var t in opTypes)
            {
                var card = new VisualElement();
                card.AddToClassList("op-type-card");
                card.AddToClassList(MilitaryOperationsController.OpTypeCardClass(t.Type));

                var header = new VisualElement();
                header.AddToClassList("op-type-card__header");
                var iconLabel = new Label(MilitaryOperationsController.OpTypeGlyph(t.Type));
                iconLabel.AddToClassList("op-type-card__icon");
                iconLabel.style.color = MilitaryOperationsController.OpTypeAccentColour(t.Type);
                var nameLabel = new Label(t.Name);
                nameLabel.AddToClassList("op-type-card__name");
                nameLabel.style.color = MilitaryOperationsController.OpTypeAccentColour(t.Type);
                header.Add(iconLabel); header.Add(nameLabel);
                card.Add(header);

                var desc = new Label(t.Description);
                desc.AddToClassList("op-type-card__desc");
                card.Add(desc);

                var reqLabel = new Label("Required Forces");
                reqLabel.AddToClassList("op-type-card__req-label");
                card.Add(reqLabel);
                var reqVal = new Label(t.RequiredForces);
                reqVal.AddToClassList("op-type-card__req-val");
                card.Add(reqVal);

                var costLabel = new Label("Est. Cost");
                costLabel.AddToClassList("op-type-card__cost-label");
                card.Add(costLabel);
                var costVal = new Label(t.EstCost);
                costVal.AddToClassList("op-type-card__cost-val");
                card.Add(costVal);

                var selectBtn = new Button(() => { ShowLaunchOpModal(); });
                selectBtn.text = "SELECT";
                selectBtn.AddToClassList("op-type-card__select-btn");
                card.Add(selectBtn);

                _opTypesGrid.Add(card);
            }
        }

        private void PopulateReadinessList(CountryDataSO country)
        {
            if (_readinessList == null) return;
            _readinessList.Clear();

            var branches = new List<System.Tuple<string, string, int>>()
            {
                new System.Tuple<string, string, int>("ARMY", "⚔", Mathf.Clamp((int)country.armyStats.readiness, 0, 100)),
                new System.Tuple<string, string, int>("RDF", "🛡", Mathf.Clamp((int)country.rdfStats.readiness, 0, 100)),
                new System.Tuple<string, string, int>("AIR FORCE", "✈", Mathf.Clamp((int)country.airForceStats.readiness, 0, 100)),
                new System.Tuple<string, string, int>("NAVY", "⚓", Mathf.Clamp((int)country.navyStats.readiness, 0, 100))
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
"""

content = content.replace("    }\n}", methods)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
