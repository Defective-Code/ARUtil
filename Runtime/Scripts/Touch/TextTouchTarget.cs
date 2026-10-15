using UnityEngine;

public class TextTouchTarget : TouchObject
{
    [SerializeField] private TextMesh text;
    private bool textToggle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //text = GetComponentInChildren<TextMesh>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        //_touchObject = GetComponent<TouchObject>();

        //_touchObject.OnTouch += ModelViewer;
        OnTouch += TextPopup;
    }

    void OnDisable()
    {
        //_touchObject = GetComponent<TouchObject>();

        //_touchObject.OnTouch += ModelViewer;
        OnTouch -= TextPopup;
    }

    private void TextPopup()
    {
        Debug.Log($"Hit the {gameObject.name}");
        text.gameObject.SetActive(textToggle);
        textToggle = !textToggle;
    }
}
