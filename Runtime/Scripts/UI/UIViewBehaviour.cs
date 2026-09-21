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
        OnInitialize();
        _initialized = true;
    }

    /// <summary>Your existing OnEnable/Start binding code goes here, verbatim</summary>
    protected virtual void OnInitialize() { }

    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

public enum UILayer {  Page, Hud, Overlay }
