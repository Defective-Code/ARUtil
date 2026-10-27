using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class ToggleTouchTarget : TouchObject
{
    [SerializeField] GameObject toToggle; // gameObject to toggle when tapped
    [SerializeField] GameObject marker; // the gameobject that represents the visual marker

    private bool toggle;
    private GameObject[] children;

    private bool touched = false; // bool to track if this marker has been touched once before.
    Coroutine indicator; // coroutine to show the user to touch this icon

    private Vector3 startingScale;
    [SerializeField] private Vector3 endScale = new Vector3(1.2f, 1.2f, 1.2f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        if (toToggle == null)
        {
            toToggle = this.gameObject;
        }

        toToggle.SetActive(false);

        //_touchObject = GetComponent<TouchObject>();

        //_touchObject.OnTouch += ModelViewer;
        OnTouch += Toggle;

        startingScale = transform.localScale;

        if (!touched)
            indicator = StartCoroutine(Indicate());
    }

    void OnDisable()
    {
        //_touchObject = GetComponent<TouchObject>();

        //_touchObject.OnTouch += ModelViewer;
        OnTouch -= Toggle;
    }

    void Toggle()
    {
        toggle = !toggle;

        if (!touched)
        {
            StopCoroutine(indicator);
            touched = true;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            //transform.GetChild(i).gameObject.SetActive(toggle);
            toToggle.transform.GetChild(i).gameObject.SetActive(toggle);
        }
    }

    IEnumerator Indicate()
    {
        marker.transform.localScale = Vector3.Lerp(startingScale, endScale, Time.deltaTime);
        yield return null;
    }
}
