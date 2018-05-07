using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

using newvisionsproject.managers.events;

namespace newvisionsproject.nakama.examples
{
	public class SimpleChatManager : MonoBehaviour {

		// +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		[SerializeField] private UnityEngine.UI.InputField _message;			// 
		[SerializeField] private UnityEngine.UI.Text _messageLog;
		nvp_NetworkManager_scr _networkManager;
		string messageToDisplay = "";
		bool msgWaiting;




		// +++ unity callbacks ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		void Awake()
		{
			// subscribe to important events
			nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnNetworkManagerInitialised, OnNetworkManagerInitialised);	
			nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.OnGameMessageReceived, OnGameMessageReceived);			
		}		
		
		void Update () 
		{
			// used to display messages received on another thread different from the main thread
			if(msgWaiting){
				msgWaiting = false;
				_messageLog.text = messageToDisplay + "\n" + _messageLog.text;
			}
		}




		// +++ Event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++	
		void OnGameMessageReceived(object sender, object eventArgs)
		{
			// decode message
			var msg = eventArgs as INMatchData;
			messageToDisplay = "REMOTE :" + System.Text.Encoding.UTF8.GetString(msg.Data);

			// flag that a new message is ready to be displayed
			msgWaiting = true;
		}

		void OnNetworkManagerInitialised(object sender, object eventArgs)
		{
			// reveive a reference to the network manager
			_networkManager = eventArgs as nvp_NetworkManager_scr;
		}

		public void OnSendClicked(){
			// read input field
			string content = _message.text;
			_message.text = "";

			// diplay you own message
			messageToDisplay = "YOU: " + content;
			msgWaiting = true;

			// encode string to byte array
			byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
			var msg = NMatchDataSendMessage.Default(
				_networkManager.nakamaMatchId,
				0L,
				data);

			// send the message to all other clients
			_networkManager.client.Send(
				msg,
				(bool done) => { Debug.Log("Message send"); },
				(INError error) => { Debug.Log("Error on send message"); }
			);
		}
	}
}
