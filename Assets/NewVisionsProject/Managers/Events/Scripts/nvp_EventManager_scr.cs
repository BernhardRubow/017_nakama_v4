using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace newvisionsproject.managers.events
{
  public enum GameEvents
  {
    OnCreateGameInitiated,
    OnJoinGameInitiated,
    OnMatchIdAccuired,
    OnInitPlayerListReceived,
    OnMatchPresencesUpdated,
    OnMatchIsReady,
    OnGameMessageReceived,
    OnNetworkManagerInitialised,
    OnMultiplayerGameStarted
  }

  public class nvp_EventManager_scr : MonoBehaviour
  {

    // +++ Singelton ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private static nvp_EventManager_scr _instance;

    public static nvp_EventManager_scr INSTANCE
    {
      get
      {
        if (_instance == null)
        {
          var temp = new GameObject("eventManager").AddComponent<nvp_EventManager_scr>();
          _instance = temp.GetComponent<nvp_EventManager_scr>();
          _instance.Reset();
        }
        return _instance;
      }
    }




    // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private Dictionary<GameEvents, List<Action<object, object>>> eventCallbacks;




    // +++ functions ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    public void SubscribeToEvent(GameEvents e, Action<object, object> callback)
    {
      if (!eventCallbacks.ContainsKey(e))
      {
        eventCallbacks[e] = new List<Action<object, object>>();
      }

      eventCallbacks[e].Add(callback);
    }


    public void UnsubscribeFromEvent(GameEvents e, Action<object, object> observer)
    {
      if (!eventCallbacks.ContainsKey(e)) return;

      if (!eventCallbacks[e].Contains(observer)) return;

      eventCallbacks[e].Remove(observer);
    }

    public void InvokeEvent(GameEvents e, object sender, object eventArgs)
    {
      if (!eventCallbacks.ContainsKey(e)) return;

      foreach (var observer in eventCallbacks[e])
        observer(sender, eventArgs);
    }

    public void Reset()
    {
      eventCallbacks = new Dictionary<GameEvents, List<Action<object, object>>>();
    }
  }
}