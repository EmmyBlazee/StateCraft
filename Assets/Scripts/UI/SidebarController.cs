using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace StateCraft.UI
{
    public class SidebarController : MonoBehaviour
    {
        [Header("Tab Panels")]
        public GameObject globalPanel;
        public GameObject militaryPanel;
        public GameObject intelPanel;
        public GameObject economyPanel;

        [Header("Drawer Container")]
        public GameObject drawerRoot;

        private List<GameObject> allPanels = new List<GameObject>();

        private void Start()
        {
            // Register all panels
            if (globalPanel) allPanels.Add(globalPanel);
            if (militaryPanel) allPanels.Add(militaryPanel);
            if (intelPanel) allPanels.Add(intelPanel);
            if (economyPanel) allPanels.Add(economyPanel);

            // Hide everything at start
            CloseDrawer();
        }

        public void OpenGlobalTab() => ShowTab(globalPanel);
        public void OpenMilitaryTab() => ShowTab(militaryPanel);
        public void OpenIntelTab() => ShowTab(intelPanel);
        public void OpenEconomyTab() => ShowTab(economyPanel);

        private void ShowTab(GameObject targetPanel)
        {
            if (targetPanel == null) return;

            // If the panel is already open, close the drawer (Toggle behavior)
            if (targetPanel.activeSelf)
            {
                CloseDrawer();
                return;
            }

            // Open the drawer
            drawerRoot.SetActive(true);

            // Hide all other panels first
            foreach (var panel in allPanels)
            {
                panel.SetActive(false);
            }

            // Show the target panel
            targetPanel.SetActive(true);
            
}

        public void CloseDrawer()
        {
            drawerRoot.SetActive(false);
            foreach (var panel in allPanels)
            {
                panel.SetActive(false);
            }
        }
    }
}
