using UnityEngine;
using StateCraft.Data;
using StateCraft.UI;

namespace StateCraft.Map
{
    [RequireComponent(typeof(Collider2D))] // Assumes 2D map for now
    public class CountryController : MonoBehaviour
    {
        public CountryDataSO countryData;
        private SpriteRenderer spriteRenderer;
        
        private Color originalColor;
        private Color hoverColor = new Color(0.8f, 0.8f, 1f, 1f);
        public Color selectionColor = Color.yellow;
        private bool isSelected = false;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (countryData != null)
            {
                originalColor = countryData.mapColor;
                if (spriteRenderer != null)
                    spriteRenderer.color = originalColor;
            }

            if (MapVisualsManager.Instance != null)
                MapVisualsManager.Instance.RegisterCountry(this);
        }

        private void Start()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnCountrySelected += HandleGlobalSelection;
            }
        }

        private void OnMouseEnter()
        {
            if (spriteRenderer != null)
                spriteRenderer.color = hoverColor;
        }

        private void OnMouseExit()
        {
            UpdateVisuals();
        }

        private void OnMouseDown()
        {
// Block click if mouse is over UI Toolkit elements
            if (UIStateManager.Instance != null && UIStateManager.Instance.IsPointerOverUI())
            {
return;
            }

            // Removed legacy EventSystem check because UI Toolkit's Event System bridge 
            // falsely flags transparent full-screen layout containers as interactive.
            // We now solely rely on UIStateManager.Instance.IsPointerOverUI() above.

if (MapManager.Instance != null)
                MapManager.Instance.SelectCountry(countryData);
        }

        private void HandleGlobalSelection(CountryDataSO selected)
        {
            isSelected = (selected == countryData);
            UpdateVisuals();
        }

        public void UpdateVisuals()
        {
            if (spriteRenderer == null) return;

            if (isSelected)
            {
                spriteRenderer.color = selectionColor;
                return;
            }

            // Update color based on stability or other metrics
            if (countryData != null)
            {
                // Dynamic coloring: Less stability = more red
                float stabilityFactor = countryData.stability / 100f;
                spriteRenderer.color = Color.Lerp(Color.red, originalColor, stabilityFactor);
            }
        }
    }
}
