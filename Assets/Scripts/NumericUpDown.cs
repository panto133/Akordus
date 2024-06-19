using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumericUpDown : MonoBehaviour
{
    [SerializeField]
    private CommandsLogic commandsLogic;
    [SerializeField]
    private TMP_InputField numericUpDownField;
    [SerializeField]
    private float maxValue = 2;
    private float value = 0;
    

    public void IncrementValue()
    {
        if (value + 0.5f > maxValue) return;

        value += 0.5f;
        numericUpDownField.text = value.ToString();
    }
    public void DecrementValue()
    {
        if (value - 0.5f < -maxValue) return;

        value -= 0.5f;
        numericUpDownField.text = value.ToString();
    }
    public void TransposeSong()
    {
        commandsLogic.TransposeSong(value);
    }
}
