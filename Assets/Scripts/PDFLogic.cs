using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System;
using SFB;
using System.Windows.Forms;

public class PDFLogic : MonoBehaviour
{
    [SerializeField]
    private CommandsLogic commandsLogic;
    private Document document;
    [SerializeField]
    private Camera rendererCamera;
    public RenderTexture rendererTexture;
    [SerializeField]
    private RectTransform renderPanel;
    [SerializeField]
    private Canvas panelCanvas;

    private string filePath;
    /// <summary>
    /// Function called from MainMenu when the user clicks the button Sacuvaj pesmu, currently closes the document but needs to be 
    /// changed
    /// </summary>
    public void SavePDF()
    {
        try
        {
            document.Add(new Paragraph("This is some additional text"));
            document.Close();
        }
        catch{ }

        document = new Document(PageSize.A4);

        // Create a PDF writer instance using the selected file path
        PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

        // Open the document
        document.Open();

        CapturePanelImageToPDF();

        document.Close();
        Debug.Log("PDF file saved successfully!");
    }

    private void OnApplicationQuit()
    {
        try
        {
            document.Add(new Paragraph("This is some additional text"));
            document.Close();
        }
        catch { }
        document = new Document(PageSize.A4);


        // Create a PDF writer instance using the selected file path
        PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

        // Open the document
        document.Open();

        CapturePanelImageToPDF();
        commandsLogic.ResetEverything();
        document.Close();
    }
    private void CapturePanelImageToPDF()
    {
        RenderTexture.active = rendererCamera.targetTexture; 

        rendererCamera.Render(); 

        // Create Texture2D and read pixels
        Texture2D capturedImage = new Texture2D(rendererCamera.targetTexture.width, rendererCamera.targetTexture.height); 
        capturedImage.ReadPixels(new Rect(0, 0, rendererCamera.targetTexture.width, rendererCamera.targetTexture.height), 0, 0);
        capturedImage.Apply();

        float imageWidth = PageSize.A4.Width;
        float imageHeight = capturedImage.height * (imageWidth / capturedImage.width);

        byte[] imageBytes = capturedImage.EncodeToJPG(100);
        Image iTextImage = Image.GetInstance(imageBytes);

        iTextImage.ScaleToFit(imageWidth, imageHeight);
        iTextImage.Alignment = Element.ALIGN_CENTER;
        
        document.Add(iTextImage);
    }

   
    public void CreatePDF()
    {
        if(document != null)
        {
            CapturePanelImageToPDF();
            document.Close();
        }
        
        filePath = StandaloneFileBrowser.SaveFilePanel("Save PDF", "", "Nova pesma", "pdf");
        
        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                // Create a new PDF document
                document = new Document(PageSize.A4);

                commandsLogic.ResetEverything();

                // Create a PDF writer instance using the selected file path
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));

                // Open the document
                document.Open();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error creating PDF: " + ex.Message);
            }
        }
        else
        {
            Debug.Log("PDF saving canceled by user.");
        }
    }
}

