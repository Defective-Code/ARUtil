using UnityEngine;
using UnityEngine.UIElements;

public abstract class UIViewPresenter : MonoBehaviour
{
    public VisualElement Root { get; private set; }

    public UIScreen Id { get; private set; }    

    public void Initialize(UIScreen id, VisualElement root)
    {
        Id = id;
        Root = root;
        Root.AddToClassList("view");
        OnInitialize();
    }

    /// <summary>
    /// Query and bind elements here. Called once
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Refresh from live state/ Called everytime this becomes the top of stack
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Persist state. Called when covered or popped
    /// </summary>
    public virtual void OnExit() { }
}
