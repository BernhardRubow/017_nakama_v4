using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nakama;
using System;

public class nvp_NakamaManager_scr : MonoBehaviour
{

  // +++ delegates ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public delegate void OnConnectDelegate(INSession currentSession);
  public delegate void OnCreateMatchDelegate(INMatch match);
  public delegate void OnJoinMatchDelegate(INResultSet<INMatch> matches);
  public delegate void OnMatchPresencesDelegate(INMatchPresence presences);



  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public nvp_NakamaServerSetting_sco serverSettings;
  public INClient _client;




  // +++ events +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public event OnConnectDelegate OnConnected;
  public event OnCreateMatchDelegate OnMatchCreated;
  public event OnJoinMatchDelegate OnMatchJoined;
  public event OnMatchPresencesDelegate OnMatchPresencesUpdated;




  // +++ functions ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public void LoginOrRegister(string loginType, object userData)
  {
    _client = new NClient
        .Builder(serverSettings.serverKey)
        .Host(serverSettings.hostName)
        .Port(serverSettings.port)
        .SSL(serverSettings.ssl)
        .Build();

    // register a central point to handle events change the consistence of the
    // players in the match
    _client.OnMatchPresence += OnMatchPresence;

    AuthenticatorFactory authenticatorFactory = new AuthenticatorFactory(_client);
    ICustomAuthenticator authenticator = authenticatorFactory.GetAuthenticator(loginType);

    authenticator.Login(
      userData,
      (INSession session) =>
      {
        _client.Connect(session, (bool done) =>
        {
          if (OnConnected != null) OnConnected(session);
        });
      },
      (INError error) =>
      {
        if (OnConnected != null) OnConnected(null);
      }
    );
  }




  // +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void OnMatchPresence(INMatchPresence presences)
  {
    OnMatchPresencesUpdated(presences);
  }




  // +++ public functions +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public void CreateMatch()
  {
    var createMatchMessage = NMatchCreateMessage.Default();
    _client.Send(
      createMatchMessage,
      (INMatch match) => { if (OnMatchCreated != null) OnMatchCreated(match); },
      (INError error) => { if (OnMatchCreated != null) OnMatchCreated(null); }
    );
  }

  public void JoinMatch(string nakamaMatchId)
  {
    var joinMatchMessage = NMatchJoinMessage.Default(nakamaMatchId);
    _client.Send(
      joinMatchMessage,
      (INResultSet<INMatch> matches) => {
        if (OnMatchJoined != null) OnMatchJoined(matches); 
      },
      (INError Error) => { 
        if (OnMatchJoined != null) OnMatchJoined(null); 
      }
    );
  }
}
