using UnityEngine;
using UnityEngine.UI;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 40.0f;
    public float zoomSpeed = 15.0f;
    public float minZoom = 1.0f;
    public float maxZoom = 100.0f;
    private Camera m_camera;

    [SerializeField] public Dropdown m_framesDropdown;
    [SerializeField] public Dropdown m_profilesDropdown;
    private bool m_canZoom = true;

    private void Awake()
    {
        m_camera = GetComponent<Camera>();
        if (m_framesDropdown != null && m_profilesDropdown != null)
        {
            m_framesDropdown.onValueChanged.AddListener(delegate { m_canZoom = true; });
            m_profilesDropdown.onValueChanged.AddListener(delegate { m_canZoom = true; });
        }
    }

    void Update()
    {
        if (m_framesDropdown != null && m_profilesDropdown != null)
        {
            if (m_framesDropdown.transform.childCount > 4 || m_profilesDropdown.transform.childCount > 4)
                m_canZoom = false;
            else
                m_canZoom = true;
        }

        // Move the camera with WASD keys
        float moveHorizontal = Input.GetAxisRaw("Horizontal");
        float moveVertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
        transform.Translate(movement * Time.unscaledDeltaTime * moveSpeed, Space.World);

        if (m_canZoom)
        {
            // Zoom in/out with scroll wheel
            float scrollData = Input.GetAxis("Mouse ScrollWheel");
            float zoomDestination = m_camera.orthographicSize - scrollData * zoomSpeed;

            // Make sure the camera doesn't zoom out beyond the set limits
            m_camera.orthographicSize = Mathf.Clamp(zoomDestination, minZoom, maxZoom);
        }
    }
}
