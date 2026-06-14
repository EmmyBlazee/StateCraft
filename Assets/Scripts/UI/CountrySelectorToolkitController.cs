using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;
using StateCraft.Map;

namespace StateCraft.UI
{
    public class CountrySelectorToolkitController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _selectorRoot;
        private ScrollView _countryList;
        private TextField _searchInput;
        private Button _btnClose;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null) return;
            _uiDocument.sortingOrder = 100; // Force to front to beat MainDashboard
            _root = _uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void Start()
        {
            RefreshList("");
            // Start hidden but initialized
            _selectorRoot.style.opacity = 0;
            _selectorRoot.pickingMode = PickingMode.Ignore;
        }

        private void InitializeElements()
        {
            if (_root == null) return;
            
            _selectorRoot = _root.Q<VisualElement>("SelectorRoot");
            
            // FALLBACK: If not in local root, check all UIDocuments (Global Search)
            if (_selectorRoot == null)
            {
                var allDocs = FindObjectsOfType<UIDocument>();
                foreach (var doc in allDocs)
                {
                    var found = doc.rootVisualElement?.Q<VisualElement>("SelectorRoot");
                    if (found != null)
                    {
                        _selectorRoot = found;
                        _root = doc.rootVisualElement; // Switch context to the successful one
                        break;
                    }
                }
            }

            if (_selectorRoot != null)
            {
                _countryList = _selectorRoot.Q<ScrollView>("CountryList");
                _searchInput = _selectorRoot.Q<TextField>("SearchInput");
                _btnClose = _selectorRoot.Q<Button>("CloseSelector");

                if (_btnClose != null) _btnClose.clicked += Hide;
                
                if (_searchInput != null)
                {
                    _searchInput.RegisterValueChangedCallback(evt => RefreshList(evt.newValue));
                }
            }
            else
            {
                Debug.LogWarning("[UI] CountrySelector: Could not find 'SelectorRoot' in any UIDocument.");
            }
        }

        public void Show()
        {
            if (_selectorRoot == null) return;
            
            _selectorRoot.style.opacity = 1;
            _selectorRoot.pickingMode = PickingMode.Position; // BLOCK MAP
            _selectorRoot.style.visibility = Visibility.Visible;
            
            if (_searchInput != null) _searchInput.Focus();
            RefreshList("");
            
}

        public void Hide()
        {
            if (_selectorRoot == null) return;
            
            _selectorRoot.style.opacity = 0;
            _selectorRoot.pickingMode = PickingMode.Ignore; // UNBLOCK MAP
            _selectorRoot.style.visibility = Visibility.Hidden;
            
}

        private void RefreshList(string filter)
        {
            if (_countryList == null) return;
            _countryList.Clear();

            if (GameManager.Instance == null || GameManager.Instance.allCountries == null) return;

            foreach (var country in GameManager.Instance.allCountries)
            {
                if (string.IsNullOrEmpty(filter) || country.countryName.ToLower().Contains(filter.ToLower()))
                {
                    _countryList.Add(CreateCountryRow(country));
                }
            }
        }

        private VisualElement CreateCountryRow(CountryDataSO country)
        {
            // Create a button as the container for maximum reliability
            var row = new Button();
            row.name = $"Row_{country.countryName}";
            row.AddToClassList("selector-row");
            
            // Layout
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.paddingLeft = 15;
            row.style.paddingRight = 15;
            row.style.height = 50;
            row.style.backgroundColor = new Color(1f, 1f, 1f, 0.03f);
            
            // Borders
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(1f, 1f, 1f, 0.05f);
            row.style.borderTopWidth = 0;
            row.style.borderLeftWidth = 0;
            row.style.borderRightWidth = 0;
            
            // Input Mode
            row.pickingMode = PickingMode.Position;

            // Flag
            var flag = new Image();
            flag.style.width = 30;
            flag.style.height = 20;
            flag.style.marginRight = 15;
            flag.pickingMode = PickingMode.Ignore;
            if (country.countryFlag != null) flag.sprite = country.countryFlag;
            row.Add(flag);

            // Name
            var name = new Label(country.countryName.ToUpper());
            name.style.color = Color.white;
            name.style.fontSize = 14;
            name.pickingMode = PickingMode.Ignore;
            row.Add(name);

            // Interaction - Using standard Button click for reliability
            row.clicked += () => {
if (MapManager.Instance != null) MapManager.Instance.SelectCountry(country);
                
                if (GameManager.Instance != null && GameManager.Instance.playerCountry == null)
                {
                    GameManager.Instance.playerCountry = country;
                }
                
                Hide();
            };

            return row;
        }
    }
}
// Force re-import: 2026-05-14 10:14
