using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEngine.UI;

public class RadioButtonMode : MonoBehaviour
{
    [SerializeField] private Button buildButton;
    [SerializeField] private Button profilesButton;
    private Button lastSelected;

    private void Start()
    {
        // Add listeners to the buttons
        buildButton.onClick.AddListener(() => SelectButton(buildButton));
        profilesButton.onClick.AddListener(() => SelectButton(profilesButton));

        // Default to the first button being selected
        SelectButton(buildButton);
    }

    void SelectButton(Button selected)
    {
        // Deselect the last selected button
        if (lastSelected)
            DeselectButton(lastSelected);
    
        // Change the button's background color to black
        selected.GetComponent<Image>().color = Color.black;
    
        // Change the button's text color to white
        Text buttonText = selected.GetComponentInChildren<Text>();
        if (buttonText)
            buttonText.color = Color.white;

        // Remember the selected button
        lastSelected = selected;

        if (lastSelected == profilesButton)
        {
            RoomManager.Instance.EnableDisableBuildMode(false);
        }
        else
        {
            RoomManager.Instance.EnableDisableBuildMode(true);
            List<Room> selectedRooms = RoomManager.Instance.GetSelectedRooms();
            foreach (var r in selectedRooms)
            {
                r.IsSelected = false;
            }
        }
    }

    void DeselectButton(Button btn)
    {
        // Reset the button's background color
        btn.GetComponent<Image>().color = Color.white;  // Or another default color

        // Reset the button's text color
        Text buttonText = btn.GetComponentInChildren<Text>();
        if (buttonText)
            buttonText.color = Color.black;  // Or another default color
    }
}
