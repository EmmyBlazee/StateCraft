import codecs

path = r'C:\Users\Tobi\StateCraft\Assets\Scripts\UI\TradeAgreementController.cs'
s = codecs.open(path, 'r', 'utf-8').read()

old_func = '''private void PopulateActiveAgreements()
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
        }'''

new_func = '''private void PopulateActiveAgreements()
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
                    
                    var title = new Label($"{dirText} TO {partner.ToUpper()}");
                    title.style.color = new StyleColor(isSeller ? new Color(0, 0.9f, 0.47f) : new Color(0, 0.82f, 1f));
                    title.style.fontSize = 10;
                    title.style.unityFontStyleAndWeight = FontStyle.Bold;
                    box.Add(title);

                    float localVal = route.monthlyPaymentUSD * player.exchangeRate;
                    string valString = UiFormat.Money(localVal, player.currencySymbol);
                    
                    var stats = new Label($"{route.resource} : {UiFormat.MoneyRaw(route.resourceQuantity, "")} Units\\nValue: {valString}/m | {route.durationMonths} mo. left");
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
        }'''

if old_func in s:
    s = s.replace(old_func, new_func)
    codecs.open(path, 'w', 'utf-8').write(s)
    print('SUCCESS')
else:
    print('FAILED')
