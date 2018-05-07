using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;

namespace newvisionsproject.nakama
{
  public class NvpRandomGuidLoginOrRegisterAuthenticator : ICustomAuthenticator
  {
    // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++	
    INClient _client;




    // +++ constructor ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    public NvpRandomGuidLoginOrRegisterAuthenticator(INClient client)
    {
      _client = client;
    }




    // +++ ICustomAuthenticator implementation ++++++++++++++++++++++++++++++++++++++++++++++++++++
    public void Login(object userData, Action<INSession> successCallback, Action<INError> failCallback)
    {
      string id;
      if (userData == null)
      {
        id = System.Guid.NewGuid().ToString();
      }
      else
      {
        if (userData.GetType() != typeof(Guid)) throw new ArgumentException("Userdata has to be typeof (Guid).");
        id = ((Guid)userData).ToString();
      }

      var authenticationMessage = NAuthenticateMessage.Device(id);
      _client.Login(authenticationMessage,
        successCallback,
        (INError error) =>
        {
          if (error.Code == ErrorCode.UserNotFound)
          {
            _client.Register(
              authenticationMessage,
              successCallback,
              failCallback
            );
          }
          else{
            failCallback(error);
          }
        }
      );
    }

    public void Register(object userData, Action<INSession> successCallback, Action<INError> failCallback)
    {
      string id;
      if(userData == null){
        id = Guid.NewGuid().ToString();
      }
      else
      {
        if (userData.GetType() != typeof(Guid)) throw new ArgumentException("Userdata has to be typeof (Guid).");
        id = ((Guid)userData).ToString();
      }

      var authenticationMessage = NAuthenticateMessage.Device(id);
      _client.Register(
        authenticationMessage,
        successCallback,
        failCallback
      );
    }
  }
}
