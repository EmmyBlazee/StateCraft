using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public partial class MilitaryDashboardController
    {
        // UXML Element References
        private Label _basesStatTotal;
        private Label _basesStatCapacity;
        private Label _basesStatOccupied;
        private Label _basesStatUpkeep;
        private ScrollView _basesListContainer;
        
        private Label _detailBaseName;
        private Label _detailBaseStatus;
        private Label _detailBaseEstablished;
        private Label _detailBaseGarrison;
        private Label _detailBasePersonnel;
        private Label _detailBaseSize;
        private Label _detailBaseThreat;
        
        private VisualElement _baseInfoListContainer;
        private VisualElement _baseCapabilitiesContainer;
        private VisualElement _assignedUnitsContainer;
        private VisualElement _baseActivityContainer;
        
        private VisualElement _globalBasesOverviewContainer;
        private ScrollView _constructionQueueContainer;
        private ScrollView _upgradeQueueContainer;
        
        private bool _basesWired = false;

        private void EnsureBasesWired()
        {
            if (_basesWired) return;
            if (_panelBases == null) 
            {
                Debug.Log("[Bases] _panelBases is null!");
                return;
            }
            
            BindBasesElements(_panelBases);
            
            if (_basesListContainer != null) 
            {
                _basesWired = true;
                Debug.Log("[Bases] EnsureBasesWired success!");
            }
            else
            {
                Debug.Log("[Bases] BindBasesElements failed to find _basesListContainer!");
                DumpVisualTree(_panelBases, 0);
            }
        }

        private void DumpVisualTree(VisualElement element, int depth)
        {
            if (element == null) return;
            string indent = new string('-', depth * 2);
            Debug.Log($"[BasesTree] {indent} Name: {element.name}, Type: {element.GetType().Name}");
            foreach (var child in element.Children())
            {
                DumpVisualTree(child, depth + 1);
            }
        }

        private void BindBasesElements(VisualElement root)
        {
            _basesStatTotal = root.Q<Label>("BasesStatTotal");
            _basesStatCapacity = root.Q<Label>("BasesStatCapacity");
            _basesStatOccupied = root.Q<Label>("BasesStatOccupied");
            _basesStatUpkeep = root.Q<Label>("BasesStatUpkeep");
            
            _basesListContainer = root.Q<ScrollView>("BasesListContainer");
            
            _detailBaseName = root.Q<Label>("DetailBaseName");
            _detailBaseStatus = root.Q<Label>("DetailBaseStatus");
            _detailBaseEstablished = root.Q<Label>("DetailBaseEstablished");
            _detailBaseGarrison = root.Q<Label>("DetailBaseGarrison");
            _detailBasePersonnel = root.Q<Label>("DetailBasePersonnel");
            _detailBaseSize = root.Q<Label>("DetailBaseSize");
            _detailBaseThreat = root.Q<Label>("DetailBaseThreat");
            
            _baseInfoListContainer = root.Q<VisualElement>("BaseInfoListContainer");
            _baseCapabilitiesContainer = root.Q<VisualElement>("BaseCapabilitiesContainer");
            _assignedUnitsContainer = root.Q<VisualElement>("AssignedUnitsContainer");
            _baseActivityContainer = root.Q<VisualElement>("BaseActivityContainer");
            
            _globalBasesOverviewContainer = root.Q<VisualElement>("GlobalBasesOverviewContainer");
            _constructionQueueContainer = root.Q<ScrollView>("ConstructionQueueContainer");
            _upgradeQueueContainer = root.Q<ScrollView>("UpgradeQueueContainer");
        }

        private void PopulateBases(CountryDataSO country)
        {
            Debug.Log("[Bases] PopulateBases called");
            if (!_basesWired) EnsureBasesWired();
            if (!_basesWired) 
            {
                Debug.Log("[Bases] Not wired, aborting populate.");
                return;
            }
            if (_basesListContainer == null) return;
            
            try 
            {
            
            // 1. Top Stats (Mocked to match design)
            if (_basesStatTotal != null) _basesStatTotal.text = "12";
            if (_basesStatCapacity != null) _basesStatCapacity.text = "38,000";
            if (_basesStatOccupied != null) _basesStatOccupied.text = "24,780";
            if (_basesStatUpkeep != null) _basesStatUpkeep.text = "₦12.4B";

            // 2. Base List (Left Column)
            _basesListContainer.Clear();
            AddBaseListItem("Lagos Joint Base", "Lagos State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "2,450 / 3,000", "Joint Operations v", FaIcons.Shield, true);
            AddBaseListItem("Kano Army Base", "Kano State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "1,800 / 2,500", "Training & Logistics v", FaIcons.HardHat, false);
            AddBaseListItem("Maiduguri Air Base", "Borno State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "1,200 / 2,000", "Air Superiority v", FaIcons.Tractor, false); // Using Tractor as plane placeholder if needed
            AddBaseListItem("Port Harcourt Naval Base", "Rivers State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "1,100 / 1,500", "Maritime Operations v", FaIcons.Shield, false);
            AddBaseListItem("Kaduna Army Base", "Kaduna State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "1,600 / 2,000", "Armored Division v", FaIcons.Shield, false);
            AddBaseListItem("Uyo Air Base", "Akwa Ibom State", "UNDER CONSTRUCTION", new Color(1f, 0.67f, 0.25f), "800 / 1,500", "Support & Transport v", FaIcons.Building, false);
            AddBaseListItem("Calabar Naval Base", "Cross River State", "OPERATIONAL", new Color(0.14f, 0.93f, 0.34f), "900 / 1,200", "Coastal Defense v", FaIcons.Shield, false);
            AddBaseListItem("Gusau Forward Base", "Zamfara State", "UNDER CONSTRUCTION", new Color(1f, 0.67f, 0.25f), "450 / 1,000", "Forward Operations v", FaIcons.Hammer, false);

            if (country.defenseFactories != null)
            {
                foreach (var fac in country.defenseFactories)
                {
                    string status = fac.level > 0 ? "OPERATIONAL" : "UNDER CONSTRUCTION";
                    Color statusColor = fac.level > 0 ? new Color(0.14f, 0.93f, 0.34f) : new Color(1f, 0.67f, 0.25f);
                    AddBaseListItem(fac.factoryName, "Defense Facility", status, statusColor, $"{fac.utilizationPct:F0}% Utilized", $"Level {fac.level} Factory", FaIcons.Wrench, false);
                }
            }
            // 3. Base Details Header
            if (_detailBaseName != null) _detailBaseName.text = "LAGOS JOINT BASE";
            if (_detailBaseStatus != null) _detailBaseStatus.text = "OPERATIONAL";
            if (_detailBaseEstablished != null) _detailBaseEstablished.text = "2018";
            if (_detailBaseGarrison != null) _detailBaseGarrison.text = "3,000";
            if (_detailBasePersonnel != null) _detailBasePersonnel.text = "2,450 (82%)";
            if (_detailBaseSize != null) _detailBaseSize.text = "520 ha";
            if (_detailBaseThreat != null) _detailBaseThreat.text = "LOW";

            // 4. Base Information List
            _baseInfoListContainer.Clear();
            AddBaseInfoRow("Location", "Lagos State, Nigeria", FaIcons.Globe);
            AddBaseInfoRow("Type", "Joint Base", FaIcons.Building);
            AddBaseInfoRow("Region", "South West", FaIcons.Globe);
            AddBaseInfoRow("Command", "Nigerian Joint Command", FaIcons.Shield);
            AddBaseInfoRow("Monthly Upkeep", "₦2.8B", FaIcons.Coins);

            // 5. Capabilities
            _baseCapabilitiesContainer.Clear();
            AddCapabilityRow("Command & Control", 85);
            AddCapabilityRow("Logistics Support", 78);
            AddCapabilityRow("Maintenance", 72);
            AddCapabilityRow("Power Supply", 90);
            AddCapabilityRow("Water Supply", 68);
            AddCapabilityRow("Runway Capacity", 80);
            AddCapabilityRow("Defenses", 75);

            // 6. Assigned Units
            _assignedUnitsContainer.Clear();
            AddAssignedUnit("1st Armored Brigade", "850", FaIcons.Shield);
            AddAssignedUnit("9th Infantry Division", "1,200", FaIcons.UserSecret);
            AddAssignedUnit("Air Wing 301", "250", FaIcons.Bolt);
            AddAssignedUnit("Logistics Group 45", "150", FaIcons.Road);

            // 7. Base Activity
            _baseActivityContainer.Clear();
            AddActivityIcon("Personnel Trained", "1,250", FaIcons.UserSecret);
            AddActivityIcon("Flights Conducted", "86", FaIcons.Bolt);
            AddActivityIcon("Supply Deliveries", "142", FaIcons.Road);
            AddActivityIcon("Maintenance Ops", "73", FaIcons.Wrench);
            AddActivityIcon("Patrols Launched", "25", FaIcons.Shield);

            // 8. Global Overview
            _globalBasesOverviewContainer.Clear();
            AddOverviewRow("Operational Bases", "9", new Color(0.14f, 0.93f, 0.34f));
            AddOverviewRow("Under Construction", "2", new Color(1f, 0.67f, 0.25f));
            AddOverviewRow("Planned", "1", new Color(0.24f, 0.53f, 0.94f));
            AddOverviewRow("Inactive", "0", Color.grey);

            // 9. Construction Queue
            _constructionQueueContainer.Clear();
            AddQueueItem(_constructionQueueContainer, "Uyo Air Base", "Akwa Ibom State", 56, "120 Days", "₦38.6B", FaIcons.Bolt);
            AddQueueItem(_constructionQueueContainer, "Gusau Forward Base", "Zamfara State", 32, "180 Days", "₦24.2B", FaIcons.Hammer);

            // 10. Upgrade Queue
            _upgradeQueueContainer.Clear();
            AddQueueItem(_upgradeQueueContainer, "Lagos Joint Base", "Expand Runway Capacity", 35, "60 Days", "₦12.6B", FaIcons.Shield);
            
            Debug.Log("[Bases] PopulateBases finished successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Bases] Exception during PopulateBases: {ex}");
            }
        }

        private void AddBaseListItem(string name, string location, string status, Color statusColor, string personnel, string role, string icon, bool isActive)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, marginBottom = 8, backgroundColor = isActive ? new Color(0.05f, 0.1f, 0.05f, 0.5f) : new Color(0.05f, 0.07f, 0.1f, 0.95f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 0.5f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderBottomColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 0.5f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderLeftColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 0.5f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderRightColor = isActive ? new Color(0.14f, 0.93f, 0.34f, 0.5f) : new Color(0.2f, 0.25f, 0.35f, 0.5f), borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
            
            var iconBox = new VisualElement { style = { width = 28, height = 28, justifyContent = Justify.Center, alignItems = Align.Center, marginRight = 10 } };
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.white } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            card.Add(iconBox);
            
            var mid = new VisualElement { style = { flexGrow = 1, flexDirection = FlexDirection.Column, justifyContent = Justify.Center } };
            mid.Add(new Label(name) { style = { fontSize = 11, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            mid.Add(new Label(location) { style = { fontSize = 9, color = Color.grey, marginBottom = 6 } });
            var statRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            statRow.Add(new VisualElement { style = { width = 6, height = 6, borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3, backgroundColor = statusColor, marginRight = 6 } });
            statRow.Add(new Label(status) { style = { fontSize = 8, color = statusColor } });
            mid.Add(statRow);
            card.Add(mid);
            
            var right = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.FlexEnd, justifyContent = Justify.SpaceBetween } };
            var rightTopRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var personIcon = new Label(FaIcons.UserSecret) { style = { fontSize = 8, color = Color.grey, marginRight = 4 } };
            personIcon.AddToClassList("fa-icon");
            rightTopRow.Add(personIcon);
            rightTopRow.Add(new Label(personnel) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            var arrow = new Label("›") { style = { fontSize = 12, color = Color.grey, marginLeft = 4 } };
            rightTopRow.Add(arrow);
            right.Add(rightTopRow);
            
            right.Add(new Label(role) { style = { fontSize = 9, color = Color.grey } });
            card.Add(right);
            
            _basesListContainer.Add(card);
        }

        private void AddBaseInfoRow(string label, string value, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 8 } };
            var iconBox = new VisualElement { style = { width = 20, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 10, color = Color.grey } };
            iconLabel.AddToClassList("fa-icon");
            iconBox.Add(iconLabel);
            row.Add(iconBox);
            
            row.Add(new Label(label) { style = { fontSize = 9, color = Color.grey, width = 80 } });
            row.Add(new Label(value) { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _baseInfoListContainer.Add(row);
        }

        private void AddCapabilityRow(string label, float val)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            row.Add(new Label(label) { style = { fontSize = 9, color = Color.grey, width = 100 } });
            var pBar = new ProgressBar { value = val, highValue = 100, style = { width = 80, height = 4 } };
            pBar.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            row.Add(pBar);
            row.Add(new Label($"{val:F0}%") { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, width = 30, unityTextAlign = TextAnchor.MiddleRight } });
            _baseCapabilitiesContainer.Add(row);
        }

        private void AddAssignedUnit(string name, string personnel, string icon)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 12, color = Color.white, width = 20 } };
            iconLabel.AddToClassList("fa-icon");
            left.Add(iconLabel);
            left.Add(new Label(name) { style = { fontSize = 9, color = Color.grey } });
            row.Add(left);
            row.Add(new Label(personnel) { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _assignedUnitsContainer.Add(row);
        }

        private void AddActivityIcon(string label, string val, string icon)
        {
            var col = new VisualElement { style = { flexDirection = FlexDirection.Column, alignItems = Align.Center } };
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.grey, marginBottom = 6 } };
            iconLabel.AddToClassList("fa-icon");
            col.Add(iconLabel);
            col.Add(new Label(label) { style = { fontSize = 8, color = Color.grey, marginBottom = 2 } });
            col.Add(new Label(val) { style = { fontSize = 12, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _baseActivityContainer.Add(col);
        }

        private void AddOverviewRow(string label, string val, Color dotColor)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, alignItems = Align.Center, marginBottom = 8 } };
            var left = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            left.Add(new VisualElement { style = { width = 8, height = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4, backgroundColor = dotColor, marginRight = 8 } });
            left.Add(new Label(label) { style = { fontSize = 10, color = Color.white } });
            row.Add(left);
            row.Add(new Label(val) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold } });
            _globalBasesOverviewContainer.Add(row);
        }

        private void AddQueueItem(VisualElement container, string name, string subtitle, float progress, string eta, string cost, string icon)
        {
            var card = new VisualElement { style = { flexDirection = FlexDirection.Column, backgroundColor = rgba(255,255,255,0.02f), borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1, borderTopColor = rgba(255,255,255,0.05f), borderBottomColor = rgba(255,255,255,0.05f), borderLeftColor = rgba(255,255,255,0.05f), borderRightColor = rgba(255,255,255,0.05f), paddingTop = 12, paddingBottom = 12, paddingLeft = 12, paddingRight = 12, marginBottom = 8, borderTopLeftRadius = 4, borderTopRightRadius = 4, borderBottomLeftRadius = 4, borderBottomRightRadius = 4 } };
            var top = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 12 } };
            var iconLabel = new Label(icon) { style = { fontSize = 16, color = Color.grey, marginRight = 10 } };
            iconLabel.AddToClassList("fa-icon");
            top.Add(iconLabel);
            var mid = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            mid.Add(new Label(name) { style = { fontSize = 10, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 2 } });
            mid.Add(new Label(subtitle) { style = { fontSize = 9, color = Color.grey } });
            top.Add(mid);
            card.Add(top);
            
            var progRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var pBar = new ProgressBar { value = progress, highValue = 100, style = { flexGrow = 1, height = 4, marginRight = 8 } };
            pBar.style.backgroundColor = new Color(0.14f, 0.93f, 0.34f);
            progRow.Add(pBar);
            progRow.Add(new Label($"{progress:F0}%") { style = { fontSize = 9, color = Color.white } });
            card.Add(progRow);
            
            var botRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            botRow.Add(new Label($"ETA: {eta}") { style = { fontSize = 9, color = Color.grey } });
            
            var costBox = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            costBox.Add(new Label(cost) { style = { fontSize = 9, color = Color.white, unityFontStyleAndWeight = FontStyle.Bold, marginRight = 6 } });
            costBox.Add(new Label("×") { style = { fontSize = 10, color = Color.grey } });
            botRow.Add(costBox);
            
            card.Add(botRow);
            container.Add(card);
        }
        
        private Color rgba(int r, int g, int b, float a)
        {
            return new Color(r/255f, g/255f, b/255f, a);
        }
    }
}
