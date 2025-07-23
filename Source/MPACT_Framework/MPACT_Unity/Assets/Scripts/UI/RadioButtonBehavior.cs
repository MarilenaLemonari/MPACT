using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;
using UnityEngine.UI;

public class RadioButtonBehavior : MonoBehaviour
{
    [SerializeField] private Button manualButton;
    [SerializeField] private Button prefabButton;
    private Button lastSelected;
    [SerializeField] private DraggablePoint goalPoint;
    [SerializeField] private DraggablePoint groupPoint;
    [SerializeField] private DraggablePoint interactPoint;
    [SerializeField] private Slider connectivitySlider;
    [SerializeField] private TriangleUI triangle;
    [SerializeField] private GameObject m_datasetPointsParent;

    private void Start()
    {
        // Add listeners to the buttons
        manualButton.onClick.AddListener(() => SelectButton(manualButton));
        prefabButton.onClick.AddListener(() => SelectButton(prefabButton));

        // Default to the first button being selected
        SelectButton(manualButton);
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

        if (lastSelected == prefabButton)
        {
            goalPoint.SetActivty(false);
            groupPoint.SetActivty(false);
            interactPoint.SetActivty(false);
            connectivitySlider.interactable = false;
            triangle.enabled = false;
            m_datasetPointsParent.SetActive(true);
        }
        else
        {
            goalPoint.SetActivty(true);
            groupPoint.SetActivty(true);
            interactPoint.SetActivty(true);
            connectivitySlider.interactable = true;
            ProfileManager.Instance.SetCurrentProfile(goalPoint.Value, groupPoint.Value, interactPoint.Value, connectivitySlider.value);
            triangle.enabled = true;
            m_datasetPointsParent.SetActive(false);
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
