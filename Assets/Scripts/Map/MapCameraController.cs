using UnityEngine;
using StateCraft.UI;

namespace StateCraft.Map
{
    public class MapCameraController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 10f;
        public float panMargin = 20f;

        [Header("Zoom")]
        public float zoomSpeed = 5f;
        public float minZoom = 2f;
        public float maxZoom = 40f;

        private Vector3 dragOrigin;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            if (cam != null)
                cam.orthographicSize = 25f;
        }

        private void Update()
        {
            bool overUI = UIStateManager.Instance != null && UIStateManager.Instance.IsPointerOverUI();
            if (overUI) return;

            bool mouseOverDossier = Input.mousePosition.x > Screen.width - 420f;
            
            HandleMovement();
            if (!mouseOverDossier) HandleZoom();
            HandleMouseDrag();
        }

        private void HandleMouseDrag()
        {
            if (Input.GetMouseButtonDown(1))
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(1))
            {
                Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                Vector3 newPos = transform.position + difference;
                newPos.z = transform.position.z;
                transform.position = newPos;
            }
        }

        private void HandleMovement()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 dir = new Vector3(horizontal, vertical, 0);
            transform.position += dir * moveSpeed * Time.deltaTime * (cam.orthographicSize / 10f);
        }

        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
            }
        }
    }
}
