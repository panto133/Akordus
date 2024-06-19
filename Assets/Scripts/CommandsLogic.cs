using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using iTextSharp.text;
using System;
using Image = UnityEngine.UI.Image;
using System.Linq;
public class CommandsLogic : MonoBehaviour
{
    //Structure used for populating Dictionary via inspector and inserting accords in the code
    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public Sprite value;
    }

    //Enum for knowing which images need to be which size
    public enum ImageType
    {
        boxSize = 1,
        moreWidth = 2,
        divider = 3,
        repeating = 4
    };

    //Variables for moving knowing where the "pointer" is. 
    private int currentRow;
    private float currentX;
    private float currentY;

    //These variables are needed to remember the previous image of each section 
    private float previousForspilImageSize = 75;
    private float previousStrofaImageSize = 75;
    private float previousRefrenImageSize = 75;


    //Variables for knowing where is the position to continue writing accords in certain sections
    private string currentlyEditing;
    [HideInInspector]
    public string CurrentlyEditing
    {
        get {  return currentlyEditing; }
        set 
        { 
            currentlyEditing = value;
            mainLogic.ChangeActiveButton(value);
        }
    }
    private string currentlyEditingBeforeRewritten;
    private Vector2 forspilEndedPosition;
    private Vector2 strofaEndedPosition = new Vector2(0,0);
    private Vector2 refrenEndedPosition = new Vector2(0,0);

    //Variables for concluding where to place the divider
    private float maxAccords;
    private float forspilAccordStep;
    private float strofaAccordStep;
    private float refrenAccordStep;
    private string currentTact;

    //Lists for storing accords in the correct order
    private List<Image> forspilEnteredAccords = new List<Image>();
    private List<Image> strofaEnteredAccords = new List<Image>();
    private List<Image> refrenEnteredAccords = new List<Image>();
    private Sprite[] sectionSprites;
    private List<int> primaImagePairs = new List<int>();
    private List<int> secundaImagePairs = new List<int>();

    [HideInInspector]
    public string CurrentTact {
        get { return currentTact; } 
        set 
        {
            currentTact = value;
            int down = Convert.ToInt32(currentTact.Substring(2, 1));
            int up = Convert.ToInt32(currentTact.Substring(0, 1));
            //Setting up steps for calculating where to place divider
            //If down number is 4 then max accord allowed is the upper number
            if (down == 4)
            {
                maxAccords = up;
                forspilAccordStep = up;
                strofaAccordStep = up;
                refrenAccordStep = up;
            }
            //Else if the down number is 8 then max accord is 2 if the upper number is 6 or 3 if the upper numbers are 7 or 9
            else
            {
                if(up == 6)
                {
                    maxAccords = 2;
                    forspilAccordStep = 2;
                    strofaAccordStep = 2;
                    refrenAccordStep = 2;
                }
                else
                {
                    maxAccords = 3;
                    forspilAccordStep = 3;
                    strofaAccordStep = 3;
                    refrenAccordStep = 3;
                }
            }
        } 
    }

    [Header("Size Adjustments")]
    [SerializeField]
    private Vector2 accordSize = new Vector2(75,75);
    [SerializeField]
    private Vector2 dividerSize = new Vector2(20, 75);
    [SerializeField]
    private float voltaOffset = .5f;
    [SerializeField]
    private float lineSpacing;
    [SerializeField]
    private float sideMargins;

    //Input field for tracking the commands and songTitle to put based on the selection menu
    [Header("Assigning text fields")]
    [SerializeField]
    private TMP_InputField commandInput;
    [SerializeField]
    private TextMeshProUGUI songTitleMain;
    [SerializeField]
    private TextMeshProUGUI songTitleRender;
    //Texts tied to sections in the song
    [SerializeField]
    private TextMeshProUGUI forspilTextMain;
    [SerializeField]
    private TextMeshProUGUI forspilTextRender;
    [SerializeField]
    private TextMeshProUGUI strofaTextMain;
    [SerializeField]
    private TextMeshProUGUI strofaTextRender;
    [SerializeField]
    private TextMeshProUGUI refrenTextMain;
    [SerializeField]
    private TextMeshProUGUI refrenTextRender;

    [Header("Script References")]
    [SerializeField]
    private MainLogic mainLogic;

    [Header("Display Panel")]
    //Panel in which the accords are shown
    [SerializeField]
    private RectTransform panelTransformMain;
    [SerializeField]
    private RectTransform panelTransformRender;

    //Images for displaying in the panel (not accords but auxillary signs)
    [Header("Auxillary Images")]
    [SerializeField]
    private Sprite dividerImage;
    [SerializeField]
    private Sprite repeatingBegin;
    [SerializeField]
    private Sprite repeatingEnd;
    [SerializeField]
    private Sprite primaShortImage;
    [SerializeField]
    private Sprite primaWideImage;
    [SerializeField]
    private Sprite primaShortFirst;
    [SerializeField]
    private Sprite primaWideFirst;
    [SerializeField]
    private Sprite primaWideSecond;
    [SerializeField]
    private Sprite primaShortSecond;
    [SerializeField]
    private Sprite secundaShortImage;
    [SerializeField]
    private Sprite secundaWideImage;
    [SerializeField]
    private Sprite secundaShortFirst;
    [SerializeField]
    private Sprite secundaWideFirst;

    private Sprite currentTactImage;

    //Prefab to spawn an image in which to place the correct accord
    [Header("Prefabs")]
    [SerializeField]
    private GameObject accordImagePrefab;
    //This is a list for populating dictionary via inspector by placing images as values, and commands as keys
    [Header("Accords Dictionary")]
    [SerializeField]
    private List<KeyValuePair> accordKeyValuePair;
    //Dictionary for accords
    private Dictionary<string, Sprite> accordsDictionary = new Dictionary<string, Sprite>();

    void Start()
    {
        //Adding event on commandsInput button so that it only takes value if ENTER is pressed
        commandInput.onSubmit.AddListener(EnterCommand);

        //Setting the variables to starting positions for positioning the "cursor"
        currentRow = 0;
        currentX = -panelTransformMain.rect.width / 2 + sideMargins;
        currentY = panelTransformMain.rect.height/2 - lineSpacing - forspilTextMain.rectTransform.rect.height;

        //Because dictionary can't be serialized, i use structure KeyValuePair to store accords via inspector and then
        //populate them in the dictionary using this code:
        foreach (var kvp in accordKeyValuePair)
        {
            accordsDictionary[kvp.key] = kvp.value;
        }
        //Setting CurrentlyEditing to forspil because the user will always start editing the song at forspil
        CurrentlyEditing = "forspil";
        currentlyEditingBeforeRewritten = "forspil";
    }
    /// <summary>
    /// Adding image based on the command entered. This function only adds images to PANEL. Parameter "text" is only because the
    /// function cannot be added on a listener for commandInput button.
    /// </summary>
    /// <param name="text"></param>
    public void EnterCommand(string text)
    {
        //Puts focus on the same textbox for continued typing
        commandInput.ActivateInputField();

        //Doesn't allow empty input
        if (text == "") return;

        //Disables listener until this function is finished because it is being called twice for unknown reason
        commandInput.onSubmit.RemoveListener(EnterCommand);
        
        //If the inserted command isn't in the dictionary, return the listener and end function
        if(!accordsDictionary.Keys.Contains(text))
        {
            commandInput.onSubmit.AddListener(EnterCommand);
            return;
        }
        //Else the image will be placed according to the inserted command, If it's a dur accord (only one letter command),
        //then the image will be placed as a box size, else the image will have more width
        if(text.Length == 1)
        {
            PlaceImage(accordsDictionary[text], ImageType.boxSize);
        }
        else
        {
            PlaceImage(accordsDictionary[text], ImageType.moreWidth);
        }
        //Returns the listener for the button
        commandInput.onSubmit.AddListener(EnterCommand);
    }
    /// <summary>
    /// This function is only called from MainLogic when new song is created to insert images of starting tact
    /// </summary>
    /// <param name="tact"></param>
    public void InsertStartingTact(Sprite tactImage)
    {
        PlaceImage(tactImage, ImageType.boxSize);
        currentTactImage = tactImage;
    }
    /// <summary>
    /// This function is only called from MainLogic when new song is created to insert the song title in the panel
    /// </summary>
    /// <param name="title"></param>
    public void PlaceTitle(string title)
    {
        songTitleMain.text = title;
        songTitleRender.text = title;
    }
    /// <summary>
    /// This function is called from buttons Predji na forspil/strofu/refren. It will remember the position where the currently
    /// editing section ended, and after that it will position the "cursor" on the adjacent section.
    /// </summary>
    /// <param name="text"></param>
    public void PlaceSectionTextButton(string text)
    {
        PlaceSectionText(text, false);
    }
    public void DeleteAccord(Image accordImage)
    {
        for (int i = 0; i < forspilEnteredAccords.Count; i++)
        {
            if (forspilEnteredAccords[i].gameObject == accordImage.gameObject)
            {
                forspilEnteredAccords.RemoveAt(i);
                RewriteAllAccords();
                return;
            }
        }
        for (int i = 0; i < strofaEnteredAccords.Count; i++)
        {
            if (strofaEnteredAccords[i].gameObject == accordImage.gameObject)
            {
                strofaEnteredAccords.RemoveAt(i);
                RewriteAllAccords();
                return;
            }
        }
        for (int i = 0; i < refrenEnteredAccords.Count; i++)
        {
            if (refrenEnteredAccords[i].gameObject == accordImage.gameObject)
            {
                refrenEnteredAccords.RemoveAt(i);
                RewriteAllAccords();
                return;
            }
        }

        //NAPRAVI BRISANJE ZA PRIMU I SEKUNDU
    }

    public void InsertRepeatingSigns(Image firstImage, Image secondImage)
    {
        GameObject repeatBeginSign = Instantiate(accordImagePrefab, panelTransformMain);
        GameObject repeatEndSign = Instantiate(accordImagePrefab, panelTransformMain);

        repeatBeginSign.GetComponent<Image>().sprite = repeatingBegin;
        repeatEndSign.GetComponent<Image>().sprite = repeatingEnd;

        for (int i = 0; i < forspilEnteredAccords.Count; i++)
        {
            if (forspilEnteredAccords[i].gameObject == firstImage.gameObject)
            {

                forspilEnteredAccords.Insert(i, repeatBeginSign.GetComponent<Image>());
                i++;
                continue;
            }
            if (forspilEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                forspilEnteredAccords.Insert(i+1, repeatEndSign.GetComponent<Image>());
                break;
            }
        }
        for (int i = 0; i < strofaEnteredAccords.Count; i++)
        {
            if (strofaEnteredAccords[i].gameObject == firstImage.gameObject)
            {
                strofaEnteredAccords.Insert(i, repeatBeginSign.GetComponent<Image>());
                i++;
                continue;
            }
            if (strofaEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                strofaEnteredAccords.Insert(i+1, repeatEndSign.GetComponent<Image>());
                break;
            }
        }
        for (int i = 0; i < refrenEnteredAccords.Count; i++)
        {
            if (refrenEnteredAccords[i].gameObject == firstImage.gameObject)
            {
                refrenEnteredAccords.Insert(i, repeatBeginSign.GetComponent<Image>());
                i++;
                continue;
            }
            if (refrenEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                refrenEnteredAccords.Insert(i+1, repeatEndSign.GetComponent<Image>());
                break;
            }
        }
        RewriteAllAccords();

    }
    private void AddToPrimaSecunda(int beginIndex, int endIndex, bool isPrima, int sectionAccords)
    {
        if (isPrima)
        {
            primaImagePairs.Add(beginIndex);
            primaImagePairs.Add(endIndex);
            primaImagePairs.Add(sectionAccords);
        }
        else
        {
            secundaImagePairs.Add(beginIndex);
            secundaImagePairs.Add(endIndex);
            secundaImagePairs.Add(sectionAccords);
        }
    }
    public void TransposeSong(float degree)
    {

    }
    private void CalculateTransposal(string currentAccord, float degree)
    {

    }
    /// <summary>
    /// Function that finds the indices of the begin and end images which are in the prima section. The function returns the list based on the
    /// list where the indices were found so that the placement of primas and secundas will be accurate
    /// </summary>
    /// <param name="beginIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="firstImage"></param>
    /// <param name="secondImage"></param>
    private List<Image> FindIndices(ref int beginIndex, ref int endIndex, Image firstImage, Image secondImage, bool isPrima, bool isRewritting)
    {
        for (int i = 0; i < forspilEnteredAccords.Count; i++)
        {
            if (forspilEnteredAccords[i].gameObject == firstImage.gameObject)
            {
                beginIndex = i;
            }
            if (forspilEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                endIndex = i;

                //This addition is for the pair to know if its for forspil, strofa or refren
                if (!isRewritting) AddToPrimaSecunda(beginIndex, endIndex, isPrima, 0);

                return forspilEnteredAccords;
            }
        }
        for (int i = 0; i < strofaEnteredAccords.Count; i++)
        {
            if (strofaEnteredAccords[i].gameObject == firstImage.gameObject)
            {
                beginIndex = i;
            }
            if (strofaEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                endIndex = i;
                //This addition is for the pair to know if its for forspil, strofa or refren
                if (!isRewritting) AddToPrimaSecunda(beginIndex, endIndex, isPrima, 1);

                return strofaEnteredAccords;
            }
        }
        for (int i = 0; i < refrenEnteredAccords.Count; i++)
        {
            if (refrenEnteredAccords[i].gameObject == firstImage.gameObject)
            {
                beginIndex = i;
            }
            if (refrenEnteredAccords[i].gameObject == secondImage.gameObject)
            {
                endIndex = i;
                //This addition is for the pair to know if its for forspil, strofa or refren
                if (!isRewritting) AddToPrimaSecunda(beginIndex, endIndex, isPrima, 2);

                return refrenEnteredAccords;
            }
        }

        return null;
    }
    /// <summary>
    /// Function called when primas and secundas are rewritten. It correctly finds the section where they are needed to be placed but it
    /// doesn't find them all if the indices are exatcly the same. FIX IT!
    /// </summary>
    /// <param name="beginIndex"></param>
    /// <param name="endIndex"></param>
    /// <param name="isPrima"></param>
    /// <returns></returns>
    private List<Image> FindAccordsFromIndices(int beginIndex, int endIndex, bool isPrima)
    {
        if(isPrima)
        {
            for (int i = 0; i < primaImagePairs.Count; i += 3)
            {
                if (primaImagePairs[i] == beginIndex && primaImagePairs[i+1] == endIndex)
                {
                    //Forspil
                    if (primaImagePairs[i + 2] == 0)
                    {
                        return forspilEnteredAccords;
                    }
                    //Strofa
                    if (primaImagePairs[i + 2] == 1)
                    {
                        return strofaEnteredAccords;
                    }
                    //Refren
                    if (primaImagePairs[i + 2] == 2)
                    {
                        return refrenEnteredAccords;
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < secundaImagePairs.Count; i += 3)
            {
                if (secundaImagePairs[i] == beginIndex && secundaImagePairs[i + 1] == endIndex)
                {
                    //Forspil
                    if (secundaImagePairs[i + 2] == 0)
                    {
                        return forspilEnteredAccords;
                    }
                    //Strofa
                    if (secundaImagePairs[i + 2] == 1)
                    {
                        return strofaEnteredAccords;
                    }
                    //Refren
                    if (secundaImagePairs[i + 2] == 2)
                    {
                        return refrenEnteredAccords;
                    }
                }
            }
        }
        return forspilEnteredAccords;
    }
    public void InsertPrima(Image firstImage, Image secondImage, bool isRewritting)
    {
        /*Kalkulisanje sirine prime radi, potrebno je ubaciti da bude neka lista gde ce se pamtiti i te prime i sekunde*/
        int beginIndex=-1, endIndex=-1;

        List<Image> accordsList = FindIndices(ref beginIndex, ref endIndex, firstImage, secondImage, true, isRewritting);

        //The check needs to be outside of FindIndices function so that the rest of the code from this function doesn't get executed if the list is null
        if(accordsList == null)
        {
            Debug.LogError("Indices were not found. Terminate inserting prima!");

            return;
        }

        InsertPrimaSecundaSigns(accordsList, beginIndex, endIndex, true);

    }
    //REWRITTING THE SAME VOLTAS WITH SAME INDICES DOESNT WORK
    private void RewritePrima(int beginIndex, int endIndex)
    {
        List<Image> accordsList = FindAccordsFromIndices(beginIndex, endIndex, true);

        InsertPrimaSecundaSigns(accordsList, beginIndex, endIndex, true);
    }
    private void RewriteSecunda(int beginIndex, int endIndex) 
    {
        List<Image> accordsList = FindAccordsFromIndices(beginIndex, endIndex, false);

        InsertPrimaSecundaSigns(accordsList, beginIndex, endIndex, false);
    }
    public void InsertSecunda(Image firstImage, Image secondImage, bool isRewritting)
    {
        int beginIndex = -1, endIndex = -1;

        List<Image> accordsList = FindIndices(ref beginIndex, ref endIndex, firstImage, secondImage, false, isRewritting);

        //The check needs to be outside of FindIndices function so that the rest of the code from this function doesn't get executed if the list is null
        if (accordsList == null)
        {
            Debug.LogError("Indices were not found. Terminate inserting secunda!");

            return;
        }

        InsertPrimaSecundaSigns(accordsList, beginIndex, endIndex, false);
    }
    private bool CheckIfVoltaOneRow(int beginIndex, int endIndex, bool isPrima)
    {
        if(isPrima)
        {
            //Forspil
            if (primaImagePairs[primaImagePairs.Count - 1] == 0)
            {
                if (forspilEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == forspilEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
            //Strofa
            if (primaImagePairs[primaImagePairs.Count - 1] == 1)
            {
                if (strofaEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == strofaEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
            //Refren
            if (primaImagePairs[primaImagePairs.Count - 1] == 2)
            {
                if (refrenEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == refrenEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
        }
        else
        {
            //Forspil
            if (secundaImagePairs[secundaImagePairs.Count - 1] == 0)
            {
                if (forspilEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == forspilEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
            //Strofa
            if (secundaImagePairs[secundaImagePairs.Count - 1] == 1)
            {
                if (strofaEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == strofaEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
            //Refren
            if (secundaImagePairs[secundaImagePairs.Count - 1] == 2)
            {
                if (refrenEnteredAccords[beginIndex].rectTransform.anchoredPosition.y == refrenEnteredAccords[endIndex].rectTransform.anchoredPosition.y)
                    return true;
            }
        }
        return false;
    }
    private void InsertVolta2Rows(List<Image> accordsList, int beginIndex, int endIndex, bool isPrima, bool isShort)
    {
        GameObject voltaShortFirstMain = Instantiate(accordImagePrefab, panelTransformMain);
        GameObject voltaShortFirstRenderer = Instantiate(accordImagePrefab, panelTransformRender);
        GameObject voltaShortSecondMain = Instantiate(accordImagePrefab, panelTransformMain);
        GameObject voltaShortSecondRenderer = Instantiate(accordImagePrefab, panelTransformRender);

        voltaShortFirstMain.tag = "Accord";
        voltaShortSecondMain.tag = "Accord";
        if (isPrima)
        {
            if(isShort)
            {
                voltaShortFirstMain.GetComponent<Image>().sprite = primaShortFirst;
                voltaShortFirstRenderer.GetComponent<Image>().sprite = primaShortFirst;
                voltaShortSecondMain.GetComponent<Image>().sprite = primaShortSecond;
                voltaShortSecondRenderer.GetComponent<Image>().sprite = primaShortSecond;
            }
            else
            {
                voltaShortFirstMain.GetComponent<Image>().sprite = primaWideFirst;
                voltaShortFirstRenderer.GetComponent<Image>().sprite = primaWideFirst;
                voltaShortSecondMain.GetComponent<Image>().sprite = primaWideSecond;
                voltaShortSecondRenderer.GetComponent<Image>().sprite = primaWideSecond;
            }
        }
        else
        {
            if(isShort)
            {
                voltaShortFirstMain.GetComponent<Image>().sprite = secundaShortFirst;
                voltaShortFirstRenderer.GetComponent<Image>().sprite = secundaShortFirst;
                voltaShortSecondMain.GetComponent<Image>().sprite = primaShortSecond;
                voltaShortSecondRenderer.GetComponent<Image>().sprite = primaShortSecond;
            }
            else
            {
                voltaShortFirstMain.GetComponent<Image>().sprite = secundaWideFirst;
                voltaShortFirstRenderer.GetComponent<Image>().sprite = secundaWideFirst;
                voltaShortSecondMain.GetComponent<Image>().sprite = primaWideSecond;
                voltaShortSecondRenderer.GetComponent<Image>().sprite = primaWideSecond;
            }
        }

        voltaShortFirstMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        voltaShortFirstMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        voltaShortSecondMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        voltaShortSecondMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        voltaShortFirstRenderer.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        voltaShortFirstRenderer.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        voltaShortSecondRenderer.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        voltaShortSecondRenderer.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        //Positioning the volta's parts based on the number of accords it covers
        List<Image> firstRowAccords = new List<Image>();
        List<Image> secondRowAccords = new List<Image>();
        float firstRowY = accordsList[beginIndex].rectTransform.position.y;
        for (int i = beginIndex; i <= endIndex; i++)
        {
            if (accordsList[i].rectTransform.position.y == firstRowY)
            {
                firstRowAccords.Add(accordsList[i]);
            }
            else
            {
                secondRowAccords.Add(accordsList[i]);
            }
        }

        voltaShortFirstMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            (firstRowAccords[0].rectTransform.anchoredPosition.x + firstRowAccords[firstRowAccords.Count - 1].rectTransform.anchoredPosition.x) / 2,
            accordsList[beginIndex].rectTransform.anchoredPosition.y + voltaOffset);
        voltaShortSecondMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            (secondRowAccords[0].rectTransform.anchoredPosition.x + secondRowAccords[secondRowAccords.Count - 1].rectTransform.anchoredPosition.x) / 2,
            accordsList[endIndex].rectTransform.anchoredPosition.y + voltaOffset);

        voltaShortFirstRenderer.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            (firstRowAccords[0].rectTransform.anchoredPosition.x + firstRowAccords[firstRowAccords.Count - 1].rectTransform.anchoredPosition.x) / 2,
            firstRowAccords[0].rectTransform.anchoredPosition.y + voltaOffset);
        voltaShortSecondRenderer.GetComponent<RectTransform>().anchoredPosition = new Vector2(
            (secondRowAccords[0].rectTransform.anchoredPosition.x + secondRowAccords[secondRowAccords.Count - 1].rectTransform.anchoredPosition.x) / 2,
            accordsList[endIndex].rectTransform.anchoredPosition.y + voltaOffset);

        float accordWidthsFirst = 0;
        float accordWidthsSecond = 0;
        //Calculating width based on the width of the images:
        for (int i = 0; i < firstRowAccords.Count; i++)
        {
            accordWidthsFirst += firstRowAccords[i].rectTransform.rect.width;
        }
        for (int i = 0; i < secondRowAccords.Count; i++)
        {
            accordWidthsSecond += secondRowAccords[i].rectTransform.rect.width;
        }
        float voltaMultiplierFirst = isShort ? .1f : .05f;
        float voltaMultiplierSecond = isShort ? .1f : .05f;
        float voltaWidthFirst = accordWidthsFirst + firstRowAccords.Count / maxAccords * dividerSize.x;
        float voltaWidthSecond = accordWidthsSecond + secondRowAccords.Count / maxAccords * dividerSize.x;

        //If the width of one row is bigger, then that row should be the same height as the row with the smaller width because of the scaling
        if(voltaWidthFirst < voltaWidthSecond)
        {
            //voltaMultiplierSecond = voltaMultiplierFirst
        }

        //If accordWidth of one row is small (which can be represented as an image of a short volta) then change the image to short half
        if(!isShort)
        {
            if (firstRowAccords.Count < 3)
            {
                voltaShortFirstMain.GetComponent<Image>().sprite = isPrima ? primaShortFirst : secundaShortFirst;
                voltaWidthFirst *= 2;
            }
            else if (secondRowAccords.Count < 3)
            {
                voltaShortSecondMain.GetComponent<Image>().sprite = primaShortSecond;
                voltaWidthSecond *= 2;
            }
        }

        voltaShortFirstMain.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidthFirst, voltaWidthFirst * voltaMultiplierFirst);
        voltaShortSecondMain.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidthSecond, voltaWidthSecond * voltaMultiplierSecond);
        voltaShortFirstRenderer.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidthFirst, voltaWidthFirst * voltaMultiplierFirst);
        voltaShortSecondRenderer.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidthSecond, voltaWidthSecond * voltaMultiplierSecond);

    }
    private void InsertPrimaSecundaSigns(List<Image> accordsList, int beginIndex, int endIndex, bool isPrima)
    {

        if (endIndex - beginIndex < 5)
        {
            //If this is true then volta is in one row
            if (CheckIfVoltaOneRow(beginIndex, endIndex, isPrima))
            {
                GameObject voltaShortMain = Instantiate(accordImagePrefab, panelTransformMain);
                GameObject voltaShortRenderer = Instantiate(accordImagePrefab, panelTransformRender);

                voltaShortMain.tag = "Accord";
                //Places appropriate image based on the volta chosen
                if(isPrima)
                {
                    voltaShortMain.GetComponent<Image>().sprite = primaShortImage;
                    voltaShortRenderer.GetComponent<Image>().sprite = primaShortImage;
                }
                else
                {
                    voltaShortMain.GetComponent<Image>().sprite = secundaShortImage;
                    voltaShortRenderer.GetComponent<Image>().sprite = secundaShortImage;
                }
                //Code for positioning anchors
                voltaShortMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                voltaShortMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                voltaShortRenderer.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                voltaShortRenderer.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                //Positioning the volta to be perfectly above the selected accords
                voltaShortMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    (accordsList[beginIndex].rectTransform.anchoredPosition.x + accordsList[endIndex].rectTransform.anchoredPosition.x) / 2,
                    accordsList[beginIndex].rectTransform.anchoredPosition.y + voltaOffset);
                voltaShortRenderer.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    (accordsList[beginIndex].rectTransform.anchoredPosition.x + accordsList[endIndex].rectTransform.anchoredPosition.x) / 2,
                    accordsList[beginIndex].rectTransform.anchoredPosition.y + voltaOffset);

                float accordWidths = 0;
                //Calculating width based on the width of the images:
                for (int i = beginIndex; i <= endIndex; i++)
                {
                    accordWidths += accordsList[i].rectTransform.rect.width;
                }
                float voltaWidth = accordWidths + (endIndex - beginIndex) / maxAccords * dividerSize.x;
                voltaShortMain.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidth, 35);
                voltaShortRenderer.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidth, 35);

            }
            //else volta is in two rows
            else
            {
                InsertVolta2Rows(accordsList, beginIndex, endIndex, isPrima, true);
            }
        }
        //Insert longer volta
        else
        {
            //If this is true then volta is in one row
            if (CheckIfVoltaOneRow(beginIndex, endIndex, isPrima))
            {
                GameObject voltaShortMain = Instantiate(accordImagePrefab, panelTransformMain);
                GameObject voltaShortRenderer = Instantiate(accordImagePrefab, panelTransformRender);

                voltaShortMain.tag = "Accord";
                if (isPrima)
                {
                    voltaShortMain.GetComponent<Image>().sprite = primaWideImage;
                    voltaShortRenderer.GetComponent<Image>().sprite = primaWideImage;
                }
                else
                {
                    voltaShortMain.GetComponent<Image>().sprite = secundaWideImage;
                    voltaShortRenderer.GetComponent<Image>().sprite = secundaWideImage;
                }

                voltaShortMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                voltaShortMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                voltaShortRenderer.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                voltaShortRenderer.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                voltaShortMain.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    (accordsList[beginIndex].rectTransform.anchoredPosition.x + accordsList[endIndex].rectTransform.anchoredPosition.x) / 2,
                    accordsList[beginIndex].rectTransform.anchoredPosition.y + voltaOffset);
                voltaShortRenderer.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    (accordsList[beginIndex].rectTransform.anchoredPosition.x + accordsList[endIndex].rectTransform.anchoredPosition.x) / 2,
                    accordsList[beginIndex].rectTransform.anchoredPosition.y + voltaOffset);

                float accordWidths = 0;
                //Calculating width based on the width of the images:
                for (int i = beginIndex; i <= endIndex; i++)
                {
                    accordWidths += accordsList[i].rectTransform.rect.width;
                }
                float voltaWidth = accordWidths + (endIndex - beginIndex) / maxAccords * dividerSize.x;
                voltaShortMain.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidth, voltaWidth * .1f);
                voltaShortRenderer.GetComponent<RectTransform>().sizeDelta = new Vector2(voltaWidth, voltaWidth * .1f);

            }
            //else volta is in two rows
            else
            {
                InsertVolta2Rows(accordsList, beginIndex, endIndex, isPrima, false);
            }
        }
    }
    private void PlaceSectionText(string text, bool isRewritting)
    {
        switch (CurrentlyEditing)
        {
            case "forspil":
                forspilEndedPosition = new Vector2(currentX, currentY);
                break;
            case "strofa":
                strofaEndedPosition = new Vector2(currentX, currentY);
                break;
            case "refren":
                refrenEndedPosition = new Vector2(currentX, currentY);
                break;
        } 

        switch (text)
        {
            case "Foršpil":
                currentX = forspilEndedPosition.x;
                currentY = forspilEndedPosition.y;
                CurrentlyEditing = "forspil";
                break;
            case "Strofa":
                CurrentlyEditing = "strofa";
                //If it's the first time going to strofa, place the text in the appropriate position and increase the number of active sections
                if (strofaTextMain.text == "")
                {
                    strofaTextMain.rectTransform.localPosition =
                    new Vector2(-panelTransformMain.rect.width / 2 + strofaTextMain.rectTransform.rect.width, currentY - accordSize.y * 2);
                    strofaTextRender.rectTransform.localPosition =
                    new Vector2(-panelTransformMain.rect.width / 2 + strofaTextRender.rectTransform.rect.width, currentY - accordSize.y * 2);
                }
                //If it's the first time going to strofa, manually set up the cursor for the beginning of writing in that section
                if (strofaEndedPosition == new Vector2(0,0))
                {
                    currentX = -panelTransformMain.rect.width / 2 + sideMargins;
                    currentY = panelTransformMain.rect.height / 2 - lineSpacing - ((currentRow+2) * (accordSize.y + accordSize.y * 0.5f)) - 
                        2 * forspilTextMain.rectTransform.rect.height;
                    previousStrofaImageSize = 75;
                    if(!isRewritting)
                    {
                        PlaceImage(currentTactImage, ImageType.boxSize);
                    }
                }
                else
                {
                    currentX = strofaEndedPosition.x;
                    currentY = strofaEndedPosition.y;
                }

                strofaTextMain.text = text;
                strofaTextRender.text = text;

                break;
            case "Refren":
                CurrentlyEditing = "refren";
                if (refrenTextMain.text == "")
                {
                    refrenTextMain.rectTransform.localPosition =
                    new Vector2(-panelTransformMain.rect.width / 2 + refrenTextMain.rectTransform.rect.width, currentY - accordSize.y * 2);
                    refrenTextRender.rectTransform.localPosition =
                    new Vector2(-panelTransformMain.rect.width / 2 + refrenTextRender.rectTransform.rect.width, currentY - accordSize.y * 2);
                }
                //If it's the first time going to strofa, manually set up the cursor for the beginning of writing in that section
                if (refrenEndedPosition == new Vector2(0, 0))
                {
                    currentX = -panelTransformMain.rect.width / 2 + sideMargins;
                    currentY = panelTransformMain.rect.height / 2 - lineSpacing - ((currentRow + 4) * (accordSize.y + accordSize.y * 0.5f)) -
                        3 * forspilTextMain.rectTransform.rect.height;
                    previousRefrenImageSize = 75;
                    if (!isRewritting)
                    {
                        PlaceImage(currentTactImage, ImageType.boxSize);
                    }
                }
                else
                {
                    currentX = refrenEndedPosition.x;
                    currentY = refrenEndedPosition.y;
                }
                refrenTextMain.text = text;
                refrenTextRender.text = text;

                break;
        }

    }
    /// <summary>
    /// Function called in this class when the divider needs to be placed in the panel
    /// </summary>
    /// <param name="dividerImage"></param>
    public void PlaceDivider(Sprite dividerImage, bool isRewritting, int index)
    {
        if(index > 0)
        {
            try
            {
                if ((sectionSprites[index - 1].name.Contains("repeat") ||
            sectionSprites[index].name.Contains("repeat") ||
            sectionSprites[index + 1].name.Contains("repeat")))
                {
                    return;
                }
            }
            catch
            {
                Debug.Log("Index out of bounds");
            }
            
        }
        if(isRewritting)
        {
            PlaceRewrittenImage(dividerImage, ImageType.divider, index);
        }
        else
        {
            PlaceImage(dividerImage, ImageType.divider);
        }
    }
    /// <summary>
    /// Function to place the image in the panel. The parameter isAccord is for the function to know which width it should 
    /// scale to for it to be correctly placed in the panel.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="isAccord"></param>
    public void PlaceImage(Sprite image, ImageType imageType)
    {
        //Instantiating the image prefab and placing the wanted image on it.
        GameObject accordImageInstanceMain = Instantiate(accordImagePrefab, panelTransformMain);
        GameObject accordImageInstanceRender = Instantiate(accordImagePrefab, panelTransformRender);
        if (!image.name.Contains("Tact") && !image.name.Contains("divider"))
            accordImageInstanceMain.tag = "Accord";

        accordImageInstanceMain.GetComponent<Image>().sprite = image;
        accordImageInstanceRender.GetComponent<Image>().sprite = image;
        // Set the anchor points for the image. They will be centered and the image position depends on it because it will 
        //calculate the position differently based on the set anchor points.
        accordImageInstanceMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        accordImageInstanceMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        accordImageInstanceRender.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        accordImageInstanceRender.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        float previousImageSize = 0;
        switch(CurrentlyEditing)
        {
            case "forspil": previousImageSize = previousForspilImageSize;
                break;
            case "strofa": previousImageSize = previousStrofaImageSize;
                break;
            case "refren": previousImageSize = previousRefrenImageSize;
                break;
            default:
                previousImageSize = 30;
                break;
        }
        // Check if image goes beyond panel width
        //If previous image was divider, check for the width of the whole accord group

        if(previousImageSize == 30)
        {
            if (currentX + (accordSize.x * maxAccords) > (panelTransformMain.rect.width / 2 - sideMargins - accordSize.x - dividerSize.x))
            {
                AddAccordImageToList(accordImageInstanceMain, imageType, accordImageInstanceMain.GetComponent<Image>(), true);
                AddAccordImageToList(accordImageInstanceRender, imageType, accordImageInstanceRender.GetComponent<Image>(), false);
                RewriteAllAccords();
                return;
            }
        }

        AddAccordImageToList(accordImageInstanceMain, imageType, accordImageInstanceMain.GetComponent<Image>(), true);
        AddAccordImageToList(accordImageInstanceRender, imageType, accordImageInstanceRender.GetComponent<Image>(), false);

        //If the image placed is tact then the function ends here because it shouldn't be counted as an accord step
        if (image.name.Contains("Tact") || image.name.Contains("divider") || image.name.Contains("repeat"))
        {
            return;
        }
        //If the counter is 0, place the divider and reset the counter. The counters are divided because the cursor can jump
        //from one section to another and this check is needed so as to not place dividers in the wrong place

        CountDividerStep(false, -1);
    }
    private void CountDividerStep(bool isRewritting, int index)
    {
        switch (CurrentlyEditing)
        {
            case "forspil":
                forspilAccordStep--;
                if (forspilAccordStep == 0)
                {
                    forspilAccordStep = maxAccords;
                    PlaceDivider(dividerImage, isRewritting, index);
                }
                break;
            case "strofa":
                strofaAccordStep--;
                if (strofaAccordStep == 0)
                {
                    strofaAccordStep = maxAccords;
                    PlaceDivider(dividerImage, isRewritting, index);
                }
                break;
            case "refren":
                refrenAccordStep--;
                if (refrenAccordStep == 0)
                {
                    refrenAccordStep = maxAccords;
                    PlaceDivider(dividerImage, isRewritting, index);
                }
                break;
        }
    }
    private void AddAccordImageToList(GameObject accordImageInstance, ImageType imageType, Image image, bool addToList)
    {
        //This condition checks if its a simple dur accord because they are box sized, and if it's not it will give them 
        //more width. The previous image size is needed to calculate where to place the next accord so as to not overwrite
        //the previous accord.
        if (imageType == ImageType.boxSize)
        {
            if (addToList)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        currentX += previousForspilImageSize - (previousForspilImageSize - accordSize.x) / 2;
                        previousForspilImageSize = accordSize.x;
                        if (addToList)
                            forspilEnteredAccords.Add(image);
                        break;
                    case "strofa":
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - accordSize.x) / 2;
                        previousStrofaImageSize = accordSize.x;
                        if (addToList)
                            strofaEnteredAccords.Add(image);
                        break;
                    case "refren":
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - accordSize.x) / 2;
                        previousRefrenImageSize = accordSize.x;
                        if (addToList)
                            refrenEnteredAccords.Add(image);
                        break;

                }
            }

            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = accordSize;

        }
        else if (imageType == ImageType.moreWidth)
        {
            if (addToList)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        currentX += previousForspilImageSize - (previousForspilImageSize - accordSize.x * 1.35f) / 2;
                        previousForspilImageSize = accordSize.x * 1.35f;
                        forspilEnteredAccords.Add(image);
                        break;
                    case "strofa":
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - accordSize.x * 1.35f) / 2;
                        previousStrofaImageSize = accordSize.x * 1.35f;
                        strofaEnteredAccords.Add(image);
                        break;
                    case "refren":
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - accordSize.x * 1.35f) / 2;
                        previousRefrenImageSize = accordSize.x * 1.35f;
                        refrenEnteredAccords.Add(image);
                        break;
                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(accordSize.x * 1.35f, accordSize.y);

        }
        else if (imageType == ImageType.divider)
        {
            if (addToList)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        //Because Divider is width:10
                        currentX += previousForspilImageSize - (previousForspilImageSize - dividerSize.x) / 2;
                        previousForspilImageSize = dividerSize.x;
                        break;
                    case "strofa":
                        //Because Divider is width:10
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - dividerSize.x) / 2;
                        previousStrofaImageSize = dividerSize.x;
                        break;
                    case "refren":
                        //Because Divider is width:10
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - dividerSize.x) / 2;
                        previousRefrenImageSize = dividerSize.x;
                        break;
                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = dividerSize;
        }

        //Places the instantiated image on the position set before this line
        accordImageInstance.GetComponent<Image>().rectTransform.anchoredPosition = new Vector2(currentX, currentY);
    }
    //For rewritting
    private void AddAccordImageRewritten(GameObject accordImageInstance, ImageType imageType, Sprite image, bool mainPanel)
    {
        if (imageType == ImageType.boxSize)
        {
            if (mainPanel)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        currentX += previousForspilImageSize - (previousForspilImageSize - accordSize.x) / 2;
                        previousForspilImageSize = accordSize.x;
                        break;
                    case "strofa":
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - accordSize.x) / 2;
                        previousStrofaImageSize = accordSize.x;
                        break;
                    case "refren":
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - accordSize.x) / 2;
                        previousRefrenImageSize = accordSize.x;
                        break;

                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = accordSize;

        }
        else if (imageType == ImageType.moreWidth)
        {
            if (mainPanel)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        currentX += previousForspilImageSize - (previousForspilImageSize - accordSize.x * 1.35f) / 2;
                        previousForspilImageSize = accordSize.x * 1.35f;
                        break;
                    case "strofa":
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - accordSize.x * 1.35f) / 2;
                        previousStrofaImageSize = accordSize.x * 1.35f;
                        break;
                    case "refren":
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - accordSize.x * 1.35f) / 2;
                        previousRefrenImageSize = accordSize.x * 1.35f;
                        break;
                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = new Vector2(accordSize.x * 1.35f, accordSize.y);

        }
        else if (imageType == ImageType.divider)
        {
            if (mainPanel)
            {
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        //Because Divider is width:10
                        currentX += previousForspilImageSize - (previousForspilImageSize - dividerSize.x) / 2;
                        previousForspilImageSize = dividerSize.x;
                        break;
                    case "strofa":
                        //Because Divider is width:10
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - dividerSize.x) / 2;
                        previousStrofaImageSize = dividerSize.x;
                        break;
                    case "refren":
                        //Because Divider is width:10
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - dividerSize.x) / 2;
                        previousRefrenImageSize = dividerSize.x;
                        break;
                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = dividerSize;
        }
        else if(imageType == ImageType.repeating)
        {
            Vector2 oldDividerSize = dividerSize;
            dividerSize = new Vector2(dividerSize.x * 1.5f, dividerSize.y);
            if (mainPanel)
            {
                
                switch (CurrentlyEditing)
                {
                    case "forspil":
                        //Because Divider is width:10
                        currentX += previousForspilImageSize - (previousForspilImageSize - dividerSize.x) / 2;
                        previousForspilImageSize = dividerSize.x;
                        break;
                    case "strofa":
                        //Because Divider is width:10
                        currentX += previousStrofaImageSize - (previousStrofaImageSize - dividerSize.x) / 2;
                        previousStrofaImageSize = dividerSize.x;
                        break;
                    case "refren":
                        //Because Divider is width:10
                        currentX += previousRefrenImageSize - (previousRefrenImageSize - dividerSize.x) / 2;
                        previousRefrenImageSize = dividerSize.x;
                        break;
                }
            }
            accordImageInstance.GetComponent<Image>().rectTransform.sizeDelta = dividerSize;
            dividerSize = oldDividerSize;
        }

        //Places the instantiated image on the position set before this line
        accordImageInstance.GetComponent<Image>().rectTransform.anchoredPosition = new Vector2(currentX, currentY);
    }
    /// <summary>
    /// Function which is called in the functions to rewrite all accords. Rewriting accords is needed so that they can all be moved
    /// in the rows they belong to when a new row is inserted between the lines. This rewriting is occured everytime when a new row
    /// is inserted, not just when there is more than 1 section active.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="imageType"></param>
    private void PlaceRewrittenImage(Sprite image, ImageType imageType, int index)
    {
        GameObject accordImageInstanceMain = Instantiate(accordImagePrefab, panelTransformMain);
        GameObject accordImageInstanceRender = Instantiate(accordImagePrefab, panelTransformRender);

        if (!image.name.Contains("Tact") && !image.name.Contains("divider"))
            accordImageInstanceMain.tag = "Accord";

        accordImageInstanceMain.GetComponent<Image>().sprite = image;
        accordImageInstanceRender.GetComponent<Image>().sprite = image;

        accordImageInstanceMain.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        accordImageInstanceMain.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        accordImageInstanceRender.GetComponent<Image>().rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        accordImageInstanceRender.GetComponent<Image>().rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        float previousImageSize = 0;
        switch (CurrentlyEditing)
        {
            case "forspil":
                previousImageSize = previousForspilImageSize;
                if(imageType != ImageType.divider)
                    forspilEnteredAccords.Add(accordImageInstanceMain.GetComponent<Image>());
                break;
            case "strofa":
                previousImageSize = previousStrofaImageSize;
                if (imageType != ImageType.divider)
                    strofaEnteredAccords.Add(accordImageInstanceMain.GetComponent<Image>());
                break;
            case "refren":
                previousImageSize = previousRefrenImageSize;
                if (imageType != ImageType.divider)
                    refrenEnteredAccords.Add(accordImageInstanceMain.GetComponent<Image>());
                break;
            default:
                previousImageSize = 30;
                break;
        }
        // Check if image goes beyond panel width
        //If previous image was divider, check for the width of the whole accord group
        if (previousImageSize == 30 || previousImageSize == (dividerSize.x * 1.5f))
        {
            if (currentX + (accordSize.x * maxAccords) > (panelTransformMain.rect.width / 2 - sideMargins - accordSize.x - dividerSize.x))
            {
                currentX = -panelTransformMain.rect.width / 2 + sideMargins;
                currentRow++;
                currentY -= (accordSize.y + accordSize.y * 0.5f);
                PlaceDivider(dividerImage, true, -1);
            }
        }

        AddAccordImageRewritten(accordImageInstanceMain, imageType, image, true);
        AddAccordImageRewritten(accordImageInstanceRender, imageType, image, false);

        if (image.name.Contains("Tact") || image.name.Contains("divider") || image.name.Contains("repeat"))
        {
            return;
        }

        CountDividerStep(true, index);
    }
    /// <summary>
    /// This function is called whenever a new row is inserted
    /// </summary>
    private void RewriteAllAccords()
    {
        currentlyEditingBeforeRewritten = CurrentlyEditing;
        ClearDisplayPanel();

        RewriteForspilSection();
        RewriteStrofaSection();
        RewriteRefrenSection();
        RewritePrimaSecunda();
        SetPositionFromBeforeRewritting();
    }
    /// <summary>
    /// This function clears all the accords, tacts, and auxillary signs from the display panel
    /// </summary>
    private void ClearDisplayPanel()
    {
        //It starts from 4 because there are 4 elements which need to be kept in that panel
        for (int i = 4; i < panelTransformMain.childCount; i++)
        {
            Destroy(panelTransformMain.GetChild(i).gameObject);
        }
        for (int i = 4; i < panelTransformRender.childCount; i++)
        {
            Destroy(panelTransformRender.GetChild(i).gameObject);
        }
        
    }
    public void ResetEverything()
    {
        ClearDisplayPanel();
        forspilEnteredAccords.Clear();
        strofaEnteredAccords.Clear();
        refrenEnteredAccords.Clear();

        currentRow = 0;
        currentX = -panelTransformMain.rect.width / 2 + sideMargins;
        currentY = panelTransformMain.rect.height / 2 - lineSpacing - forspilTextMain.rectTransform.rect.height;

        CurrentlyEditing = "forspil";
        currentlyEditingBeforeRewritten = "forspil";

        refrenTextMain.text = "";
        refrenTextRender.text = "";
        strofaTextMain.text = "";
        strofaTextRender.text = "";

        forspilEndedPosition = new Vector2(0, 0);
        strofaEndedPosition = new Vector2(0, 0);
        refrenEndedPosition = new Vector2(0, 0);
    }
    private void RewritePrimaSecunda()
    {
        //It adds 3 to the counter because pair has 2 indices of accords and one number is for knowing where to place it
        for (int i = 0; i < primaImagePairs.Count; i+=3)
        {
            RewritePrima(primaImagePairs[i], primaImagePairs[i + 1]);
        }
        for (int i = 0; i < secundaImagePairs.Count; i += 3)
        {
            RewriteSecunda(secundaImagePairs[i], secundaImagePairs[i + 1]);
        }
    }
    /// <summary>
    /// This function only rewrites the forspil section
    /// </summary>
    private void RewriteForspilSection()
    {
        //Resetting accord steps because setting this variable will trigger set property
        CurrentTact = CurrentTact;
        CurrentlyEditing = "forspil";
        currentRow = 0;

        currentX = -panelTransformMain.rect.width / 2 + sideMargins;
        currentY = panelTransformMain.rect.height / 2 - lineSpacing - forspilTextMain.rectTransform.rect.height;

        if (strofaEndedPosition != new Vector2(0, 0) && currentlyEditingBeforeRewritten == "forspil")
        {
            strofaEndedPosition.y -= accordSize.y;
        }
        if(refrenEndedPosition != new Vector2(0,0) && currentlyEditingBeforeRewritten != "refren")
        {
            refrenEndedPosition.y -= accordSize.y;
        }
        RewriteImagesInSections(forspilEnteredAccords);
        forspilEndedPosition = new Vector2(currentX, currentY);
    }
    /// <summary>
    /// This function only rewrites the strofa section, if it was written in the first place
    /// </summary>
    private void RewriteStrofaSection()
    {
        if (strofaTextMain.text == "") return;

        strofaTextMain.text = "";
        strofaTextRender.text = "";

        PlaceSectionText("Strofa", true);
        if(strofaEndedPosition != new Vector2(0,0))
        {
            currentX = -panelTransformMain.rect.width / 2 + sideMargins;
            currentY = panelTransformMain.rect.height / 2 - lineSpacing - ((currentRow + 2) * (accordSize.y + accordSize.y * 0.5f)) -
                2 * forspilTextMain.rectTransform.rect.height;
        }
        
        RewriteImagesInSections(strofaEnteredAccords);
        strofaEndedPosition = new Vector2(currentX, currentY);
    }
    /// <summary>
    /// This function only rewrites the refren section, if it was written in the first place
    /// </summary>
    private void RewriteRefrenSection()
    {
        if (refrenTextMain.text == "") return;

        refrenTextMain.text = "";
        refrenTextRender.text = "";

        PlaceSectionText("Refren", true);
        if (refrenEndedPosition != new Vector2(0, 0))
        {
            currentX = -panelTransformMain.rect.width / 2 + sideMargins;
            currentY = panelTransformMain.rect.height / 2 - lineSpacing - ((currentRow + 4) * (accordSize.y + accordSize.y * 0.5f)) -
                3 * forspilTextMain.rectTransform.rect.height;
        }

        RewriteImagesInSections(refrenEnteredAccords);
        refrenEndedPosition = new Vector2(currentX, currentY);
    }
    /// <summary>
    /// This function rewrites images and places them in the correct width needed based on their names, because dur accords and repeat
    /// sign need to be box sized, others have more width
    /// </summary>
    private void RewriteImagesInSections(List<Image> sectionEnteredAccords)
    {
        int i;
        sectionSprites = new Sprite[sectionEnteredAccords.Count];
        for (i = 0; i < sectionSprites.Length; i++)
        {
            sectionSprites[i] = sectionEnteredAccords[i].sprite;
        }
        switch(CurrentlyEditing)
        {
            case "forspil":
                forspilEnteredAccords.Clear();            
                break;
            case "strofa":
                strofaEnteredAccords.Clear();
                break;
            case "refren":
                refrenEnteredAccords.Clear();
                break;
        }
        i = 0;
        foreach (Sprite image in sectionSprites)
        {
            if (image.name.Contains("Dur") || image.name == "%" || image.name.Contains("Tact"))
            {
                PlaceRewrittenImage(image, ImageType.boxSize, i);
            }
            else if (image.name.Contains("repeat"))
            {
                PlaceRewrittenImage(image, ImageType.repeating, i);
            }
            else
            {
                PlaceRewrittenImage(image, ImageType.moreWidth, i);
            }
            i++;
        }
    }
    /// <summary>
    /// When all sections are rewritten the pointer needs to go back to where it was before rewritting, because during rewritting
    /// the pointer is changing positions. This function brings it back to the original place and the conditions inside switch are
    /// meant to prevent bugs because if there hasn't been set an end position, that means that the cursor should stay on that position
    /// </summary>
    private void SetPositionFromBeforeRewritting()
    {
        CurrentlyEditing = currentlyEditingBeforeRewritten;
        switch(CurrentlyEditing)
        {
            case "forspil":
                if (forspilEndedPosition == new Vector2(0, 0)) return;
                currentX = forspilEndedPosition.x;
                currentY = forspilEndedPosition.y;
                break;
            case "strofa":
                if (strofaEndedPosition == new Vector2(0, 0)) return;
                currentX = strofaEndedPosition.x;
                currentY = strofaEndedPosition.y;
                break;
            case "refren":
                if (refrenEndedPosition == new Vector2(0, 0)) return;
                currentX = refrenEndedPosition.x;
                currentY = refrenEndedPosition.y;
                break;
        }
    }
}

