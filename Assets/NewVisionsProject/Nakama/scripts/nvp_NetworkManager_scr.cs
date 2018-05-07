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
    public INSession nakamaSession;
    public INMatch nakamaMatch;
    public string nakamaMatchId;
    public Queue<IEnumerator> nakamaEventQueue = new Queue<IEnumerator>();
    public List<INUserPresence> nakamaMatchPresences = new List<INUserPresence>();
    public INUserPresence self;
    public INClient client;

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
      client = _nakama.LoginOrRegister("random_guid", null);

      // directly subscribe to the in game message received event 
      // on the newly created client.
      client.OnMatchData += OnMatchData;
    }

    void Start()
    {
      // initial state - try to connect to server
      _stateUpdate = State_ConnectToServer_Update;
    }

    void Update()
    {
      _stateUpdate();

      lock (nakamaEventQueue)
      {
        for (int i = 0, len = nakamaEventQueue.Count; i < len; i++)
        {
          StartCoroutine(nakamaEventQueue.Dequeue());
        }
      }
    }




    // +++ nakama event handler +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private void OnConnectedToNakamaServer(INSession currentSession)
    {
      if (currentSession != null)
      {
        nakamaSession = currentSession;
        _connected = true;
      }
    }

    private void OnMatchCreated(INMatch match)
    {
      if (match != null)
      {
        nakamaMatch = match;
        _matchCreated = true;
      }
    }

    private void OnMatchJoined(INResultSet<INMatch> matches)
    {
      if (matches != null)
      {
        _matchJoined = true;
        self = matches.Results[0].Self;
        nakamaMatchPresences.AddRange(matches.Results[0].Presence);
        nakamaMatchPresences.Remove(nakamaMatchPresences.Single(x => x.Handle == self.Handle));
      }
    }

    private void OnMatchPresencesUpdated(INMatchPresence presences)
    {
      _matchPresencesUpdated = true;
      foreach (var user in presences.Leave)
      {
        nakamaMatchPresences.Remove(nakamaMatchPresences.Single(x => x.Handle == user.Handle));
      }

      foreach (var user in presences.Join)
      {
        nakamaMatchPresences.Add(user);

        var playerCount = nakamaMatchPresences.Count();
        if (playerCount >= minNumberOfPlayers && playerCount <= maxNumberOfPlayers)
        {
          Debug.Log("Game is ready");
          Enqueue(
            () => nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIsReady, this, nakamaMatchPresences)
          );
        }
        else
        {
          var matchIsFullMessage = NMatchDataSendMessage.Default(
            nakamaMatchId,
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
          if (userHandle == self.Handle) client.Disconnect();
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
      nakamaMatchId = matchId;
      _stateUpdate = State_JoinMatch_Update;
    }




    // +++ states +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    void State_ConnectToServer_Update()
    {
      if (_connected)
      {
        Debug.LogFormat("Connected to server. Session token: {0}", nakamaSession.Token);

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
        Debug.LogFormat("Matcht created. MatchId: {0}", nakamaMatch.Id);
        nakamaMatchId = nakamaMatch.Id;
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIdAccuired, this, nakamaMatch.Id);
        _stateUpdate = State_WaitingForPlayers_Update;
      }
    }



    // +++ states that handle the joining of an existiong match
    void State_JoinMatch_Update()
    {
      _nakama.JoinMatch(nakamaMatchId);
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIdAccuired, this, nakamaMatchId);
      _stateUpdate = State_WaitingForJoin_Update;
    }

    void State_WaitingForJoin_Update()
    {
      if (_matchJoined)
      {
        Debug.LogFormat("Joined match with Id: {0}", nakamaMatchId);
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, nakamaMatchPresences);
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
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, nakamaMatchPresences);
        _matchPresencesUpdated = false;
      }
    }

    // +++ helper for bringing events to the main thread
    private void Enqueue(Action action)
    {
      lock (nakamaEventQueue)
      {
        nakamaEventQueue.Enqueue(ActionWrapper(action));
        if (nakamaEventQueue.Count > 1024)
        {
          Debug.LogWarning("Queued actions not consumed fast enough.");
          client.Disconnect();
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
