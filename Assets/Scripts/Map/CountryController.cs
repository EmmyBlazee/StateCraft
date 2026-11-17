using UnityEngine;
using StateCraft.Data;
using StateCraft.UI;

namespace StateCraft.Map
{
    [RequireComponent(typeof(Collider2D))]
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
            if (UIStateManager.Instance != null && UIStateManager.Instance.IsPointerOverUI())
            {
return;
            }

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

            if (countryData != null)
            {
                float stabilityFactor = countryData.stability / 100f;
                spriteRenderer.color = Color.Lerp(Color.red, originalColor, stabilityFactor);
            }
        }
    }
}
