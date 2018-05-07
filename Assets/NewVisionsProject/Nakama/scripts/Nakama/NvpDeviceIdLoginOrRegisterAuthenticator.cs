using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

namespace newvisionsproject.nakama
{

  public class NvpDeviceIdLoginOrRegisterAuthenticator : ICustomAuthenticator
  {
		// +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++	
		INClient _client;




		// +++ constructor ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		public NvpDeviceIdLoginOrRegisterAuthenticator(INClient client){
			_client = client;
		}




		// +++ ICustomAuthenticator implementation ++++++++++++++++++++++++++++++++++++++++++++++++++++
    public void Login(object userData, Action<INSession> successCallback, Action<INError> failCallback)
    {
      
    }

    public void Register(object userData, Action<INSession> successCallback, Action<INError> failCallback)
    {
      
    }
  }
}
