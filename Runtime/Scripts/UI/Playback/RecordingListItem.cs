using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordingListItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Button button;

    private string fileName;
    private System.Action<string> onSelected;

    public void Setup(string displayName, string fullFileName, System.Action<string> selectCallback)
    {
        fileName = fullFileName;
        onSelected = selectCallback;
        label.text = displayName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onSelected?.Invoke(fileName));
    }

    public void SetHighlighted(bool highlighted)
    {
        var colors = button.colors;
        colors.normalColor = highlighted ? new Color(0.7f, 0.9f, 1f) : Color.white;
        button.colors = colors;
    }
}