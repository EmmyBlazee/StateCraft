using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;
using StateCraft.Interfaces;

namespace StateCraft.UI
{
    public class TradeAgreementController : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _panel;

        private ScrollView _activeAgreementsScroll;

        private DropdownField _dropdownTradeDirection;
        private DropdownField _dropdownPartner;
        private DropdownField _dropdownResource;
        private TextField _inputQuantity;
        private Label _lblMarketValue;
        private TextField _inputDuration;
        private Button _btnMaxQuantity;
        
        private Label _lblRelationsImpact;
        private Label _lblTreasuryDelta;
        private Label _lblStrategicRisk;
        private Label _lblAdviserRecommendation;
        
        private Button _btnPropose;
        private Button _btnClose;
        
        private VisualElement _counterOfferArea;
        private Label _lblCounterDetails;
        private Button _btnAccept;
        private Button _btnReject;

        private Label _lblStockOil;
        private Label _lblStockGas;
        private Label _lblStockMinerals;
        private Label _lblStockUranium;
        private Label _lblStockFood;

        private string _currentProposalId = null;

        private void Awake()
        {
            // Find and disable any standalone UIDocument that is trying to render the TradeAgreementPanel directly
            // This prevents the old popup from rendering automatically over the screen
            var allDocs = UnityEngine.Object.FindObjectsOfType<UIDocument>();
            foreach (var doc in allDocs)
            {
                if (doc.visualTreeAsset != null && doc.visualTreeAsset.name != null && doc.visualTreeAsset.name.Contains("TradeAgreement"))
                {
                    doc.enabled = false;
                }
            }

            // Strategy 2: Panel is embedded as a Template inside the DiplomacyDashboard UXML
            if (_panel == null)
            {
                var dipDoc = UnityEngine.Object.FindObjectOfType<DiplomacyDashboardController>();
                if (dipDoc != null)
                {
                    var dipUI = dipDoc.GetComponent<UIDocument>();
                    if (dipUI != null)
                    {
                        _root  = dipUI.rootVisualElement;
                        _panel = _root?.Q<VisualElement>("TradeAgreementPanel");
                    }
                }
            }

            if (_panel == null)
            {
                UnityEngine.Debug.LogWarning("[TradeAgreementController] TradeAgreementPanel not found in any UIDocument. Ensure it is assigned.");
                return;
            }

            _activeAgreementsScroll = _panel.Q<ScrollView>("ActiveAgreementsScroll");
            _lblRelationsImpact = _panel.Q<Label>("LblRelationsImpact");
            _lblTreasuryDelta = _panel.Q<Label>("LblTreasuryDelta");
            _lblStrategicRisk = _panel.Q<Label>("LblStrategicRisk");
            _lblAdviserRecommendation = _panel.Q<Label>("LblAdviserRecommendation");
            
            _dropdownTradeDirection = _panel.Q<DropdownField>("DropdownTradeDirection");
            _dropdownPartner = _panel.Q<DropdownField>("DropdownPartner");
            _dropdownResource = _panel.Q<DropdownField>("DropdownResource");
            _inputQuantity = _panel.Q<TextField>("InputQuantity");
            _lblMarketValue = _panel.Q<Label>("LblMarketValue");
            _btnMaxQuantity = _panel.Q<Button>("BtnMaxQuantity");
            
            _inputQuantity?.RegisterValueChangedCallback(evt => 
            {
                ValidateQuantityInput();
                UpdateMarketValue();
            });
            if (_btnMaxQuantity != null)
                _btnMaxQuantity.clicked += OnMaxQuantityClicked;
            _dropdownTradeDirection?.RegisterValueChangedCallback(evt => 
            {
                if (evt.newValue != "Export (Sell)" && evt.newValue != "Import (Buy)") return;
                if (_dropdownResource != null) _dropdownResource.style.display = DisplayStyle.Flex;
                PopulateDropdowns();
            });
            _dropdownResource?.RegisterValueChangedCallback(evt => 
            {
                UpdatePartners();
                UpdateMarketValue();
            });
            _inputDuration = _panel.Q<TextField>("InputDuration");
            
            _btnPropose = _panel.Q<Button>("BtnPropose");
            _btnClose = _panel.Q<Button>("BtnCloseTrade");
            
            _counterOfferArea = _panel.Q<VisualElement>("CounterOfferArea");
            _lblCounterDetails = _panel.Q<Label>("LblCounterDetails");
            _btnAccept = _panel.Q<Button>("BtnAccept");
            _btnReject = _panel.Q<Button>("BtnReject");

            _lblStockOil = _panel.Q<Label>("LblStockOil");
            _lblStockGas = _panel.Q<Label>("LblStockGas");
            _lblStockMinerals = _panel.Q<Label>("LblStockMinerals");
            _lblStockUranium = _panel.Q<Label>("LblStockUranium");
            _lblStockFood = _panel.Q<Label>("LblStockFood");

            _btnPropose.clicked += OnPropose;
            if (_btnClose != null) _btnClose.clicked += Hide;
            _btnAccept.clicked += OnAccept;
            _btnReject.clicked += OnReject;
        }

        public void Show()
        {
            if (_panel == null) return;
            
            _panel.style.display = DisplayStyle.Flex;
            _counterOfferArea.style.display = DisplayStyle.None;
            _currentProposalId = null;
            
            if (_dropdownTradeDirection != null)
            {
                _dropdownTradeDirection.index = -1;
                _dropdownTradeDirection.value = "Select Trade Type";
            }
            if (_dropdownResource != null) _dropdownResource.style.display = DisplayStyle.None;
            
            if (_dropdownPartner != null) 
            {
                _dropdownPartner.choices = new List<string> { "Select Direction & Resource" };
                _dropdownPartner.index = 0;
            }
            
            UpdateMarketValue();
            PopulateActiveAgreements();
            UpdateStockpiles();
        }

        /// <summary>
        /// Opens the trade panel with a specific partner country pre-selected.
        /// Used when opening trade from the bilateral detail view.
        /// </summary>
        public void ShowWithPartner(string partnerName)
        {
            if (_panel == null) return;

            // First reset and open the panel
            _panel.style.display = DisplayStyle.Flex;
            _counterOfferArea.style.display = DisplayStyle.None;
            _currentProposalId = null;

            // Set direction to Export (Sell) as a sensible default
            if (_dropdownTradeDirection != null)
            {
                _dropdownTradeDirection.value = "Export (Sell)";
                if (_dropdownResource != null) _dropdownResource.style.display = DisplayStyle.Flex;
                PopulateDropdowns();
            }

            // Pre-select the partner in the dropdown
            if (_dropdownPartner != null)
            {
                int idx = _dropdownPartner.choices.IndexOf(partnerName);
                if (idx >= 0)
                    _dropdownPartner.index = idx;
                // If the country is not in the list (no Trade Agreement yet), show a helpful message
                else
                {
                    NotificationManager.Instance?.AddNotification(
                        $"TRADE GATE: Sign a Trade Agreement with {partnerName} first to unlock resource trading.",
                        Color.yellow);
                }
            }

            UpdateMarketValue();
            PopulateActiveAgreements();
            UpdateStockpiles();
        }

        public void Hide()
        {
            if (_panel != null)
                _panel.style.display = DisplayStyle.None;
            
            if (!string.IsNullOrEmpty(_currentProposalId))
            {
                OnReject(); // Auto-reject if closed while proposing
            }
        }

        private void UpdateStockpiles()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            var player = gm.playerCountry;

            if (_lblStockOil != null) _lblStockOil.text = $"{player.invOilBarrels:N0}";
            if (_lblStockGas != null) _lblStockGas.text = $"{player.invGasUnits:N0}";
            if (_lblStockMinerals != null) _lblStockMinerals.text = $"{player.invMineralsTonnes:N0}";
            if (_lblStockUranium != null) _lblStockUranium.text = $"{player.invUraniumUnits:N0}";
            
            float food = player.invRiceTonnes + player.invCassavaTonnes + player.invCocoaTonnes;
            if (_lblStockFood != null) _lblStockFood.text = $"{food:N0}";
        }

        private void PopulateDropdowns()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.allCountries == null) return;

            var resources = new List<string>();
            var player = gm.playerCountry;
            bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
            
            if (isSelling)
            {
                if (player.invOilBarrels > 0 || player.resources.Exists(r => r.type == MineralType.Oil))
                    resources.Add($"Oil ({player.invOilBarrels:N0} barrels)");
                    
                if (player.resources.Exists(r => r.type == MineralType.NaturalGas))
                {
                    float amt = player.resources.Find(r => r.type == MineralType.NaturalGas).abundance * 100000f;
                    resources.Add($"Gas ({amt:N0} units)");
                }
                    
                if (player.resources.Exists(r => r.type == MineralType.Uranium))
                {
                    float amt = player.resources.Find(r => r.type == MineralType.Uranium).abundance * 500f;
                    resources.Add($"Uranium ({amt:N0} units)");
                }
                    
                float food = player.invRiceTonnes + player.invCassavaTonnes + player.invCocoaTonnes;
                if (food > 0)
                    resources.Add($"Food ({food:N0} tonnes)");
                    
                if (player.invMineralsTonnes > 0 || player.resources.Exists(r => r.type == MineralType.Gold || r.type == MineralType.Copper || r.type == MineralType.Cobalt))
                    resources.Add($"Minerals ({player.invMineralsTonnes:N0} tonnes)");
            }
            else
            {
                // If buying, player can attempt to buy ANY valid trade resource
                resources.Add("Oil");
                resources.Add("Gas");
                resources.Add("Uranium");
                resources.Add("Food");
                resources.Add("Minerals");
                resources.Add("Timber");
            }

            if (resources.Count == 0)
                resources.Add("None");

            _dropdownResource.choices = resources;
            _dropdownResource.index = 0;
            
            UpdatePartners();
            UpdateMarketValue();
            PopulateActiveAgreements();
        }

        private void UpdatePartners()
        {
            if (_dropdownPartner == null || _dropdownResource == null) return;
            
            var gm = GameManager.Instance;
            if (gm == null || gm.allCountries == null) return;
            
            string selectedStr = _dropdownResource.value;
            TradeResourceType selectedResource = TradeResourceType.Other;
            
            if (!string.IsNullOrEmpty(selectedStr) && selectedStr != "None")
            {
                string resourceEnumStr = selectedStr.Split(' ')[0];
                System.Enum.TryParse(resourceEnumStr, out selectedResource);
            }

            bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
            
            var partners = new List<string>();
            foreach (var country in gm.allCountries)
            {
                if (country == gm.playerCountry) continue;

                // ── GATE: only show countries with a signed Trade Agreement OR in the same Faction ──
                bool hasTreaty = StateCraft.Systems.DiplomacySystem.HasSignedTradeAgreementTreaty(gm.playerCountry, country);
                bool sameFaction = (gm.playerCountry.globalFaction == country.globalFaction && gm.playerCountry.globalFaction != "Independent" && gm.playerCountry.globalFaction != "Non-Aligned");
                
                if (!hasTreaty && !sameFaction)
                    continue;

                bool validPartner = false;
                
                if (isSelling)
                {
                    // Player sells, AI buys (AI must have a shortage)
                    switch (selectedResource)
                    {
                        case TradeResourceType.Oil: validPartner = country.invOilBarrels < 2000000; break;
                        case TradeResourceType.Gas: validPartner = !country.resources.Exists(r => r.type == MineralType.NaturalGas); break;
                        case TradeResourceType.Uranium: validPartner = !country.resources.Exists(r => r.type == MineralType.Uranium); break;
                        case TradeResourceType.Food: validPartner = (country.invRiceTonnes + country.invCassavaTonnes + country.invCocoaTonnes) < 500000; break;
                        case TradeResourceType.Minerals: validPartner = country.invMineralsTonnes < 1000000; break;
                        default: validPartner = true; break; 
                    }
                }
                else
                {
                    // Player buys, AI sells (AI must have a surplus / abundance)
                    switch (selectedResource)
                    {
                        case TradeResourceType.Oil: validPartner = country.invOilBarrels > 1000000 || country.resources.Exists(r => r.type == MineralType.Oil); break;
                        case TradeResourceType.Gas: validPartner = country.resources.Exists(r => r.type == MineralType.NaturalGas); break;
                        case TradeResourceType.Uranium: validPartner = country.resources.Exists(r => r.type == MineralType.Uranium); break;
                        case TradeResourceType.Food: validPartner = (country.invRiceTonnes + country.invCassavaTonnes + country.invCocoaTonnes) > 200000; break;
                        case TradeResourceType.Minerals: validPartner = country.invMineralsTonnes > 500000 || country.resources.Exists(r => r.type == MineralType.Gold); break;
                        default: validPartner = true; break; 
                    }
                }
                
                if (validPartner) partners.Add(country.countryName);
            }
            
            if (partners.Count == 0)
                partners.Add("No Interested Partners");

            _dropdownPartner.choices = partners;
            _dropdownPartner.index = 0;
        }

        private float GetPlayerStock(TradeResourceType resource)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return 0f;
            var player = gm.playerCountry;

            switch (resource)
            {
                case TradeResourceType.Oil: return player.invOilBarrels;
                case TradeResourceType.Gas: return player.invGasUnits;
                case TradeResourceType.Uranium: return player.invUraniumUnits;
                case TradeResourceType.Food: return player.invRiceTonnes + player.invCassavaTonnes + player.invCocoaTonnes;
                case TradeResourceType.Minerals: return player.invMineralsTonnes;
                default: return 0f;
            }
        }

        private void OnMaxQuantityClicked()
        {
            if (_dropdownResource == null || _inputQuantity == null) return;
            string selectedStr = _dropdownResource.value;
            if (string.IsNullOrEmpty(selectedStr) || selectedStr == "None") return;
            
            bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
            if (!isSelling) return; // Only makes sense to Max when selling your own stock

            string resourceEnumStr = selectedStr.Split(' ')[0];
            if (System.Enum.TryParse(resourceEnumStr, out TradeResourceType resource))
            {
                float stock = GetPlayerStock(resource);
                _inputQuantity.value = stock.ToString("F0");
            }
        }

        private void ValidateQuantityInput()
        {
            if (_inputQuantity == null || string.IsNullOrEmpty(_inputQuantity.value)) return;
            bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
            if (!isSelling) return;

            string selectedStr = _dropdownResource?.value;
            if (string.IsNullOrEmpty(selectedStr) || selectedStr == "None") return;

            string resourceEnumStr = selectedStr.Split(' ')[0];
            if (System.Enum.TryParse(resourceEnumStr, out TradeResourceType resource))
            {
                if (float.TryParse(_inputQuantity.value, out float qty))
                {
                    float stock = GetPlayerStock(resource);
                    if (qty > stock)
                    {
                        // Just warn, don't hard block.
                        NotificationManager.Instance?.AddNotification("Warning: Export quantity exceeds current inventory.", Color.yellow);
                    }
                }
            }
        }

        private void PopulateActiveAgreements()
        {
            if (_activeAgreementsScroll == null) return;
            _activeAgreementsScroll.Clear();

            var gm = GameManager.Instance;
            if (gm == null || gm.playerCountry == null) return;
            
            var player = gm.playerCountry;
            bool foundAny = false;

            if (player.activeTradeAgreements != null)
            {
                foreach (var route in player.activeTradeAgreements)
                {
                    if (route.status != StateCraft.Interfaces.TradeAgreementStatus.Active) continue;
                    
                    foundAny = true;
                    var box = new VisualElement();
                    box.style.backgroundColor = new StyleColor(new Color(0,0,0,0.3f));
                    box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 1;
                    box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = new StyleColor(new Color(255/255f, 255/255f, 255/255f, 0.1f));
                    box.style.paddingLeft = 8;
                    box.style.paddingRight = 8;
                    box.style.paddingTop = 6;
                    box.style.paddingBottom = 6;
                    box.style.marginBottom = 5;

                    bool isSeller = route.isSeller;
                    string partner = route.partnerCountry;
                    string dirText = isSeller ? "EXPORTING" : "IMPORTING";
                    
                    var title = new Label($"{dirText} ➔ {partner.ToUpper()}");
                    title.style.color = new StyleColor(isSeller ? new Color(0, 0.9f, 0.47f) : new Color(0, 0.82f, 1f));
                    title.style.fontSize = 10;
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    box.Add(title);

                    float localVal = route.monthlyPaymentUSD * player.exchangeRate;
                    string valString = UiFormat.MoneyRaw(localVal, player.currencySymbol);
                    
                    var stats = new Label($"{route.resource} : {UiFormat.MoneyRaw(route.resourceQuantity, "")} Units\nValue: {valString}/m | {route.durationMonths} mo. left");
                    stats.style.color = new StyleColor(Color.white);
                    stats.style.fontSize = 9;
                    box.Add(stats);
                    
                    _activeAgreementsScroll.Add(box);
                }
            }

            if (!foundAny)
            {
                var lbl = new Label("No Active Trade Routes");
                lbl.style.color = new StyleColor(Color.grey);
                lbl.style.fontSize = 10;
                lbl.style.marginTop = 10;
                _activeAgreementsScroll.Add(lbl);
            }
        }

        private void UpdateMarketValue()
        {
            if (_lblMarketValue == null || _dropdownResource == null || _inputQuantity == null) return;
            
            string selectedStr = _dropdownResource.value;
            if (string.IsNullOrEmpty(selectedStr) || selectedStr == "None")
            {
                _lblMarketValue.text = "₦0.0 / Month";
                if (_lblTreasuryDelta != null) _lblTreasuryDelta.text = "-";
                if (_lblRelationsImpact != null) _lblRelationsImpact.text = "-";
                if (_lblStrategicRisk != null) _lblStrategicRisk.text = "Low";
                if (_lblAdviserRecommendation != null) _lblAdviserRecommendation.text = "Awaiting proposal...";
                return;
            }
            
            string resourceEnumStr = selectedStr.Split(' ')[0];
            if (System.Enum.TryParse(resourceEnumStr, out TradeResourceType resource))
            {
                if (float.TryParse(_inputQuantity.value, out float qty))
                {
                    bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
                    float pricePerUnit = GlobalTradeMarket.GetPricePerUnit(resource);
                    float totalValUSD = qty * pricePerUnit;
                    
                    var gm = GameManager.Instance;
                    string localStr = "";
                    float localVal = 0;
                    if (gm != null && gm.playerCountry != null)
                    {
                        var p = gm.playerCountry;
                        localVal = totalValUSD * p.exchangeRate;
                        localStr = $"{UiFormat.MoneyRaw(localVal, p.currencySymbol)}";
                    }
                    
                    _lblMarketValue.text = $"{localStr} / Month";

                    if (_lblTreasuryDelta != null)
                    {
                        _lblTreasuryDelta.text = isSelling ? $"+{localStr}" : $"-{localStr}";
                        _lblTreasuryDelta.style.color = new StyleColor(isSelling ? new Color(0, 0.9f, 0.47f) : new Color(1f, 0.32f, 0.32f));
                    }

                    if (_lblRelationsImpact != null)
                    {
                        float relGain = (totalValUSD / 10000000f) + (isSelling ? 0.5f : 1.5f);
                        relGain = UnityEngine.Mathf.Clamp(relGain, 1f, 15f);
                        _lblRelationsImpact.text = $"+{relGain:F1}";
                        _lblRelationsImpact.style.color = new StyleColor(new Color(0, 0.82f, 1f));
                    }

                    if (_lblStrategicRisk != null)
                    {
                        if (resource == TradeResourceType.Uranium)
                        {
                            _lblStrategicRisk.text = "HIGH";
                            _lblStrategicRisk.style.color = new StyleColor(new Color(1f, 0.32f, 0.32f));
                        }
                        else
                        {
                            _lblStrategicRisk.text = "Low";
                            _lblStrategicRisk.style.color = new StyleColor(new Color(0, 0.9f, 0.47f));
                        }
                    }

                    if (_lblAdviserRecommendation != null)
                    {
                        if (qty == 0) 
                        {
                            _lblAdviserRecommendation.text = "Adjust the volume slider.";
                            _lblAdviserRecommendation.style.color = new StyleColor(Color.white);
                        }
                        else if (isSelling) 
                        {
                            float production = 0f;
                            switch(resource)
                            {
                                case TradeResourceType.Oil: production = gm.playerCountry.indMiningOil * 2000000f; break;
                                case TradeResourceType.Gas: production = gm.playerCountry.resources.Exists(r => r.type == MineralType.NaturalGas) ? gm.playerCountry.indMiningOil * 500000f : 0f; break;
                                case TradeResourceType.Uranium: production = gm.playerCountry.resources.Exists(r => r.type == MineralType.Uranium) ? gm.playerCountry.indMiningLocal * 5000f : 0f; break;
                                case TradeResourceType.Minerals: production = gm.playerCountry.indMiningLocal * 100000f; break;
                                case TradeResourceType.Food: production = (gm.playerCountry.indAgriRice * 50000f) + (gm.playerCountry.indAgriCassava * 80000f) + (gm.playerCountry.indAgriCocoa * 20000f); break;
                            }
                            float consumption = (resource == TradeResourceType.Food) ? gm.playerCountry.population * 0.001f : 0f;
                            float surplus = production - consumption;
                            
                            // Subtract other active export deals from surplus to be accurate
                            if (gm.playerCountry.activeTradeAgreements != null)
                            {
                                foreach (var deal in gm.playerCountry.activeTradeAgreements)
                                {
                                    if (deal.status == StateCraft.Interfaces.TradeAgreementStatus.Active && deal.isSeller && deal.resource == resource)
                                        surplus -= deal.resourceQuantity;
                                }
                            }

                            if (qty > surplus)
                            {
                                _lblAdviserRecommendation.text = $"⚠ WARNING: Export ({qty:N0}) exceeds your monthly surplus ({surplus:N0}). Your stockpile will drain.";
                                _lblAdviserRecommendation.style.color = new StyleColor(Color.yellow);
                            }
                            else
                            {
                                _lblAdviserRecommendation.text = $"Exporting {resource} will strengthen ties and inject {localStr}/month into our treasury. Recommended.";
                                _lblAdviserRecommendation.style.color = new StyleColor(Color.white);
                            }
                        }
                        else 
                        {
                            _lblAdviserRecommendation.text = $"Importing this secures vital stock but costs {localStr}/month. Assess if the deficit is sustainable.";
                            _lblAdviserRecommendation.style.color = new StyleColor(Color.white);
                        }
                    }
                }
            }
        }

        private void ShowErrorOnButton(string errorMsg)
        {
            if (_btnPropose != null)
            {
                string oldText = _btnPropose.text;
                _btnPropose.text = errorMsg;
                _btnPropose.style.backgroundColor = new StyleColor(Color.red);
                
                // Reset after 3 seconds
                _btnPropose.schedule.Execute(() => 
                {
                    _btnPropose.text = "PROPOSE TRADE";
                    _btnPropose.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0)); // Revert to stylesheet default
                }).StartingIn(3000);
            }
        }

        private void OnPropose()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.DiplomacySystem == null) return;

            if (_dropdownTradeDirection != null && _dropdownTradeDirection.value != "Export (Sell)" && _dropdownTradeDirection.value != "Import (Buy)")
            {
                ShowErrorOnButton("ERROR: SELECT TRADE TYPE");
                return;
            }

            string partnerName = _dropdownPartner.value;
            if (partnerName == "No Interested Partners" || string.IsNullOrEmpty(partnerName))
            {
                // Give a more helpful message if the list is empty due to no treaties
                bool hasAnyTreaty = gm.playerCountry?.activeTreaties != null &&
                    gm.playerCountry.activeTreaties.Exists(t => t.treatyName == "Trade Agreement");
                if (!hasAnyTreaty)
                    ShowErrorOnButton("SIGN TRADE AGREEMENT FIRST");
                else
                    ShowErrorOnButton("ERROR: NO PARTNER");
                return;
            }

            CountryDataSO partner = null;
            foreach (var c in gm.allCountries)
            {
                if (c.countryName == partnerName)
                {
                    partner = c;
                    break;
                }
            }

            if (partner == null) return;

            string selectedStr = _dropdownResource.value;
            if (string.IsNullOrEmpty(selectedStr) || selectedStr == "None")
            {
                ShowErrorOnButton("ERROR: NO RESOURCE");
                return;
            }
            
            // Extract the first word which maps to TradeResourceType
            string resourceEnumStr = selectedStr.Split(' ')[0];
            TradeResourceType resource = (TradeResourceType)System.Enum.Parse(typeof(TradeResourceType), resourceEnumStr);
            
            if (!float.TryParse(_inputQuantity.value, out float quantity))
                quantity = 1000f;
            
            if (!int.TryParse(_inputDuration.value, out int duration))
                duration = 12;

            bool isSelling = _dropdownTradeDirection == null || _dropdownTradeDirection.value == "Export (Sell)";
            // We removed the hard block: if (isSelling && quantity > GetPlayerStock(resource)) return;

            bool isBulk = false;
            
            float pricePerUnit = GlobalTradeMarket.GetPricePerUnit(resource);
            float basePayment = (quantity * pricePerUnit) / 1e6f; // Store payment internally in Millions if needed, wait, the game uses raw USD or Millions? 
            // In DiplomacySystem we usually use raw USD. Let's use raw USD.
            basePayment = quantity * pricePerUnit;

            if (gm.DiplomacySystem is DiplomacySystem diplo)
            {
                var proposal = diplo.CreateProposal(gm.playerCountry, partner, resource, basePayment, duration, isSelling);
                if (proposal == null)
                {
                    ShowErrorOnButton("SIGN TRADE AGREEMENT FIRST");
                    return;
                }
                proposal.resourceQuantity = quantity;
                proposal.isBulkTransfer = isBulk;
                
                // Immediately show the counter offer (since AI evaluation is synchronous in the plan)
                OnReceiveCounterOffer(proposal);
            }
        }


        private void ShowErrorOnButton(Button btn, string errorMsg, string originalText)
        {
            if (btn != null)
            {
                btn.text = errorMsg;
                btn.style.backgroundColor = new StyleColor(UnityEngine.Color.red);
                
                // Reset after 3 seconds
                btn.schedule.Execute(() => 
                {
                    btn.text = originalText;
                    btn.style.backgroundColor = new StyleColor(new UnityEngine.Color(0, 0, 0, 0)); // Revert to stylesheet default
                }).StartingIn(3000);
            }
        }

        private void OnReceiveCounterOffer(TradeAgreementProposal proposal)
        {
            _currentProposalId = proposal.id;
            _counterOfferArea.style.display = DisplayStyle.Flex;
            string type = "MONTHLY";
            
            float usd = proposal.baseMonthlyPaymentUSD;
            float local = usd * GameManager.Instance.playerCountry.exchangeRate;
            string currSym = GameManager.Instance.playerCountry.currencySymbol;
            string currName = GameManager.Instance.playerCountry.currencyName;
            
            _lblCounterDetails.text = $"Payment ({type}): {UiFormat.MoneyRaw(usd, "$")} USD ({UiFormat.MoneyRaw(local, currSym)} {currName}) | Qty: {UiFormat.MoneyRaw(proposal.resourceQuantity, "")} | Duration: {proposal.baseDurationMonths} Months";
        }

        private void OnAccept()
        {
            if (string.IsNullOrEmpty(_currentProposalId)) 
            {
                ShowErrorOnButton(_btnAccept, "ERROR: INVALID ID", "ACCEPT");
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null)
            {
                ShowErrorOnButton(_btnAccept, "ERROR: NO GM", "ACCEPT");
                return;
            }

            if (gm.DiplomacySystem is DiplomacySystem diplo)
            {
                try
                {
                    var agreement = diplo.AcceptProposal(_currentProposalId);
                    if (agreement == null)
                    {
                        ShowErrorOnButton(_btnAccept, "ERROR: EXPIRED", "ACCEPT");
                        return;
                    }
                    
                    _currentProposalId = null;
                    _counterOfferArea.style.display = DisplayStyle.None;
                    
                    // Refresh diplomacy dashboard to show the new agreement
                    var dashboard = UnityEngine.Object.FindObjectOfType<DiplomacyDashboardController>();
                    if (dashboard != null) dashboard.RefreshDiplomacyData();
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"[TradeAgreementController] Exception in OnAccept: {ex}");
                    ShowErrorOnButton(_btnAccept, "ERROR: EXCEPTION", "ACCEPT");
                }
            }
            else
            {
                ShowErrorOnButton(_btnAccept, "ERROR: TYPE MISMATCH", "ACCEPT");
            }
        }

        private void OnReject()
        {
            if (string.IsNullOrEmpty(_currentProposalId)) return;
            
            var gm = GameManager.Instance;
            if (gm != null && gm.DiplomacySystem is DiplomacySystem diplo)
            {
                diplo.CancelProposal(_currentProposalId);
            }
            
            _currentProposalId = null;
            _counterOfferArea.style.display = DisplayStyle.None;
        }
    }
}
