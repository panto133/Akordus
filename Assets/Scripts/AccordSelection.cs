using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AccordSelection : MonoBehaviour
{
    [SerializeField]
    private GraphicRaycaster raycaster;

    private GameObject selectedImage = null;
    private GameObject firstSelectedImage;
    private GameObject secondSelectedImage;

    [SerializeField]
    private CommandsLogic commandsLogic;

    [SerializeField]
    private LayerMask imageLayerMask;

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            SelectImage();
        }
    }
    public void DeleteAccord()
    {
        if (secondSelectedImage != null || firstSelectedImage == null) return;
        commandsLogic.DeleteAccord(firstSelectedImage.GetComponent<Image>());
        ResetSelections();
    }
    public void InsertRepeatingSigns()
    {
        if (firstSelectedImage == null || secondSelectedImage == null) return;

        commandsLogic.InsertRepeatingSigns(firstSelectedImage.GetComponent<Image>(),
            secondSelectedImage.GetComponent<Image>());
    }
    public void InsertPrima()
    {
        if (firstSelectedImage == null || secondSelectedImage == null) return;

        commandsLogic.InsertPrima(firstSelectedImage.GetComponent<Image>(),
            secondSelectedImage.GetComponent<Image>(), false);
    }
    public void InsertSecunda()
    {
        if (firstSelectedImage == null || secondSelectedImage == null) return;

        commandsLogic.InsertSecunda(firstSelectedImage.GetComponent<Image>(),
            secondSelectedImage.GetComponent<Image>(), false);
    }
    private void SelectImage()
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current);
        pointerData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();

        // Cast the ray
        raycaster.Raycast(pointerData, results);

        foreach(RaycastResult result in results)
        {
            if (result.gameObject.CompareTag("Button"))
            {
                return;
            }
            if(result.gameObject.CompareTag("Accord"))
            {
                selectedImage = result.gameObject;
                break;
                // result.gameObject.GetComponent<Image>().color = Color.red;
            }
        }

        if(selectedImage != null)
        {
            Debug.Log(selectedImage.GetComponent<Image>().sprite.name);
            if(Input.GetKey(KeyCode.LeftShift))
            {
                if (firstSelectedImage == null)
                {
                    ResetSelections();
                    firstSelectedImage = selectedImage;
                    Image imageComponent = firstSelectedImage.GetComponent<Image>();
                    imageComponent.color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);

                }
                else
                {
                    if(secondSelectedImage != null)
                        secondSelectedImage.GetComponent<Image>().color = Color.white;

                    secondSelectedImage = selectedImage;
                    Image imageComponent = secondSelectedImage.GetComponent<Image>();
                    imageComponent.color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);
                }
            }
            else
            {
                ResetSelections();
                firstSelectedImage = selectedImage;
                Image imageComponent = firstSelectedImage.GetComponent<Image>();
                imageComponent.color = new Color(77f / 255f, 184 / 255f, 38 / 255f, 1f);
                secondSelectedImage = null;
            }
        }
        //If the user didn't click on any accord image, reset selections
        else
        {
            ResetSelections();
        }

        selectedImage = null;

        Canvas.ForceUpdateCanvases();
    }
    private void ResetSelections()
    {
        if (firstSelectedImage != null)
        {
            firstSelectedImage.GetComponent<Image>().color = Color.white;
            firstSelectedImage = null;
        }       

        if(secondSelectedImage != null)
        {
            secondSelectedImage.GetComponent<Image>().color = Color.white;
            secondSelectedImage = null;
        }
        Canvas.ForceUpdateCanvases();
    }
}
