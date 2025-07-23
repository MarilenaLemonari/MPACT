using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : Singleton<ProfileManager>
{
    private BehaviorProfile m_profile;
    [SerializeField] private Text m_goalText;
    [SerializeField] private Text m_groupText;
    [SerializeField] private Text m_interactText;
    [SerializeField] private Text m_connectivityText;
    private Dictionary<int, BehaviorProfile[]> m_timelineProfiles;

    public override void Awake()
    {
        m_profile = new BehaviorProfile();
        base.Awake();
    }

    public BehaviorProfile GetCurrentProfile() { return m_profile; }

    public void SetTimelineProfilesList(Dictionary<int, BehaviorProfile[]> list, int width, int height)
    {
        m_timelineProfiles = list;
        List<Room> rootRooms = RoomManager.Instance.GetRootRooms();
        foreach (var kvp in m_timelineProfiles)
        {
            BehaviorProfile[] frameProfiles = kvp.Value;
            int profileIndex = 0;
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    foreach (var room in rootRooms)
                    {
                        if(room.Coordinates == new Vector2(z ,x))
                            room.AppendProfile(frameProfiles[profileIndex]);
                    }
                    profileIndex += 1;
                }
            }
        }
    }

    public void BuildTimeline()
    {
        TimelineSlider.Instance.InitializeRuler(m_timelineProfiles.Count);
    }

    public void SetCurrentProfile(BehaviorProfile profile)
    {
        m_profile.goal = profile.goal;
        m_profile.group = profile.group;
        m_profile.interaction = profile.interaction;
        m_profile.connection = profile.connection;
        UpdateInfo();
    }
    
    public void SetCurrentProfile(float goal, float group, float interact, float connectivity)
    {
        m_profile.goal = goal;
        m_profile.group = group;
        m_profile.interaction = interact;
        m_profile.connection = connectivity;
        UpdateInfo();
    }

    public void SetCurrentProfilePerType(BehaviorProfile.Type type, float value)
    {
        if (type == BehaviorProfile.Type.Goal) m_profile.goal = value;
        else if (type == BehaviorProfile.Type.Group) m_profile.group = value;
        else if (type == BehaviorProfile.Type.Interact) m_profile.interaction = value;
        else if (type == BehaviorProfile.Type.Connectivity) m_profile.connection = value;
        UpdateInfo();
    }

    public void SetProfileToSelectedRooms()
    {
        int index = TimelineSlider.Instance.Value;
        List<Room> selectedRooms = RoomManager.Instance.GetSelectedRooms();
        foreach (var r in selectedRooms)
        {
            r.SetProfileAtIndex(index, m_profile);
            r.IsSelected = false;
        }

    }
    
    public void UpdateConnectivity(float value)
    {
        m_profile.connection = value;
        UpdateInfo();
    }

    public void UpdateGoal(float value)
    {
        m_profile.goal = value;
        UpdateInfo();
    }
    
    public void UpdateGroup(float value)
    {
        m_profile.group = value;
        UpdateInfo();
    }
    
    public void UpdateInteract(float value)
    {
        m_profile.interaction = value;
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        m_goalText.text = m_profile.goal.ToString("F2");;
        m_groupText.text = m_profile.group.ToString("F2");;
        m_interactText.text = m_profile.interaction.ToString("F2");;
        m_connectivityText.text = m_profile.connection.ToString("F2");;
    }
}
