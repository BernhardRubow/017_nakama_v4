using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System;
using System.Linq;
using newvisionsproject.managers.events;

namespace newvisionsproject.nakama
{
  public class nvp_NetworkManager_scr : MonoBehaviour
  {

    // +++ inpector fields ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    [SerializeField] private nvp_NakamaManager_scr _nakama;
    [SerializeField] private int maxNumberOfPlayers;
    [SerializeField] private int minNumberOfPlayers;




    // +++ private fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    // +++ values received from nakama server
    private INSession _nakamaSession;
    private INMatch _nakamaMatch;
    private string _nakamaMatchId;
    private Queue<IEnumerator> _nakamaEventQueue = new Queue<IEnumerator>();
    private List<INUserPresence> _nakamaMatchPresences = new List<INUserPresence>();
    private INUserPresence _self;
    private INClient _client;

    // +++ internal flags for that can trigger state transitions
    private bool _connected = false;
    private bool _matchCreated = false;
    private bool _matchJoined = false;
    private bool _matchPresencesUpdated = false;

    // +++ reference to the update action of the currently active state
    private Action _stateUpdate;




    // +++ unity callbacks ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    void Awake()
    {
      // subscribe to nakama events
      _nakama.OnConnected += OnConnectedToNakamaServer;
      _nakama.OnMatchCreated += OnMatchCreated;
      _nakama.OnMatchJoined += OnMatchJoined;
      _nakama.OnMatchPresencesUpdated += OnMatchPresencesUpdated;

      // Login or Register to Nakama Multiplayer Server and retrieve the client;
      _client = _nakama.LoginOrRegister("random_guid", null);

      // directly subscribe to the in game message received event 
      // on the newly created client.
      _client.OnMatchData += OnMatchData;
    }

    void Start()
    {
      // initial state - try to connect to server
      _stateUpdate = State_ConnectToServer_Update;
    }

    void Update()
    {
      _stateUpdate();

      lock (_nakamaEventQueue)
      {
        for (int i = 0, len = _nakamaEventQueue.Count; i < len; i++)
        {
          StartCoroutine(_nakamaEventQueue.Dequeue());
        }
      }
    }




    // +++ nakama event handler +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private void OnConnectedToNakamaServer(INSession currentSession)
    {
      if (currentSession != null)
      {
        _nakamaSession = currentSession;
        _connected = true;
      }
    }

    private void OnMatchCreated(INMatch match)
    {
      if (match != null)
      {
        _nakamaMatch = match;
        _matchCreated = true;
      }
    }

    private void OnMatchJoined(INResultSet<INMatch> matches)
    {
      if (matches != null)
      {
        _matchJoined = true;
        _self = matches.Results[0].Self;
        _nakamaMatchPresences.AddRange(matches.Results[0].Presence);
        _nakamaMatchPresences.Remove(_nakamaMatchPresences.Single(x => x.Handle == _self.Handle));
      }
    }

    private void OnMatchPresencesUpdated(INMatchPresence presences)
    {
      _matchPresencesUpdated = true;
      foreach (var user in presences.Leave)
      {
        _nakamaMatchPresences.Remove(_nakamaMatchPresences.Single(x => x.Handle == user.Handle));
      }

      foreach (var user in presences.Join)
      {
        _nakamaMatchPresences.Add(user);

        var playerCount = _nakamaMatchPresences.Count();
        if (playerCount >= minNumberOfPlayers && playerCount <= maxNumberOfPlayers)
        {
          Debug.Log("Game is ready");
          Enqueue(
            () => nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIsReady, this, _nakamaMatchPresences)
          );
        }
        else
        {
          var matchIsFullMessage = NMatchDataSendMessage.Default(
            _nakamaMatchId,
            99L,
            System.Text.Encoding.UTF8.GetBytes(user.Handle));
          _nakama.SendDataMessage(matchIsFullMessage);
        }
      }
    }

    private void OnMatchData(INMatchData msg)
    {
      switch (msg.OpCode)
      {
        case 99L:
          // game is full so disconnect
          string userHandle = System.Text.Encoding.UTF8.GetString(msg.Data);
          if (userHandle == _self.Handle) _client.Disconnect();
          break;
        
        default:
          nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnGameMessageReceived, this, msg);
          break;
      }
    }




    // +++ other game event handler +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    void OnCreateGameInitiated(object sender, object eventArgs)
    {
      CreateGame();
    }

    void OnJoinGameInitiated(object sender, object eventArgs)
    {
      JoinGame(eventArgs.ToString());
    }


    // +++ public methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    public void CreateGame()
    {
      _stateUpdate = State_CreateMatch_Update;
    }

    public void JoinGame(string matchId)
    {
      _nakamaMatchId = matchId;
      _stateUpdate = State_JoinMatch_Update;
    }




    // +++ states +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    void State_ConnectToServer_Update()
    {
      if (_connected)
      {
        Debug.LogFormat("Connected to server. Session token: {0}", _nakamaSession.Token);

        // do nothing
        _stateUpdate = () => { };
      }
    }

    // +++ States that handle the creation of a match
    void State_CreateMatch_Update()
    {
      _nakama.CreateMatch();
      _stateUpdate = State_WaitingForMatchCreated;
    }

    void State_WaitingForMatchCreated()
    {
      if (_matchCreated)
      {
        Debug.LogFormat("Matcht created. MatchId: {0}", _nakamaMatch.Id);
        _nakamaMatchId = _nakamaMatch.Id;
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIdAccuired, this, _nakamaMatch.Id);
        _stateUpdate = State_WaitingForPlayers_Update;
      }
    }



    // +++ states that handle the joining of an existiong match
    void State_JoinMatch_Update()
    {
      _nakama.JoinMatch(_nakamaMatchId);
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIdAccuired, this, _nakamaMatchId);
      _stateUpdate = State_WaitingForJoin_Update;
    }

    void State_WaitingForJoin_Update()
    {
      if (_matchJoined)
      {
        Debug.LogFormat("Joined match with Id: {0}", _nakamaMatchId);
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, _nakamaMatchPresences);
        _stateUpdate = State_WaitingForPlayers_Update;
      }
    }

    void State_WaitingForPlayers_OnEnter()
    {
      _stateUpdate = State_WaitingForPlayers_Update;
    }

    void State_WaitingForPlayers_Update()
    {
      if (_matchPresencesUpdated == true)
      {
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, _nakamaMatchPresences);
        _matchPresencesUpdated = false;
      }
    }

    // +++ helper for bringing events to the main thread
    private void Enqueue(Action action)
    {
      lock (_nakamaEventQueue)
      {
        _nakamaEventQueue.Enqueue(ActionWrapper(action));
        if (_nakamaEventQueue.Count > 1024)
        {
          Debug.LogWarning("Queued actions not consumed fast enough.");
          _client.Disconnect();
        }
      }
    }

    private IEnumerator ActionWrapper(Action action)
    {
      action();
      yield return null;
    }
  }
}
