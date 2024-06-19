using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SFB;
using System;
using TMPro;

public class SaveFile : MonoBehaviour
{
    public TextMeshProUGUI textMeshPro;

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL
    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    public void OnClickSave() {
        var bytes = Encoding.UTF8.GetBytes(textMeshPro.text);
        DownloadFile(gameObject.name, "OnFileDownload", "model.obj", bytes, bytes.Length);
    }

    // Called from browser
    public void OnFileDownload() { }
#else

    // Standalone platforms & editor
    public void OnClickSave()
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "", "model", "obj");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, textMeshPro.text);
        }
    }
#endif

}