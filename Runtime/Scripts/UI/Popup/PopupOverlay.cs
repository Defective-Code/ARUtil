using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;

public class PopupOverlay : UIViewBehaviour, IPayloadReceiver
{
    // UI Elements 
    [Header("Overlay root - the element to show/hide")]
    public VisualElement overlayRoot;
    public VisualElement popup;

    private VisualElement content = null; // stores reference to the VisualElement which is the current content of the Popup

    protected override void OnInitialize()
    {
        EnhancedTouchSupport.Enable(); // required for Touch.activeTouches

        overlayRoot = Root.Q<VisualElement>("overlay");
        overlayRoot.style.display = DisplayStyle.None; // hide the UI by default

        popup = Root.Q<VisualElement>("popup");

    }

    public void SetPayload(object payload)
    {
        content = payload as VisualElement;
    }

    public override void OnEnter()
    {

        if (content != null)
        {
            popup.Add(content);
        }

        overlayRoot.style.display = DisplayStyle.Flex;
    }

    public override void OnExit()
    {

        // if we had content in the popup then we want to remove it and set it back to null
        if (content != null)
        {
            popup.Remove(content);
            content = null;
        }
    }
}
