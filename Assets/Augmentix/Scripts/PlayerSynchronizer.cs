using System.Linq;
using Augmentix.Scripts.AR;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Events;

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
            if (GetComponent<PhotonView>().IsMine)
            {
                GetComponentsInChildren<Transform>().First(transform1 => transform1.gameObject.name.Equals("Cube"))
                    .GetComponent<Renderer>().enabled = false;

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
                        GetComponent<PhotonView>().RPC("OnPickupRPC", GetComponent<PhotonView>().Controller, PhotonNetwork.LocalPlayer.ActorNumber);
                    
                    PickupTarget.Instance.LostPlayer += (player) =>
                        GetComponent<PhotonView>().RPC("OnLostRPC", GetComponent<PhotonView>().Controller, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            InstanceAction?.Invoke(info);
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
