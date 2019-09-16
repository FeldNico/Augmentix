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

        void Start()
        {
            if (GetComponent<PhotonView>().IsMine)
            {
                GetComponentsInChildren<Transform>().First(transform1 => transform1.gameObject.name.Equals("Cube"))
                    .GetComponent<Renderer>().enabled = false;
            }
            else
            {
                if (TangibleTarget.Instance != null)
                {
                    TangibleTarget.Instance.GotPlayer += (player) =>
                        GetComponent<PhotonView>().RPC("OnPickup", GetComponent<PhotonView>().Controller, PhotonNetwork.LocalPlayer.ActorNumber);
                }
            }
        }

        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (InstanceAction != null)
                InstanceAction.Invoke(info);
        }

        [PunRPC]
        public void OnPickup(int primaryActorNumber)
        {
            if (GetComponent<PhotonView>().IsMine)
            {
                Primary = PhotonNetwork.PlayerListOthers.First( (player) =>player.ActorNumber == primaryActorNumber);
            }
        }

        void OnDestroy()
        {
            if (TangibleTarget.Instance != null)
                TangibleTarget.Instance.LostPlayer.Invoke();
        }
    }
}
