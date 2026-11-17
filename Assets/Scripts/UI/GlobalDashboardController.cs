using UnityEngine;
using System.Collections.Generic;
using StateCraft.Core;
using StateCraft.Data;

namespace StateCraft.UI
{
    public class GlobalDashboardController : MonoBehaviour
    {
        public GameObject rowPrefab;
        public Transform contentParent;

        private void OnEnable()
        {
            RefreshList();
        }

        public void RefreshList()
        {
            if (GameManager.Instance == null || rowPrefab == null || contentParent == null) return;

            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            foreach (var country in GameManager.Instance.allCountries)
            {
                GameObject newRow = Instantiate(rowPrefab, contentParent);
                
                CountryRowUI rowUI = newRow.GetComponent<CountryRowUI>();
                if (rowUI != null)
                {
                    rowUI.Setup(country);
                }
            }
        }
    }
}
