using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class UIViewBehaviour : MonoBehaviour
{
    [SerializeField] private UIScreen id;
    [SerializeField] private VisualTreeAsset uxml;
    [SerializeField] private UILayer layer = UILayer.Page;

    public UIScreen Id => id;
    public VisualTreeAsset Uxml => uxml;
    public UILayer Layer => layer;
    public VisualElement Root {  get; private set; }

    private bool _initialized;

    ///<summary>Called once by UIManager after the UXML is cloned in</summary>
    public void Bind(VisualElement root)
    {
        if (_initialized) return;
        Root = root;
        Root.AddToClassList("view");
        //InstallTouchFilter();
        //Root.pickingMode = PickingMode.Ignore;
        OnInitialize();
        _initialized = true;
    }


    readonly HashSet<int> blockedPointers = new(); // Hashset for storing pointers that get blocked
    void InstallTouchFilter()
    {
        const TrickleDown td = TrickleDown.TrickleDown;

        Root.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (IsAllowed(evt))
            {
                blockedPointers.Remove(evt.pointerId);
                return;
            }

            blockedPointers.Add(evt.pointerId);
            evt.StopImmediatePropagation();
            Debug.Log($"Blocked pointer {evt.pointerId} at {evt.position}");
        }, td);

        // Swallow the rest of a rejected touch's life, including the Click it would generate
        Root.RegisterCallback<PointerMoveEvent>(evt => BlockIfRejected(evt.pointerId, evt), td);
        Root.RegisterCallback<PointerUpEvent>(evt => BlockIfRejected(evt.pointerId, evt), td);
        Root.RegisterCallback<PointerCancelEvent>(evt => BlockIfRejected(evt.pointerId, evt), td);
        Root.RegisterCallback<ClickEvent>(evt => BlockIfRejected(evt.pointerId, evt), td);
    }

    void BlockIfRejected(int pointerId, EventBase evt)
    {
        if (blockedPointers.Contains(pointerId)) evt.StopImmediatePropagation();
    }

    bool IsAllowed(IPointerEvent evt)
    {
        Vector2 panelSize = Root.panel.visualTree.layout.size;
        Vector2 p = evt.position;
        var screen = new Vector2(
            p.x / panelSize.x * Screen.width,
            (1f - p.y / panelSize.y) * Screen.height);

        return TouchFilter.IsValidPosition(screen);
    }

    /// <summary>Your existing OnEnable/Start binding code goes here, verbatim</summary>
    protected virtual void OnInitialize() { }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

public enum UILayer {  Page, Hud, Overlay }
