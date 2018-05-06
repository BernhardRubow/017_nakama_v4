using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Nakama
{
  public interface ICustomAuthenticator 
  {   
    void Login(object userData, System.Action<INSession> successCallback, System.Action<INError>failCallback);

    void Register(object userData, System.Action<INSession> successCallback, System.Action<INError>failCallback);
  }
}
