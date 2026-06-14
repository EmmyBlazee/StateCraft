// ============================================================
// FILE: GameManager_IntegrationPatch.cs
// PURPOSE: Summary of ALL changes needed to wire up the 6 new
//          features. Apply these diffs to your existing files.
// ============================================================

// ════════════════════════════════════════════════════════════
// 1. GameManager.cs CHANGES
// ════════════════════════════════════════════════════════════
/*
// ── ADD THESE PUBLIC PROPERTIES (with the other system properties) ──

public RelationsMatrix  Relations      { get; private set; } = new RelationsMatrix();
public EconomyLedger    Ledger         { get; private set; } = new EconomyLedger();
public VictorySystem    VictorySystem  { get; private set; }

// ── IN InitializeSystems(), after TurnSystem = new TurnSystem(): ──

VictorySystem = new VictorySystem();
TurnSystem.OnMonthEnded += () => Relations.SnapshotHistory();

// ── IN ProcessMonth(), inside the player country block, at the END: ──

VictorySystem.CheckVictory(playerCountry, allCountries, TurnSystem.CurrentTurn);
*/

// ════════════════════════════════════════════════════════════
// 2. DiplomacySystem.cs — UpdateRelations() body + hook calls
// ════════════════════════════════════════════════════════════
/*
// UpdateRelations() — the old stub can stay empty;
// Relations are now adjusted at the point of each action.

// In SignTradeDeal(), after ActiveDeals.Add(deal): ──────────
var gm2 = GameManager.Instance;
if (gm2?.Relations != null)
{
    int month2 = gm2.TurnSystem?.CurrentTurn ?? 0;
    gm2.Relations.Adjust(seller.countryName, buyer.countryName, +10f,
        "Trade agreement signed", month2);
}

// In FundProxyInsurgency(), inside the "EXPOSED!" branch: ───
var gm3 = GameManager.Instance;
if (gm3?.Relations != null)
{
    int month3 = gm3.TurnSystem?.CurrentTurn ?? 0;
    gm3.Relations.Adjust(sponsor.countryName, target.countryName, -25f,
        "Caught funding insurgents", month3);
}
*/

// ════════════════════════════════════════════════════════════
// 3. WarSystem.cs — DeclareWar() relation hook
// ════════════════════════════════════════════════════════════
/*
// In DeclareWar(), after the war key is added to activeWars: ─
var gm4 = GameManager.Instance;
if (gm4?.Relations != null)
{
    int month4 = gm4.TurnSystem?.CurrentTurn ?? 0;
    gm4.Relations.Adjust(aggressor.countryName, defender.countryName, -40f,
        $"{aggressor.countryName} declared war", month4);
}

// Replace EndWar() with the version in WarSystem_EndWar_Patch.cs
*/

// ════════════════════════════════════════════════════════════
// 4. DiplomacySystem.cs — Ultimatum response for player
// ════════════════════════════════════════════════════════════
/*
// Add this public method to DiplomacySystem:

public void RespondToUltimatum(CountryDataSO player, CountryDataSO sender, bool comply)
{
    var gm = GameManager.Instance;
    if (gm?.Relations == null) return;
    var rel = gm.Relations.GetOrCreate(player.countryName, sender.countryName);
    int currentMonth = gm.TurnSystem?.CurrentTurn ?? 0;

    if (!rel.hasUltimatum || rel.ultimatumFromCountry != sender.countryName) return;

    rel.ultimatumResolved = true;
    rel.hasUltimatum      = false;

    if (comply)
    {
        // Player accepts terms — avoids war, pays prestige
        gm.Relations.Adjust(player.countryName, sender.countryName, +15f,
            "Ultimatum accepted — tensions defused", currentMonth);
        VictorySystem.AwardPrestige(player, -5f, $"Conceded to {sender.countryName} ultimatum");
        NotificationManager.Instance?.AddNotification(
            $"ULTIMATUM ACCEPTED: Terms accepted. War with {sender.countryName} averted. " +
            $"Prestige −5.", Color.yellow);
        NewsSystem.Instance?.PublishNews(
            $"DIPLOMACY: {player.countryName} accepts {sender.countryName}'s demands. " +
            $"Crisis defused.");
    }
    else
    {
        // Player refuses — sender may now declare war
        gm.Relations.Adjust(player.countryName, sender.countryName, -20f,
            "Ultimatum refused", currentMonth);
        VictorySystem.AwardPrestige(player, +3f, $"Refused to bow to {sender.countryName}");
        NotificationManager.Instance?.AddNotification(
            $"ULTIMATUM REFUSED: You have rejected {sender.countryName}'s demands. " +
            $"Expect military escalation. Prestige +3.", Color.red);
        NewsSystem.Instance?.PublishNews(
            $"CRISIS: {player.countryName} rejects {sender.countryName}'s ultimatum. " +
            $"War preparations underway.");
    }
}

// Hook up UI call from DiplomacyDashboardController:
// When player selects a country with a pending ultimatum,
// show two buttons: "Accept Terms" and "Refuse Demands".
// On click call: GameManager.Instance.DiplomacySystem.RespondToUltimatum(player, selected, comply: true/false)
*/

// ════════════════════════════════════════════════════════════
// 5. EconomySystem.cs — paste Economy Patch block
// ════════════════════════════════════════════════════════════
// Paste the commented-out block from EconomyLedger_and_FeedbackPanel.cs
// at the bottom of UpdateEconomy(), after the existing Debug.Log line.

// ════════════════════════════════════════════════════════════
// 6. Scene / Prefab wiring
// ════════════════════════════════════════════════════════════
/*
A. Add a new UIDocument to the scene for EventChoicePopup:
   - Create GameObject "EventChoicePopup"
   - Add UIDocument component → assign EventChoicePopup.uxml
   - Add EventChoicePopupController.cs script
   - The popup hides itself at Awake; EventSystem calls Show() when needed.

B. Add EconomyFeedbackPanel.cs to the EconomyDashboard GameObject
   (the one that already has an UIDocument with EconomyDashboard.uxml).
   Add the UXML snippet from the patch into EconomyDashboard.uxml.

C. VictorySystem requires no scene GameObject — it's a pure C# class
   instantiated by GameManager.

D. RelationsMatrix and EconomyLedger are pure C# classes —
   no scene objects needed.

E. In DiplomacyDashboardController, extend the bilateral panel to show:
   - Relation score from gm.Relations.GetScore(player, selected)
   - RelationTier tier label
   - A "ACCEPT ULTIMATUM / REFUSE ULTIMATUM" pair of buttons when
     gm.Relations.GetOrCreate(player, selected).hasUltimatum == true
   Wire buttons to DiplomacySystem.RespondToUltimatum().
*/
