using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using newvisionsproject.managers.events;
using System;
using Nakama;

public class nvp_UiManager_scr : MonoBehaviour {

	// +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	[Header("SPECIAL UI ELEMENTS")]
	[SerializeField] private InputField _matchId;	
	[SerializeField] private InputField _matchIdToJoin_01;
	[SerializeField] private InputField _matchIdToJoin_02;
	[SerializeField] private Text _playerLog;
	[SerializeField] private Button _startGameButton;

	[Header("UI FOR DIFFERENT STATES")]
	[SerializeField] private GameObject _startGameUI;
	[SerializeField] private GameObject _createNetworkGameUI;
	[SerializeField] private GameObject _joinNetworkGameUI;
	[SerializeField] private GameObject _waitingForPlayersUI;


	// Use this for initialization
	void Start () {
		// subscribe from events
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnMatchIdAccuired, OnMatchIdAccuired);
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnMatchPresencesUpdated, OnMatchPresencesUpdated);
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnMatchIsReady, OnMatchIsReady);

		// reset ui
		_startGameUI.SetActive(true);
		_createNetworkGameUI.SetActive(false);
		_joinNetworkGameUI.SetActive(false);
		_waitingForPlayersUI.SetActive(false);

	}




  // +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void OnMatchIdAccuired(object sender, object eventArgs)
  {
    _matchId.text = eventArgs.ToString();
		_matchIdToJoin_02.text = _matchId.text;
  }

	private void OnCreateGameSelected(){
		// inform all listeners that we want to create a game
		nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnCreateGameInitiated, this, null);
		_startGameUI.SetActive(false);
		_createNetworkGameUI.SetActive(false);
		_waitingForPlayersUI.SetActive(true);
	}

	private void OnJoinGameSelected(){		
		_startGameUI.SetActive(false);
		_joinNetworkGameUI.SetActive(true);
	}

	private void OnJoinGameClicked(){
		var id = _matchIdToJoin_01.text;
		_matchIdToJoin_02.text = id;
		_joinNetworkGameUI.SetActive(false);
		_waitingForPlayersUI.SetActive(true);
		nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnJoinGameInitiated, this, id);
	}

	private void OnMatchPresencesUpdated(object sender, object eventArgs){
		var presences = (List<INUserPresence>)eventArgs;

		_playerLog.text += "";
		_playerLog.text += "Playerlistupdate \n";

		foreach(var user in presences)
		{	
			_playerLog.text += string.Format("Player with handle {0}.\n", user.Handle);
		}
	}

	private void OnMatchIsReady(object sender, object eventArgs){
		_startGameButton.gameObject.SetActive(true);
	}

	public void OnStartMultiPlayerGame(){
		nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnMultiplayerGameStarted, this, null);

		// unsubscribe from events
		nvp_EventManager_scr.INSTANCE.UnsubscribeFromEvent(GameEvents.OnMatchIdAccuired, OnMatchIdAccuired);
		nvp_EventManager_scr.INSTANCE.UnsubscribeFromEvent(GameEvents.OnMatchPresencesUpdated, OnMatchPresencesUpdated);
		nvp_EventManager_scr.INSTANCE.UnsubscribeFromEvent(GameEvents.OnMatchIsReady, OnMatchIsReady);

		Destroy(this.gameObject);
	}
}
