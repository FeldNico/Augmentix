using System.Linq;
using Augmentix.Scripts.AR;
using Augmentix.Scripts.VR;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;
using Valve.VR;

namespace Augmentix.Scripts
{
    public class PlayerSynchronizer : MonoBehaviourPunCallbacks, IPunInstantiateMagicCallback
    {
        public static UnityAction<PhotonMessageInfo> InstanceAction;

        public Player Primary { private set; get; } = null;

        public UnityAction<Player> OnLost;
        public UnityAction<Player> OnPickup;

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
                
                ((StandaloneTargetManager) TargetManager.Instance).Help.AddOnStateUpListener((action, source) =>
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
            else
            {
                if (PickupTarget.Instance != null)
                {
                    PickupTarget.Instance.GotPlayer += (player) =>
                        photonView.RPC("OnPickupRPC", GetComponent<PhotonView>().Controller, PhotonNetwork.LocalPlayer.ActorNumber);
                    
                    PickupTarget.Instance.LostPlayer += (player) =>
                        photonView.RPC("OnLostRPC", GetComponent<PhotonView>().Controller, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            InstanceAction?.Invoke(info);
        }

        [PunRPC]
        public void OnHelpRPC()
        {
            Debug.Log("Gondor "+photonView.Owner.ActorNumber+" calls for aid!");
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
