import codecs
import re

path = r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\TradeAgreementController.cs'
s = codecs.open(path, 'r', 'utf-8').read()

# Replace the fields
idx_start = s.find('private VisualElement _panel;')
idx_awake = s.find('private void Awake()')

fields = '''private VisualElement _panel;

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
'''
s = s[:idx_start] + fields + s[idx_awake:]

# Replace Awake bindings
idx_awake_body = s.find('_panel = _root.Q<VisualElement>("TradeAgreementPanel");')
idx_post_awake = s.find('_dropdownTradeDirection = _panel.Q<DropdownField>("DropdownTradeDirection");')

binds = '''_panel = _root.Q<VisualElement>("TradeAgreementPanel");
            if (_panel == null) return;
            
            _activeAgreementsScroll = _panel.Q<ScrollView>("ActiveAgreementsScroll");
            _lblRelationsImpact = _panel.Q<Label>("LblRelationsImpact");
            _lblTreasuryDelta = _panel.Q<Label>("LblTreasuryDelta");
            _lblStrategicRisk = _panel.Q<Label>("LblStrategicRisk");
            _lblAdviserRecommendation = _panel.Q<Label>("LblAdviserRecommendation");
            
            '''
s = s[:idx_awake_body] + binds + s[idx_post_awake:]

idx_show = s.find('UpdateMarketValue();\n        }')
if idx_show != -1:
    s = s.replace('UpdateMarketValue();\n        }', 'UpdateMarketValue();\n            PopulateActiveAgreements();\n        }')
elif s.find('UpdateMarketValue();\r\n        }') != -1:
    s = s.replace('UpdateMarketValue();\r\n        }', 'UpdateMarketValue();\r\n            PopulateActiveAgreements();\r\n        }')

update_mv_start = s.find('private void UpdateMarketValue()')
update_mv_end = s.find('private void ShowErrorOnButton', update_mv_start)

mv_method = '''private void UpdateMarketValue()
        {
            if (_lblMarketValue == null || _dropdownResource == null || _inputQuantity == null) return;
            
            string selectedStr = _dropdownResource.value;
            if (string.IsNullOrEmpty(selectedStr) || selectedStr == "None")
            {
                _lblMarketValue.text = "Value: $0.0M USD";
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
                        localStr = $"{UiFormat.Money(localVal, p.currencySymbol)}";
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
                        relGain = Mathf.Clamp(relGain, 1f, 15f);
                        _lblRelationsImpact.text = $"+{relGain:F1}";
                        _lblRelationsImpact.style.color = new StyleColor(new Color(0, 0.82f, 1f));
                    }

                    if (_lblStrategicRisk != null)
                    {
                        if (resource == TradeResourceType.Uranium || resource == TradeResourceType.Weapons)
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
                        if (qty == 0) _lblAdviserRecommendation.text = "Adjust the volume slider.";
                        else if (isSelling) _lblAdviserRecommendation.text = $"Exporting {resource.ToString()} will strengthen ties and inject {localStr}/m into our treasury. Recommended.";
                        else _lblAdviserRecommendation.text = $"Importing this secures vital stock but costs {localStr}/m. Assess if the deficit is sustainable.";
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

            if (player.activeTradeRoutes != null)
            {
                foreach (var route in player.activeTradeRoutes)
                {
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

                    bool isSeller = route.seller == player.countryName;
                    string partner = isSeller ? route.buyer : route.seller;
                    string dirText = isSeller ? "EXPORTING" : "IMPORTING";
                    
                    var title = new Label($"{dirText} TO {partner.ToUpper()}");
                    title.style.color = new StyleColor(isSeller ? new Color(0, 0.9f, 0.47f) : new Color(0, 0.82f, 1f));
                    title.style.fontSize = 10;
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    box.Add(title);

                    float localVal = route.costPerMonthUSD * player.exchangeRate;
                    string valString = UiFormat.Money(localVal, player.currencySymbol);
                    
                    var stats = new Label($"{route.resourceType.ToString()} : {UiFormat.MoneyRaw(route.volume, "")} Units\\nValue: {valString}/m | {route.monthsRemaining} mo. left");
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

        '''

s = s[:update_mv_start] + mv_method + s[update_mv_end:]
codecs.open(path, 'w', 'utf-8').write(s)
print('SUCCESS')
