using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class ToggleTouchTarget : TouchObject
{
    [SerializeField] GameObject marker; // the gameobject that represents the visual marker
    [SerializeField] GameObject toToggle; // gameObject to toggle visibility of when tapped
    
    private bool toggle;
    private GameObject[] children;

    private bool touched = false; // bool to track if this marker has been touched once before.
    Coroutine indicator; // coroutine to show the user to touch this icon

    private Vector3 startingScale;
    //[SerializeField] private Vector3 endScale = new Vector3(1.2f, 1.2f, 1.2f);
    [SerializeField] float amplitude = 0.1f;
    [SerializeField] float speed = 2f;

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

        if (indicator != null) StopCoroutine(indicator);
        transform.localScale = startingScale;
    }

    void Toggle()
    {
        toggle = !toggle;

        toToggle.SetActive(toggle);

        if (!touched)
        {
            StopCoroutine(indicator);
            touched = true;
        }

        //for (int i = 0; i < transform.childCount; i++)
        //{
        //    //transform.GetChild(i).gameObject.SetActive(toggle);
        //    toToggle.transform.GetChild(i).gameObject.SetActive(toggle);
        //}
    }

    IEnumerator Indicate()
    {
        float t = 0f;
        while(true)
        {
            t += Time.deltaTime * speed;
            float factor = 1f + Mathf.Sin(t) * amplitude;
            transform.localScale = startingScale * factor;
            yield return null;
        }
    }
}
