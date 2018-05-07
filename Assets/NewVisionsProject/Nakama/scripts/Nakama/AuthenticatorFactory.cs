using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

namespace newvisionsproject.nakama {

	public class AuthenticatorFactory{

		// +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		INClient _client;




		// +++ constructor ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public AuthenticatorFactory(INClient client){
			_client = client;
		}

		public ICustomAuthenticator GetAuthenticator(string authenticatorType){
			switch(authenticatorType){
				case "random_guid":
					return new NvpRandomGuidLoginOrRegisterAuthenticator(_client);
				
				case "device_id":
					return null;

				case "email":
					return null;
				
				default:
					return null;
			}
		}
	}
}
