import codecs

path = r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\TradeAgreementController.cs'
s = codecs.open(path, 'r', 'utf-8').read()

mv_start = s.find('private void UpdateMarketValue()')
mv_end = s.find('private void ShowErrorOnButton')

if mv_start != -1 and mv_end != -1:
    new_method = """private void PopulateActiveAgreements()
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
                    
                    var title = new Label($"{dirText} ➔ {partner.ToUpper()}");
                    title.style.color = new StyleColor(isSeller ? new Color(0, 0.9f, 0.47f) : new Color(0, 0.82f, 1f));
                    title.style.fontSize = 10;
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    box.Add(title);

                    float localVal = route.costPerMonthUSD * player.exchangeRate;
                    string valString = UiFormat.Money(localVal, player.currencySymbol);
                    
                    var stats = new Label($"{route.resourceType} : {UiFormat.MoneyRaw(route.volume, "")} Units\\nValue: {valString}/m | {route.monthsRemaining} mo. left");
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
                        relGain = UnityEngine.Mathf.Clamp(relGain, 1f, 15f);
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
                        else if (isSelling) _lblAdviserRecommendation.text = $"Exporting {resource} will strengthen ties and inject {localStr}/month into our treasury. Recommended.";
                        else _lblAdviserRecommendation.text = $"Importing this secures vital stock but costs {localStr}/month. Assess if the deficit is sustainable.";
                    }
                }
            }
        }

        """
    s = s[:mv_start] + new_method + s[mv_end:]
    codecs.open(path, 'w', 'utf-8').write(s)
    print("SUCCESS MV")
else:
    print("FAIL MV")
