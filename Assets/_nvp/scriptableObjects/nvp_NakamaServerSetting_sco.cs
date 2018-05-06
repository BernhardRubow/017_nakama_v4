using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NakamaServerSettings", menuName = "New Visions Project/Nakama/Settings", order = 1)]
public class nvp_NakamaServerSetting_sco : ScriptableObject {

	[Header("NAKAMA SERVER SETTINGS")]	
	public string serverKey;
	public string hostName;
	public uint port;
	public bool ssl;

}
