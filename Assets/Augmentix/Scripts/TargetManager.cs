using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Hashtable = ExitGames.Client.Photon.Hashtable;
#if UNITY_ANDROID
using Vuforia;
#endif


namespace Augmentix.Scripts
{
    public abstract class TargetManager : MonoBehaviourPunCallbacks
    {


        public enum PlayerType
        {
            Unkown,
            Primary,
            Secondary
        }
    
        public static TargetManager Instance { protected set; get; }

        public const string ROOMNAME = "XRTREVERORUM";

        public PlayerType Type = PlayerType.Unkown;

        public GameObject OOITextPrefab;

        public UnityAction OnConnection;

        protected const string gameVersion = "1";

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            PhotonNetwork.AutomaticallySyncScene = false;
        }

        public void Start()
        {
            if (Type == PlayerType.Unkown)
            {
                Debug.LogError("No Classname set!");
                return;
            }
            
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        public override void OnConnectedToMaster()
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable {{"Class", Type.ToString()}});
            PhotonNetwork.JoinOrCreateRoom(ROOMNAME, new RoomOptions {MaxPlayers = 0}, TypedLobby.Default);
        }

        public override void OnJoinedRoom()
        {
            StartCoroutine(OnConnect());

            IEnumerator OnConnect()
            {
                yield return new WaitForSeconds(0.5f);
                OnConnection.Invoke();
            }
        }
    }
}