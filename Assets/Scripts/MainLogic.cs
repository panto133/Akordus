using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainLogic : MonoBehaviour
{
    [Header("Panels for displaying information")]
    [SerializeField]
    private GameObject tactPanel;
    [SerializeField]
    private GameObject transposeSongPanel;
    [SerializeField]
    private GameObject warningPanel;
    [SerializeField]
    private GameObject savedSongPanel;
    [SerializeField]
    private GameObject changeTitlePanel;
    [SerializeField]
    private TextMeshProUGUI songTitle;
    [SerializeField]
    private TMP_InputField changeTitleInput;

    [Header("Assigning Scripts")]
    [SerializeField]
    private PDFLogic pdfLogic;
    [SerializeField]
    private CommandsLogic commandsLogic;

    [Header("Assigning buttons and images for tact selection")]
    [SerializeField]
    private GameObject[] chooseTactButtons;
    [SerializeField]
    private Sprite[] selectedTactImages;
    [SerializeField]
    private Sprite[] normalTactImages;
    [SerializeField]
    private Button[] currentlyEditingButtons;

    private int selectedTact = -1;


    /// <summary>
    /// This function is called when clicked on a menu button called Kreiraj pesmu
    /// </summary>
    public void CreateNewSong()
    {
        tactPanel.SetActive(true);
    }
    /// <summary>
    /// This function is called when user entered song's specification and is ready to compose
    /// </summary>
    public void StartNewSong()
    {
        //For some reason if a string is empty, it will return the Length of 1
        if (songTitle.text.Trim().Length == 1 || selectedTact == -1) return;

        switch(selectedTact+1)
        {
            case 1:
                commandsLogic.CurrentTact = "2|4";
                break;
            case 2:
                commandsLogic.CurrentTact = "3|4";
                break;
            case 3:
                commandsLogic.CurrentTact = "4|4";
                break;
            case 4:
                commandsLogic.CurrentTact = "6|8";
                break;
            case 5:
                commandsLogic.CurrentTact = "7|8";
                break;
            case 6:
                commandsLogic.CurrentTact = "9|8";
                break;

        }
        //Creating pdf and opening the document
        pdfLogic.CreatePDF();
        //Placing the title and tact in Program
        commandsLogic.PlaceTitle(songTitle.text);
        commandsLogic.InsertStartingTact(normalTactImages[selectedTact]);

        //Disabling the tact panel and warning panel and resetting tact selection 
        tactPanel.SetActive(false);
        warningPanel.SetActive(false);
        ResetTacts();
    }
    public void CloseCreateNewSongPopup()
    {
        songTitle.text = "";
        changeTitleInput.text = "";
        ResetTacts();
        tactPanel.SetActive(false);
    }
    public void SaveSong()
    {
        pdfLogic.SavePDF();
        savedSongPanel.SetActive(true);
        //TODO smisli kako da se u programu pisu oni akordi i kako ce se posle sacuvati
    }
    /// <summary>
    /// Function which is called when user presses X on the popup for saving the song
    /// </summary>
    public void CloseSaveSongPopup()
    {
        savedSongPanel.SetActive(false);
    }
    public void CloseTransposePanel()
    {
        transposeSongPanel.SetActive(false);
    }
    public void OpenTransposePanel()
    {
        transposeSongPanel.SetActive(true);
    }
    public void OpenChangeTitlePanel()
    {
        changeTitlePanel.SetActive(true);
    }
    public void SaveCloseChangeTitlePanel()
    {
        songTitle.text = changeTitleInput.text;
       
        commandsLogic.PlaceTitle(songTitle.text);
        changeTitlePanel.SetActive(false);
    }
    /// <summary>
    /// This function is called when the user chooses a tact from the panel Creating New Song. selectedTact is saved so
    /// that it can be passed on the function InsertStartingTact in commandsLogic so the appropriate tact can be inserted.
    /// </summary>
    /// <param name="i"></param>
    public void ChooseTact(int i)
    {
        ResetTacts();

        //Display appropriate graphics for selected tact
        chooseTactButtons[i - 1].GetComponent<Image>().sprite = selectedTactImages[i-1];
        selectedTact = i-1;
    }
    //Function which resets the graphic of tacts in the selection panel
    public void ResetTacts()
    {
        //Reset the graphics
        for (int j = 0; j < 6; j++)
        {
            chooseTactButtons[j].GetComponent<Image>().sprite = normalTactImages[j];
        }
    }
    public void QuitProgram()
    {
        Application.Quit();
    }
    public void ChangeActiveButton(string currentlyEditing)
    {
        currentlyEditingButtons[0].GetComponent<Image>().color = new Color(176f / 255f, 255f / 255f, 148f / 255f, 1f);
        currentlyEditingButtons[1].GetComponent<Image>().color = new Color(176f / 255f, 255f / 255f, 148f / 255f, 1f);
        currentlyEditingButtons[2].GetComponent<Image>().color = new Color(176f / 255f, 255f / 255f, 148f / 255f, 1f);

        switch (currentlyEditing)
        {
            case "forspil":
                currentlyEditingButtons[0].GetComponent<Image>().color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);
                break;
            case "strofa":
                currentlyEditingButtons[1].GetComponent<Image>().color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);
                break;
            case "refren":
                currentlyEditingButtons[2].GetComponent<Image>().color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);
                break;
        }
    }
}
