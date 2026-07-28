using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    static readonly Queue<Action> _queue = new();
    static UnityMainThreadDispatcher _instance;

    public static void Enqueue(Action action)
    {
        lock (_queue) _queue.Enqueue(action);
    }

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        lock (_queue)
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
    }
}