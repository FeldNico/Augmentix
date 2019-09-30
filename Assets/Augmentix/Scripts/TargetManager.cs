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

        private class CustomPhunPool : IPunPrefabPool
        {
            private readonly Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        
            public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
            {
                GameObject res = null;
                bool cached = ResourceCache.TryGetValue(prefabId, out res);
                if (!cached)
                {
                    res = (GameObject)Resources.Load(prefabId, typeof(GameObject));
                    if (res == null)
                    {
                        Debug.LogError("DefaultPool failed to load \"" + prefabId + "\" . Make sure it's in a \"Resources\" folder.");
                    }
                    else
                    {
                        ResourceCache.Add(prefabId, res);
                    }
                }

                bool wasActive = res.activeSelf;
                if (wasActive) res.SetActive(false);

                GameObject instance =GameObject.Instantiate(res, position, rotation) as GameObject;

                if (wasActive) res.SetActive(true);
                return instance;
            }

            public void Destroy(GameObject gameObject)
            {
                Destroy(gameObject);
            }
        }
    
    
        public enum PlayerType
        {
            Unkown,
            Primary,
            Secondary
        }
    
        public static TargetManager Instance { protected set; get; }

        public const string ROOMNAME = "XRTREVERORUM";

        public PlayerType Type = PlayerType.Unkown;

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


#if UNITY_ANDROID
            CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
#endif
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