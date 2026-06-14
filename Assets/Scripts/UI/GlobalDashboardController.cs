using UnityEngine;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    public class GlobalDashboardController : MonoBehaviour
    {
        public GameObject rowPrefab;      // Drag your Country_Row_Prefab here
        public Transform contentParent;   // Drag the Scroll View's "Content" object here

        private void OnEnable()
        {
            RefreshList();
        }

        public void RefreshList()
        {
            if (GameManager.Instance == null || rowPrefab == null || contentParent == null) return;

            // Clear old list
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            // Create a new row for every country in the game
            foreach (var country in GameManager.Instance.allCountries)
            {
                GameObject newRow = Instantiate(rowPrefab, contentParent);
                
                // Set the data on the row
                CountryRowUI rowUI = newRow.GetComponent<CountryRowUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(country);
                }
            }
        }
    }
}
