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
            if (globalPanel) allPanels.Add(globalPanel);
            if (militaryPanel) allPanels.Add(militaryPanel);
            if (intelPanel) allPanels.Add(intelPanel);
            if (economyPanel) allPanels.Add(economyPanel);

            CloseDrawer();
        }

        public void OpenGlobalTab() => ShowTab(globalPanel);
        public void OpenMilitaryTab() => ShowTab(militaryPanel);
        public void OpenIntelTab() => ShowTab(intelPanel);
        public void OpenEconomyTab() => ShowTab(economyPanel);

        private void ShowTab(GameObject targetPanel)
        {
            if (targetPanel == null) return;

            if (targetPanel.activeSelf)
            {
                CloseDrawer();
                return;
            }

            drawerRoot.SetActive(true);

            foreach (var panel in allPanels)
            {
                panel.SetActive(false);
            }

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
