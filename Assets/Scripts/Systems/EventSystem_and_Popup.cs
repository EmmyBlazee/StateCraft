
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Data;
using StateCraft.Systems;
using System;

namespace StateCraft.UI
{
    public class EventChoicePopupController : MonoBehaviour
    {
        private static EventChoicePopupController _instance;

        private UIDocument   _uiDoc;
        private VisualElement _root;

        private Label   _titleLabel;
        private Label   _descLabel;
        private Label   _optionALabel;
        private Label   _optionAConsequence;
        private Label   _optionBLabel;
        private Label   _optionBConsequence;
        private Button  _btnChooseA;
        private Button  _btnChooseB;

        private Action  _onChoiceA;
        private Action  _onChoiceB;

        private void Awake()
        {
            _instance = this;
            _uiDoc    = GetComponent<UIDocument>();
            _root     = _uiDoc?.rootVisualElement?.Q<VisualElement>("EventChoicePopup");
            if (_root == null) { Debug.LogError("[EventChoicePopup] Root element 'EventChoicePopup' not found."); return; }

            _titleLabel         = _root.Q<Label>("EventTitle");
            _descLabel          = _root.Q<Label>("EventDescription");
            _optionALabel       = _root.Q<Label>("OptionAText");
            _optionAConsequence = _root.Q<Label>("OptionAConsequence");
            _optionBLabel       = _root.Q<Label>("OptionBText");
            _optionBConsequence = _root.Q<Label>("OptionBConsequence");
            _btnChooseA         = _root.Q<Button>("BtnChooseA");
            _btnChooseB         = _root.Q<Button>("BtnChooseB");

            _btnChooseA.clicked += () => { Hide(); _onChoiceA?.Invoke(); };
            _btnChooseB.clicked += () => { Hide(); _onChoiceB?.Invoke(); };

            Hide();
        }

        public static void Show(GameEventSO ev, CountryDataSO country, Action onChoiceA, Action onChoiceB)
        {
            if (_instance == null)
            {
                Debug.LogError("[EventChoicePopup] No instance in scene — event skipped to prevent silent consequences.");
                return;
            }
            _instance.ShowInternal(ev, country, onChoiceA, onChoiceB);
        }

        private void ShowInternal(GameEventSO ev, CountryDataSO country, Action onChoiceA, Action onChoiceB)
        {
            _onChoiceA = onChoiceA;
            _onChoiceB = onChoiceB;

            if (_titleLabel != null)         _titleLabel.text         = ev.eventTitle;
            if (_descLabel  != null)         _descLabel.text          = ev.description;
            if (_optionALabel != null)       _optionALabel.text       = ev.optionAText;
            if (_optionBLabel != null)       _optionBLabel.text       = ev.optionBText;
            if (_optionAConsequence != null) _optionAConsequence.text = BuildConsequenceText(
                ev.optionACost, ev.optionAStability, ev.optionAGDP, ev.optionAIntel, ev.optionASecurity);
            if (_optionBConsequence != null) _optionBConsequence.text = BuildConsequenceText(
                ev.optionBCost, ev.optionBStability, ev.optionBGDP, ev.optionBIntel, ev.optionBSecurity);

            _root.style.display = DisplayStyle.Flex;
        }

        private void Hide() => _root.style.display = DisplayStyle.None;

        private static string BuildConsequenceText(float cost, float stab, float gdp, float intel, float sec)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (cost  != 0f) parts.Add($"${Mathf.Abs(cost):F1}B {(cost > 0 ? "gained" : "cost")}");
            if (stab  != 0f) parts.Add($"Stability {(stab  > 0 ? "+" : "")}{stab:F0}");
            if (gdp   != 0f) parts.Add($"GDP {(gdp   > 0 ? "+" : "")}{gdp * 100f:F1}%");
            if (intel != 0f) parts.Add($"Intel {(intel > 0 ? "+" : "")}{intel:F0}");
            if (sec   != 0f) parts.Add($"Security {(sec > 0 ? "+" : "")}{sec:F0}");
            return parts.Count > 0 ? string.Join("  ·  ", parts) : "No immediate effect";
        }
    }
}
