using System;
using System.Linq;
using Augmentix.Scripts.AR;
using Augmentix.Scripts.AR.UI;
using Augmentix.Scripts.VR;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;
#if UNITY_ANDROID
using Vuforia;
#endif

namespace Augmentix.Scripts
{
    public class PlayerSynchronizer : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
    {
        public static UnityAction<PhotonMessageInfo> InstanceAction;

        public Player Primary { private set; get; } = null;

        public UnityAction<Player> OnLost;
        public UnityAction<Player> OnPickup;

        private LineRenderer lineHeight;
        private LineRenderer linePickup;
        
        #if UNITY_ANDROID
        private TrackableBehaviour _trackable;
        #endif

        void Start()
        {
            if (photonView.IsMine)
            {
                /*
                GetComponentsInChildren<Transform>().First(transform1 => transform1.gameObject.name.Equals("Cube"))
                    .GetComponent<Renderer>().enabled = false;
                */
                foreach (var child in GetComponentsInChildren<Transform>())
                {
                    if (child.GetComponent<Renderer>())
                        child.GetComponent<Renderer>().enabled = false;

                    if (child.GetComponent<Collider>())
                        child.GetComponent<Collider>().enabled = false;
                }

#if UNITY_STANDALONE
                if (OpenVR.Input != null)
                    ((StandaloneTargetManager) TargetManager.Instance).Help?.AddOnStateUpListener((action, source) =>
                    {
                        photonView.RPC("OnHelpRPC",RpcTarget.Others);
                    },SteamVR_Input_Sources.RightHand);

#endif

                OnPickup += (player) =>
                {
                    Primary = player;
                    PhotonNetwork.SetInterestGroups((byte) player.ActorNumber, true);
                };

                OnLost += (player) =>
                {
                    Primary = null;
                    PhotonNetwork.SetInterestGroups(0, true);
                };
            }
            else if (StandaloneTargetManager.Instance.Type == TargetManager.PlayerType.Primary)
            {
                lineHeight = gameObject.AddComponent<LineRenderer>();
                lineHeight.widthMultiplier = 0.001f;
                lineHeight.material = OOIUI.Instance.HeightLineMaterial;
                
                var dummy = new GameObject("Dummy");
                dummy.transform.parent = transform;
                linePickup = dummy.AddComponent<LineRenderer>();
                linePickup.widthMultiplier = 0.001f;
                linePickup.material = OOIUI.Instance.HeightLineMaterial;
                

                #if UNITY_ANDROID
                _trackable = MapTarget.Instance.GetComponent<TrackableBehaviour>();

                PickupTarget.Instance.GotPlayer += (player) =>
                {
                    photonView.RPC("OnPickupRPC", GetComponent<PhotonView>().Controller,
                        PhotonNetwork.LocalPlayer.ActorNumber);
                    _trackable = PickupTarget.Instance.GetComponent<TrackableBehaviour>();
                };

                PickupTarget.Instance.LostPlayer += (player) =>
                {
                    photonView.RPC("OnLostRPC", GetComponent<PhotonView>().Controller,
                        PhotonNetwork.LocalPlayer.ActorNumber);
                    _trackable = MapTarget.Instance.GetComponent<TrackableBehaviour>();
                };
                #endif
            }
        }

#if UNITY_ANDROID
        public void Update()
        {
            if (lineHeight != null)
            {
                if (_trackable.CurrentStatus != TrackableBehaviour.Status.NO_POSE)
                {
                    var pos = _trackable.transform.InverseTransformPoint(transform.position);
                    pos.y = 0f;
                    lineHeight.SetPosition(0, transform.position - new Vector3(0f, 0.001f, 0f));
                    lineHeight.SetPosition(1, _trackable.transform.TransformPoint(pos));
                }
            }

            if (linePickup != null)
            {
                if (PickupTarget.Instance.Current != this &&
                    PickupTarget.Instance.GetComponent<TrackableBehaviour>().CurrentStatus ==
                    TrackableBehaviour.Status.TRACKED)
                {
                    linePickup.enabled = true;
                    linePickup.SetPosition(0,transform.position);
                    linePickup.SetPosition(1,PickupTarget.Instance.transform.position);
                }
                else
                {
                    linePickup.enabled = false;
                }
            }
        }
#endif

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            InstanceAction?.Invoke(info);
        }

        [PunRPC]
        public void OnHelpRPC()
        {
            Debug.Log("Gondor " + photonView.Owner.ActorNumber + " calls for aid!");
        }

        [PunRPC]
        public void OnPickupRPC(int primaryActorNumber)
        {
            var player = PhotonNetwork.PlayerList.First(p => p.ActorNumber == primaryActorNumber);
            OnPickup.Invoke(player);
        }

        [PunRPC]
        public void OnLostRPC(int primaryActorNumber)
        {
            var player = PhotonNetwork.PlayerList.First(p => p.ActorNumber == primaryActorNumber);
            OnLost.Invoke(player);
        }

        void OnDestroy()
        {
            if (PickupTarget.Instance != null)
                PickupTarget.Instance.LostPlayer.Invoke(gameObject);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            base.OnPlayerLeftRoom(otherPlayer);
            if (Equals(otherPlayer, Primary))
            {
                OnLost.Invoke(Primary);
            }
        }
    }
}