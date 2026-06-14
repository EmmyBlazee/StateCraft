using UnityEngine;

namespace StateCraft.UI
{
    public class UIPanelController : MonoBehaviour
    {
        public GameObject panelRoot;

        public void Open()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        public void Close()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf);
        }
    }
}
