using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer
{
    public class EventManager : MonoBehaviour
    {
        public delegate void EventHandler(object sender, EventArgs e);

        private static readonly Dictionary<EventType, EventHandler> eventDictionary = new();

        public static void StartListening(EventType eventType, EventHandler listener)
        {
            if (eventDictionary.TryGetValue(eventType, out var thisEvent))
            {
                thisEvent += listener;
                eventDictionary[eventType] = thisEvent;
            }
            else
                eventDictionary[eventType] = listener;
        }

        public static void StopListening(EventType eventType, EventHandler listener)
        {
            if (eventDictionary.TryGetValue(eventType, out var thisEvent))
            {
                thisEvent -= listener;
                eventDictionary[eventType] = thisEvent;
            }
        }

        public static void TriggerEvent(EventType eventType, object sender, EventArgs e)
        {
            if (eventDictionary.TryGetValue(eventType, out var thisEvent))
                thisEvent?.Invoke(sender, e);
        }
    }
}