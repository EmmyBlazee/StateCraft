
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Systems;

namespace StateCraft.UI
{
    public class MilitaryInboxController : MonoBehaviour
    {
        [Header("UXML template for each battle result card")]
        public VisualTreeAsset battleCardTemplate;

        private VisualElement _container;
        private UIDocument    _doc;

        private void Awake()
        {
            _doc = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _doc?.rootVisualElement?.schedule
                .Execute(Init)
                .StartingIn(50);
        }

        private void Init()
        {
            if (_doc == null) return;
            _container = _doc.rootVisualElement.Q<VisualElement>("BattleInboxContainer");
            if (_container == null)
            {
                Debug.LogWarning("[MilitaryInbox] 'BattleInboxContainer' not found in UXML.");
                return;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_container == null || battleCardTemplate == null) return;

            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            _container.Clear();

            var results = country.battleResults;
            if (results == null || results.Count == 0)
            {
                var empty = new Label("No battle reports yet.")
                {
                    style =
                    {
                        color     = new StyleColor(new Color(0.5f, 0.5f, 0.5f)),
                        fontSize  = 13,
                        marginTop = 12,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                _container.Add(empty);
                return;
            }

            int limit = Mathf.Min(results.Count, 10);
            for (int i = 0; i < limit; i++)
            {
                var card = BuildBattleCard(results[i]);
                _container.Add(card);
            }
        }

        private VisualElement BuildBattleCard(BattleResultMessage r)
        {
            var root = battleCardTemplate.Instantiate();
            var card = root.Q<VisualElement>("BattleResultCard");

            var badge = card.Q<VisualElement>("OutcomeBadge");
            var outcomeLabel = card.Q<Label>("OutcomeLabel");
            if (badge != null && outcomeLabel != null)
            {
                outcomeLabel.text = r.outcomeLabel.ToUpper();
                badge.RemoveFromClassList("outcome-victory");
                badge.RemoveFromClassList("outcome-defeat");
                badge.RemoveFromClassList("outcome-stalemate");
                badge.AddToClassList(r.outcomeLabel.ToLower() switch
                {
                    "victory"  => "outcome-victory",
                    "defeat"   => "outcome-defeat",
                    _          => "outcome-stalemate"
                });
            }

            SetLabel(card, "EnemyName", $"vs {r.enemyCountryName}");
            SetLabel(card, "DateLabel", r.dateReceived);

            var frontBadge = card.Q<VisualElement>("FrontBadge");
            var frontLabel = card.Q<Label>("FrontLabel");
            if (frontBadge != null && frontLabel != null)
            {
                frontLabel.text = r.frontStatusLabel +
                    (r.frontProgress > 0 ? " ▲" : r.frontProgress < 0 ? " ▼" : " ◼");
                frontBadge.RemoveFromClassList("front-advancing");
                frontBadge.RemoveFromClassList("front-retreating");
                frontBadge.RemoveFromClassList("front-stalemate");
                frontBadge.AddToClassList(r.frontStatusLabel.ToLower() switch
                {
                    "advancing"  => "front-advancing",
                    "retreating" => "front-retreating",
                    _            => "front-stalemate"
                });
            }

            SetLabel(card, "NarrativeLabel", BuildNarrative(r));

            SetLabel(card, "OurInfantry",   FormatLoss(r.ourInfantryLost));
            SetLabel(card, "EnemyInfantry", FormatLoss(r.enemyInfantryLost));
            SetLabel(card, "OurTanks",      FormatLoss(r.ourTanksLost));
            SetLabel(card, "EnemyTanks",    FormatLoss(r.enemyTanksLost));
            SetLabel(card, "OurAPCs",       FormatLoss(r.ourAPCsLost));
            SetLabel(card, "EnemyAPCs",     FormatLoss(r.enemyAPCsLost));
            SetLabel(card, "OurFighters",   FormatLoss(r.ourFightersLost));
            SetLabel(card, "EnemyFighters", FormatLoss(r.enemyFightersLost));
            SetLabel(card, "OurShips",      FormatLoss(r.ourShipsLost));
            SetLabel(card, "EnemyShips",    FormatLoss(r.enemyShipsLost));

            var navyRow = card.Q<VisualElement>("RowNavy");
            if (navyRow != null && r.ourShipsLost == 0 && r.enemyShipsLost == 0)
                navyRow.style.display = DisplayStyle.None;

            SetLabel(card, "KIACount",      r.ourKIA.ToString("N0"));
            SetLabel(card, "WoundedCount",  r.ourWounded.ToString("N0"));
            SetLabel(card, "EnemyKIACount", r.enemyKIA.ToString("N0"));

            SetLabel(card, "AmmoLabel",    $"Ammo: {r.ammoRemaining:F0}");
            SetLabel(card, "FuelLabel",    $"Fuel: {r.fuelRemaining:F0}");
            SetLabel(card, "ExhaustLabel", $"Exhaustion: {r.warExhaustion:F0}%");
            SetLabel(card, "WillLabel",    $"Will to fight: {r.willToFight:F0}%");

            var exhaustEl = card.Q<Label>("ExhaustLabel");
            if (exhaustEl != null)
            {
                if (r.warExhaustion > 70f)
                    exhaustEl.AddToClassList("supply-warn");
                else
                    exhaustEl.RemoveFromClassList("supply-warn");
            }

            if (!r.isRead)
            {
                card.style.borderLeftWidth = 3;
                card.style.borderLeftColor = new StyleColor(new Color(0.9f, 0.75f, 0.25f));
                r.isRead = true;
            }

            return root;
        }

        private static void SetLabel(VisualElement root, string name, string text)
        {
            var el = root.Q<Label>(name);
            if (el != null) el.text = text;
        }

        private static string FormatLoss(int n)
        {
            if (n <= 0)    return "—";
            if (n >= 1000) return $"{n / 1000.0f:0.#}k";
            return n.ToString();
        }

        private static string BuildNarrative(BattleResultMessage r)
        {
            if (r.weWon && r.frontProgress > 20f)
                return $"Our forces broke through {r.enemyCountryName}'s defensive lines. Momentum is with us.";
            if (r.weWon)
                return $"Our forces repelled the {r.enemyCountryName} advance. The front is holding.";
            if (!r.weWon && r.frontProgress < -20f)
                return $"{r.enemyCountryName} pushed us back. Reinforcements have been committed.";
            if (!r.weWon)
                return $"We absorbed heavy casualties against {r.enemyCountryName}. Tactical withdrawal underway.";
            return $"Neither side gained decisive ground against {r.enemyCountryName}. Artillery exchanges continue.";
        }
    }
}
