using TMPro;
using UnityEngine;

public class StatSlotView : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI labelText;
    public TextMeshProUGUI valueText;

    public void Bind(string label, string value)
    {
        if (labelText != null)
        {
            labelText.text = string.IsNullOrWhiteSpace(label) ? "-" : label;
        }

        if (valueText != null)
        {
            valueText.text = string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }
}
