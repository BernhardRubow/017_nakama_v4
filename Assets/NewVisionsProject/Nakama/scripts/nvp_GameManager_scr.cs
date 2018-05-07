using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using newvisionsproject.managers.events;
using newvisionsproject.nakama;
using System;

public class nvp_GameManager_scr : MonoBehaviour {

	// +++ inspector fiels ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	[SerializeField] private nvp_NetworkManager_scr _networkManager;
	[SerializeField] private string _matchId;

	

	// Use this for initialization
	void Start () {
		_networkManager = GameObject.Find("networkManager").GetComponent<nvp_NetworkManager_scr>();		
    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnNetworkManagerInitialised, this, _networkManager);

		// subscribe to events
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnCreateGameInitiated, OnCreateGameInitiated);
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnJoinGameInitiated, OnJoinGameInitiated);
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnMatchIdAccuired, OnMatchIdAccuired);

	}

  // Update is called once per frame
  void Update () {
		
	}

	// +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void OnCreateGameInitiated(object sender, object eventArgs)
  {
    if(_networkManager != null) _networkManager.CreateGame();
  }

  private void OnJoinGameInitiated(object sender, object eventArgs)
  {
    string matchId = eventArgs.ToString();
		if(_networkManager != null) _networkManager.JoinGame(matchId);
  }

  private void OnMatchIdAccuired(object sender, object eventArgs)
  {
    _matchId = eventArgs.ToString();		
  }
	
}
