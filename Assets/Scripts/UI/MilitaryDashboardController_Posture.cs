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
        private void CyclePosture()
        {
            var postures = new string[] { "DEFENSIVE", "DETERRENCE", "OFFENSIVE", "COIN FOCUS" };
            int idx = System.Array.IndexOf(postures, _currentPosture);
            if (idx == -1) idx = 0;
            else idx = (idx + 1) % postures.Length;
            SetPosture(postures[idx]);
        }

        private void SetPosture(string posture)
        {
            _currentPosture = posture;
            var country = GameManager.Instance?.playerCountry;
            if (country == null) return;

            switch (posture)
            {
                case "DEFENSIVE":
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 0.5f);
                    break;
                case "OFFENSIVE":
                    country.militaryReadiness = Mathf.Min(100f, country.militaryReadiness + 1f);
                    break;
                case "COIN FOCUS":
                    foreach (var r in country.regions)
                        if (r.security < 60f) r.security = Mathf.Min(100f, r.security + 1f);
                    break;
            }

            UpdatePostureButtonStyles();
            UpdateDoctrineText(posture);
            NotificationManager.Instance?.AddNotification($"DOCTRINE: {posture} posture enacted.", new Color(0, 210/255f, 1f));
        }

        private void UpdatePostureButtonStyles()
        {
            var btnDef = _root.Q<Button>("BtnPostureDefensive");
            var btnDet = _root.Q<Button>("BtnPostureDeterrence");
            var btnOff = _root.Q<Button>("BtnPostureOffensive");
            var btnCoin = _root.Q<Button>("BtnPostureCOIN");

            if (btnDef == null || btnDet == null || btnOff == null || btnCoin == null) return;

            var buttons = new Dictionary<string, Button>
            {
                { "DEFENSIVE", btnDef },
                { "DETERRENCE", btnDet },
                { "OFFENSIVE", btnOff },
                { "COIN FOCUS", btnCoin }
            };

            foreach (var kvp in buttons)
            {
                if (kvp.Key == _currentPosture)
                {
                    kvp.Value.AddToClassList("posture-active");
                    kvp.Value.RemoveFromClassList("posture-inactive");
                }
                else
                {
                    kvp.Value.AddToClassList("posture-inactive");
                    kvp.Value.RemoveFromClassList("posture-active");
                }
            }
        }

        private void UpdateDoctrineText(string posture)
        {
            if (_doctrineDesc != null)
            {
                switch (posture)
                {
                    case "DEFENSIVE":
                        _doctrineDesc.text = "Territorial integrity focus. Readiness +5% in home regions; lower escalation risk.";
                        break;
                    case "DETERRENCE":
                        _doctrineDesc.text = "Visible patrols and exercises. Neighbours less likely to escalate; +8% defence budget strain.";
                        break;
                    case "OFFENSIVE":
                        _doctrineDesc.text = "Force projection and rapid strikes. RDF effectiveness up; diplomatic tension rises.";
                        break;
                    case "COIN FOCUS":
                        _doctrineDesc.text = "Counter-insurgency prioritized. Internal security ops cheaper; heavy armour readiness −5%.";
                        break;
                }
            }

            if (_postureLabel != null)
                _postureLabel.text = posture == "DETERRENCE" ? "PEACETIME" : posture;

            if (_postureIcon != null)
            {
                if (posture == "DEFENSIVE")
                    _postureIcon.style.unityBackgroundImageTintColor = new Color(0, 230f/255f, 118f/255f);
                else if (posture == "OFFENSIVE")
                    _postureIcon.style.unityBackgroundImageTintColor = new Color(255f/255f, 82f/255f, 82f/255f);
                else if (posture == "DETERRENCE")
                    _postureIcon.style.unityBackgroundImageTintColor = new Color(0.1f, 0.5f, 0.9f);
                else if (posture == "COIN FOCUS")
                    _postureIcon.style.unityBackgroundImageTintColor = new Color(0.9f, 0.6f, 0f);
            }
        }

    }
}
