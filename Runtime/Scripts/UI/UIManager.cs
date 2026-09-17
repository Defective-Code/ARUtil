using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements;

using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private UIDocument document;
    [SerializeField] private UIViewRegistry registry;
    [SerializeField] private UIScreen rootScreen = UIScreen.MainMenu;
    [SerializeField] private float transitionDuration = 0.15f; // Must match the Views.uss file

    private readonly Dictionary<UIScreen, UIViewPresenter> _views = new();
    private readonly Stack<UIViewPresenter> _stack = new Stack<UIViewPresenter>(); // This stack manages our navigation history
    private VisualElement _container;
    private bool _isTransitioning;

    public UIScreen Current => _stack.Count > 0 ? _stack.Peek().Id : UIScreen.None;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        _container = document.rootVisualElement.Q<VisualElement>("view-container");
        Push(rootScreen);
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

    private UIViewPresenter GetOrCreate(UIScreen screen)
    {
        if (_views.TryGetValue(screen, out var existing)) return existing;

        var uxml = registry.Get(screen);
        if (uxml == null)
        {
            Debug.LogError($"No UXML registered for {screen}");
            return null;
        }

        // CloneTree into a wrapper so the view owns one root element
        var root = new VisualElement { name = screen.ToString() };
        uxml.CloneTree(root);
        _container.Add(root);

        var presenter = CreatePresenter(screen);
        presenter.Initialize(screen, root);
        _views[screen] = presenter;
        return presenter;
    }

    private static UIViewPresenter CreatePresenter(UIScreen screen) => screen switch
    {
        UIScreen.MainMenu => new MainMenuPresenter(),
        UIScreen.Map => new MapPresenter(),
        UIScreen.AnchorMenu => new AnchorMenuPresenter(),
        UIScreen.PlaybackMenu => new PlaybackMenuPresenter(),
        UIScreen.ImageViewer => new ImageViewerPresenter(),
        _ => null
    };

    // --- navigation -------------------------------------------------------

    // This method is used to show the next/new UI element
    public void Push(UIScreen screen)
    {
        if (_isTransitioning) return;
        if (_stack.Count > 0 && _stack.Peek().Id == screen) return;

        var next = GetOrCreate(screen);
        if (next == null) return;  

        StartCoroutine(PushRoutine(next));
    }

    private IEnumerator PushRoutine(UIViewPresenter next)
    {
        _isTransitioning = true;

        if (_stack.Count > 0)
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
        if (_isTransitioning || _stack.Count <= 1) return;

        StartCoroutine(PopRoutine);
    }

    private IEnumerator PopRoutine()
    {
        _isTransitioning = true;

        var current = _stack.Pop();
        yield return HideView(current);
        yield return ShowView(_stack.Peek());

        _isTransitioning = false;
    }

    public void PopToRoot()
    {
        if (_isTransitioning) return;
        StartCoroutine(PopToRootRoutine);
    }

    private IEnumerator PopToRootRoutine()
    {
        while(_stack.Count > 1 )
        {
            yield return PopRoutine();
        }
    }

    private void HandleBack()
    {
        if (_stack.Count > 1) Pop();
        else Application.Quit();    
    }

    // --- transitions --------------------------------------------------------------------------------------------

    private IEnumerator ShowView(UIViewPresenter view)
    {
        view.Root.pickingMode = PickingMode.Position;
        view.OnEnter();

        view.Root.AddToClassList("view-mounted");
        yield return null;
        view.Root.AddToClassList("view--visible");
        yield return new WaitForSecondsRealtime(transitionDuration);
    }

    private IEnumerator HideView(UIViewPresenter view)
    {
        view.Root.pickingMode = PickingMode.Ignore;
        view.Root.RemoveFromClassList("view--visible");
        yield return new WaitForSecondsRealtime(transitionDuration);
        view.Root.RemoveFromClassList("view-mounted");
        view.OnExit();
    }
}
