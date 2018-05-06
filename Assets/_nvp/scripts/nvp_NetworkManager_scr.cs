using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System;
using System.Linq;
using newvisionsproject.managers.events;

public class nvp_NetworkManager_scr : MonoBehaviour
{

  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  [SerializeField] private nvp_NakamaManager_scr _nakama;

  // +++ values received from nakama server
  private INSession _nakamaSession;
  private INMatch _nakamaMatch;
  private string _nakamaMatchId;
  private List<INUserPresence> _nakamaMatchPresences = new List<INUserPresence>();
  private INUserPresence _self;


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
    

    // Login or Register to Nakama Multiplayer Server;
    _nakama.LoginOrRegister("random_guid", null);
  }

  void Start()
  {
    // initial state - try to connect to server
    _stateUpdate = State_ConnectToServer_Update;

    // subscribe to events
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnCreateGameInitiated, OnCreateGameInitiated);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnJoinGameInitiated, OnJoinGameInitiated);
  }

  void Update()
  {
    _stateUpdate();
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
    if(match != null) {
    _nakamaMatch = match;
      _matchCreated = true;
    }
  }

  private void OnMatchJoined(INResultSet<INMatch> matches)
  {
    if(matches != null) {
      _matchJoined = true;
      _self = matches.Results[0].Self;
      _nakamaMatchPresences.AddRange(matches.Results[0].Presence);
      _nakamaMatchPresences.Remove(_nakamaMatchPresences.Single(x=>x.Handle == _self.Handle));
    }
  }  

  private void OnMatchPresencesUpdated(INMatchPresence presences)
  {
    _matchPresencesUpdated = true;
    foreach(var user in presences.Leave)
    {
      _nakamaMatchPresences.Remove(_nakamaMatchPresences.Single(x=>x.Handle == user.Handle));
    }

    foreach(var user in presences.Join)
    {
       _nakamaMatchPresences.Add(user);
    }
  }




  // +++ other game event handler +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  void OnCreateGameInitiated(object sender, object eventArgs){
    _stateUpdate = State_CreateMatch_Update;
  }

  void OnJoinGameInitiated(object sender , object eventArgs){
    _nakamaMatchId = eventArgs.ToString();
    _stateUpdate = State_JoinMatch_Update;
  }




  // +++ states +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  void State_ConnectToServer_Update()
  {
    if (_connected)
    {
      Debug.LogFormat("Connected to server. Session token: {0}", _nakamaSession.Token);

      // do nothing
      _stateUpdate = () => {};
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
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchIdAccuired, this, _nakamaMatch.Id);
      _stateUpdate = State_WaitingForPlayers_Update;
    }
  }



  // +++ states that handle the joining of an existiong match
  void State_JoinMatch_Update(){
    _nakama.JoinMatch(_nakamaMatchId);
    _stateUpdate = State_WaitingForJoin_Update;
  }

  void State_WaitingForJoin_Update(){
    if(_matchJoined){
      Debug.LogFormat("Joined match with Id: {0}", _nakamaMatchId);
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, _nakamaMatchPresences);
      _stateUpdate = State_WaitingForPlayers_Update;
    }    
  }

  void State_WaitingForPlayers_OnEnter(){
    _stateUpdate = State_WaitingForPlayers_Update;
  }

  void State_WaitingForPlayers_Update()
  {
    if(_matchPresencesUpdated == true){
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMatchPresencesUpdated, this, _nakamaMatchPresences);
      _matchPresencesUpdated = false;
    }
  }
}
