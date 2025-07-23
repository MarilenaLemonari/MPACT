using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundScaler : MonoBehaviour
{
    [SerializeField] private Camera m_camera;

    private void Update()
    {
        Vector3 scaling = new Vector3(m_camera.orthographicSize * 8f, m_camera.orthographicSize * 8f, 1f);
        transform.localScale = scaling;
    }
}
