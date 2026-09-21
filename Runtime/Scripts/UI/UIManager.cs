using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private UIDocument document;
    [SerializeField] private UIViewBehaviour[] views; // drag all view components here
    [SerializeField] private UIScreen rootScreen = UIScreen.MainMenu;
    [SerializeField] private float transitionDuration = 0.15f; // Must match the Views.uss file

    private readonly Dictionary<UIScreen, UIViewBehaviour> _registry = new();
    private readonly Stack<UIViewBehaviour> _stack = new(); // This stack manages our navigation history

    private VisualElement _pageLayer, _hudLayer, _overlayLayer;
    private bool _isTransitioning;

    public UIScreen Current => _stack.Count > 0 ? _stack.Peek().Id : UIScreen.None;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        var root = document.rootVisualElement;
        _pageLayer = root.Q<VisualElement>("view-container");
        _hudLayer = root.Q<VisualElement>("hud-layer");
        _overlayLayer = root.Q<VisualElement>("overlay-layer");

        foreach (var view in views)
        {
            Debug.Log($"Mounting - {view.Id}");
            _registry[view.Id] = view;
            Mount(view);
        }

        SetHudVisible(false);
        Push(rootScreen);
    }

    private void Mount(UIViewBehaviour view)
    {
        var parent = view.Layer switch
        {
            UILayer.Hud => _hudLayer,
            UILayer.Overlay => _overlayLayer,
            _ => _pageLayer
        };

        var root = new VisualElement { name = view.Id.ToString() };
        root.style.position = Position.Absolute;
        view.Uxml.CloneTree(root);
        parent.Add(root);

        view.Bind(root); // the existing init code runs here (eg. imageViewer.cs etc)

        // By default make it so the Hud is visible
        if (view.Layer == UILayer.Hud)
        {
            root.AddToClassList("view--mounted");
            root.AddToClassList("view--visible");
        }
    }

    private void Update()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (UnityEngine.InputSystem.Keyboard.current.escapeKey.isPressed)
            {
                HandleBack();
            }
        }
    }

    // --- view lifecycle --------------------------------------

    //private UIViewPresenter GetOrCreate(UIScreen screen)
    //{
    //    if (_views.TryGetValue(screen, out var existing)) return existing;

    //    var uxml = registry.Get(screen);
    //    if (uxml == null)
    //    {
    //        Debug.LogError($"No UXML registered for {screen}");
    //        return null;
    //    }

    //    // CloneTree into a wrapper so the view owns one root element
    //    var root = new VisualElement { name = screen.ToString() };
    //    uxml.CloneTree(root);
    //    _container.Add(root);

    //    var presenter = CreatePresenter(screen);
    //    presenter.Initialize(screen, root);
    //    _views[screen] = presenter;
    //    return presenter;
    //}

    //private static UIViewPresenter CreatePresenter(UIScreen screen) => screen switch
    //{
    //    UIScreen.MainMenu => new MainMenuPresenter(),
    //    //UIScreen.Map => new MapPresenter(),
    //    //UIScreen.AnchorMenu => new AnchorMenuPresenter(),
    //    //UIScreen.PlaybackMenu => new PlaybackMenuPresenter(),
    //    //UIScreen.ImageViewer => new ImageViewer(),
    //    _ => null
    //};

    // --- navigation -------------------------------------------------------

    // This method is used to show the next/new UI element
    public void Push(UIScreen screen, object payload = null)
    {
        if (_isTransitioning) return;
        if (!_registry.TryGetValue(screen, out var next))
        {
            Debug.LogError($"No view registered for the screen - check that in the behaviour you set the Id field");
            return;
        }
        if (next.Layer == UILayer.Hud)
        {
            Debug.LogError($"{screen}  is a HUD view - use SetHudVisible instead");
            return;
        }
        Debug.Log($"{screen.ToString()} - UIScreen Value that is getting Pushed!");

        if (_stack.Count > 0 && _stack.Peek() == next) return;

        //if (payload != null && _stack.Count > 0 && _stack.Peek() is IPayloadReceiver receiver)
        if (payload != null && next is IPayloadReceiver receiver)
        {
            receiver.SetPayload(payload);
        }

        StartCoroutine(PushRoutine(next));
    }

    private IEnumerator PushRoutine(UIViewBehaviour next)
    {
        _isTransitioning = true;

        if (next.Layer != UILayer.Overlay && _stack.Count > 0)
        {
            yield return HideView(_stack.Peek());
        }

        _stack.Push(next);
        yield return ShowView(next);

        _isTransitioning = false;
    }

    // This method is backtrack through the UI stack
    public void Pop()
    {
        if (_isTransitioning || _stack.Count <= 0) return;

        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        _isTransitioning = true;

        var current = _stack.Pop();
        yield return HideView(current);

        // only re-show the one below if it was actually hidden
        if (current.Layer != UILayer.Overlay && _stack.Count > 0)
        {
            yield return ShowView(_stack.Peek());
        }

        _isTransitioning = false;
    }

    public void PopToRoot()
    {
        if (_isTransitioning) return;
        StartCoroutine(PopToRootRoutine());
    }

    private IEnumerator PopToRootRoutine()
    {
        while(_stack.Count > 1 )
        {
            yield return PopRoutine();
        }
    }

    // --- hud --------------------------------------------------------------------------------------------

    public void SetHudVisible(bool visible)
    {
        Debug.Log("Setting hud visibility");
        _hudLayer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        _hudLayer.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
    }
    private void HandleBack()
    {
        if (_stack.Count > 1) Pop();
        else Application.Quit();    
    }

    // --- transitions --------------------------------------------------------------------------------------------

    private IEnumerator ShowView(UIViewBehaviour view)
    {
        Debug.Log($"Currently ShowView for {view.Id} with uxml of {view.Uxml.ToString()}");

        view.Root.pickingMode = PickingMode.Position;
        view.OnEnter();

        view.Root.AddToClassList("view--mounted");
        yield return null;
        view.Root.AddToClassList("view--visible");
        yield return new WaitForSecondsRealtime(transitionDuration);

        Debug.Log($"PRINTING classes attached to view.Root element");
        foreach (var className in view.Root.GetClasses())
        {
            Debug.Log($"{view.Id} - {className}");
        }
    }

    private IEnumerator HideView(UIViewBehaviour view)
    {
        view.Root.pickingMode = PickingMode.Ignore;
        view.Root.RemoveFromClassList("view--visible");
        yield return new WaitForSecondsRealtime(transitionDuration);
        view.Root.RemoveFromClassList("view--mounted");
        view.OnExit();
    }
}
