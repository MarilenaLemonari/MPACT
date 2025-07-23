using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Room : MonoBehaviour
{
    [SerializeField] private bool m_isRootRoom;
    private BehaviorProfile m_currentProfile;
    private List<BehaviorProfile> m_profiles;
    private BehaviorProfile[] m_edgeProfiles; //[Top, Bottom, Right, Left]
    [SerializeField] private LayerMask m_objectsLayer;
    private MeshCollider m_collider;
    [SerializeField] private Vector2 m_coordinates;
    [SerializeField] private Image m_profileImage;
    [SerializeField] private Image m_selectedImage;
    private int m_frameIndex = 0;
    private SetEdgeColors m_colorSetter;
    private bool m_isSelected;

    public Vector2 Coordinates
    {
        get => m_coordinates;
        set { m_coordinates = value; }
    }
    
    public bool IsSelected
    {
        get => m_isSelected;
        set
        {
            m_isSelected = !m_isSelected;
            m_selectedImage.enabled = m_isSelected;
        }
    }

    public List<BehaviorProfile> GetProfiles()
    {
        return m_profiles;
    }

    public BehaviorProfile Profile
    {
        get => m_currentProfile;
        set
        {
            BehaviorProfile temp = new BehaviorProfile();
            temp.goal = value.goal;
            temp.group = value.group;
            temp.interaction = value.interaction;
            temp.connection = value.connection;
            m_currentProfile = temp;
        }
    }
    
    public bool IsRoot
    {
        get => m_isRootRoom;
        set
        {
            m_isRootRoom = value;
            if (!m_isRootRoom)
            {
                int size = TimelineSlider.Instance.MaxValue;
                m_profiles = new List<BehaviorProfile>(size);
                for (int i = 0; i < size; i++)
                {
                    m_profiles.Add(new BehaviorProfile(1f, 0f, 0f, 0.25f));
                }
            }
        }
    }

    private void Awake()
    {
        m_collider = GetComponent<MeshCollider>();
        m_currentProfile = new BehaviorProfile();
        m_profiles = new List<BehaviorProfile>();
        m_colorSetter = GetComponent<SetEdgeColors>();
        m_currentProfile = new BehaviorProfile(1f, 0f, 0f, 0.25f);
        m_edgeProfiles = new BehaviorProfile[4];
    }

    private void FixedUpdate()
    {
        UpdateEdgeProfiles();
    }

    public void AppendProfile(BehaviorProfile profile)
    {
        m_profiles.Add(profile);
        if(m_profiles.Count > 1 || m_currentProfile == m_profiles[0]) return;
        m_currentProfile = m_profiles[0];
    }

    public void SetCurrentProfile(int index)
    {
        m_frameIndex = index;
        if (m_frameIndex >= m_profiles.Count) return;
        if (!EnvironmentSetup_modCCP.Instance.m_updateGridProfiles) return;
        m_currentProfile = m_profiles[m_frameIndex];
    }
    
    public void SetProfileAtIndex(int index, BehaviorProfile value)
    {
        BehaviorProfile temp = new BehaviorProfile();
        temp.goal = value.goal;
        temp.group = value.group;
        temp.interaction = value.interaction;
        temp.connection = value.connection;
        m_profiles[index] = temp;
        m_frameIndex = index;
        m_currentProfile = m_profiles[m_frameIndex];
    }

    private void UpdateEdgeProfiles()
    {
        List<BehaviorProfile> profiles = RoomManager.Instance.GetNeighborsProfiles(transform.position);
        for (int i = 0; i < profiles.Count; i++)
        {
            if (profiles[i] == null)
            {
                profiles[i] = m_currentProfile;
                m_edgeProfiles[i] = m_currentProfile;
            }
            else
            {
                m_edgeProfiles[i] = m_currentProfile.LerpProfile(profiles[i], 0.5f);
            }
        }
        
        UpdateColor(profiles);
    }
    
    public BehaviorProfile GetCurrentRoomProfile(Vector3 agentPosition)
    {
        Vector3 relativePosition = agentPosition - transform.position;

        // Remap agent's position within the 10x10 room to 0-1 range
        float u = relativePosition.x / (EnvironmentSetup_modCCP.Instance.scale);
        float v = relativePosition.z / (EnvironmentSetup_modCCP.Instance.scale);

        BehaviorProfile horizontal = null;
        if (u > 0)
            horizontal = m_currentProfile.LerpProfile(m_edgeProfiles[2], u);
        else
            horizontal = m_currentProfile.LerpProfile(m_edgeProfiles[3], Mathf.Abs(u));
        
        BehaviorProfile vertical = null;
        if (v > 0)
            vertical = m_currentProfile.LerpProfile(m_edgeProfiles[0], v);
        else 
            vertical = m_currentProfile.LerpProfile(m_edgeProfiles[1], Mathf.Abs(v));

        // Interpolate vertically between results of horizontal interpolations
        float totalMagnitude = Mathf.Abs(u) + Mathf.Abs(v);
        float horizontalRatio = Mathf.Abs(u) / totalMagnitude;
        float verticalRatio = Mathf.Abs(v) / totalMagnitude;

        // Use the ratios to blend the two behaviors
        BehaviorProfile finalProfile = horizontal.Multiply(horizontalRatio).Add((vertical.Multiply(verticalRatio)));

        return finalProfile;
    }

    
    private void UpdateColor(List<BehaviorProfile> profiles)
    {
        m_colorSetter.UpdateEdgeColors(m_currentProfile, profiles[0], profiles[1], profiles[2], profiles[3]);
    }

    public void ShowHideBehavior(bool show)
    {
        m_profileImage.enabled = show;
    }
    
    public bool DeleteRoom()
    {
        if (IsRoot) return false;

        float maxObjectHeight = 5;
        Vector3 roomPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 colliderSize = m_collider.bounds.size;
        Vector3 realSize = Vector3.Scale(colliderSize, transform.localScale);
        Vector3 halfExtents = new Vector3(realSize.x / 2, maxObjectHeight / 2, realSize.z / 2);
        RaycastHit[] hits = Physics.BoxCastAll(roomPos + new Vector3(0, maxObjectHeight / 2, 0), 
            halfExtents, Vector3.up, Quaternion.identity, 0.1f, m_objectsLayer);

        foreach (RaycastHit hit in hits)
        {
            if (IsWithinBounds(hit.collider.bounds.center, roomPos, halfExtents))
            {
                GameObject obj = hit.collider.gameObject;
                if (obj.GetComponentInParent<modCCPAgent_Framework>() != null)
                {
                    obj.GetComponentInParent<modCCPAgent_Framework>().DeactivateAgent();
                }
                else
                    Destroy(hit.collider.gameObject);   
            }
        }

        return true;
    }
    
    bool IsWithinBounds(Vector3 point, Vector3 boxCenter, Vector3 boxHalfExtents)
    {
        return point.x > (boxCenter.x - boxHalfExtents.x) && point.x < (boxCenter.x + boxHalfExtents.x) &&
               point.z > (boxCenter.z - boxHalfExtents.z) && point.z < (boxCenter.z + boxHalfExtents.z);
    }
}
