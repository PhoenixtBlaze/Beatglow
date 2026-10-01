using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeatGlow
{
    internal sealed class BeatGlowCoroutineHost : MonoBehaviour
    {
        private static BeatGlowCoroutineHost _instance;
        private static readonly Queue<Action> Pending = new Queue<Action>();

        internal static void Ensure()
        {
            if (_instance != null)
                return;

            GameObject go = new GameObject("BeatGlow.CoroutineHost");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<BeatGlowCoroutineHost>();
        }

        internal static void Enqueue(Action action)
        {
            if (action == null)
                return;
            lock (Pending)
                Pending.Enqueue(action);
        }

        internal static Coroutine Run(IEnumerator routine)
        {
            Ensure();
            return _instance.StartCoroutine(routine);
        }

        internal static void Stop(Coroutine routine)
        {
            if (_instance != null && routine != null)
                _instance.StopCoroutine(routine);
        }

        private void Update()
        {
            while (true)
            {
                Action action;
                lock (Pending)
                {
                    if (Pending.Count == 0)
                        return;
                    action = Pending.Dequeue();
                }

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Plugin.Log?.Error("BeatGlow main-thread action failed: " + ex);
                }
            }
        }
    }
}
