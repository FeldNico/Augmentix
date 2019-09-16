using System.Collections.Generic;
using Augmentix.Scripts.AR;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Augmentix.Scripts
{
    [RequireComponent(typeof(PhotonView))]
    public class TreverisView : MonoBehaviourPunCallbacks, IPunObservable
    {
        private static Dictionary<int,TreverisView> _treveri = new Dictionary<int, TreverisView>();
        private static TreverisView _currentTreveris = null;

        // Start is called before the first frame update
        void Start()
        {
            if (!GetComponent<PhotonView>().IsMine)
            {
                transform.parent = PickupTarget.Instance.Scaler.transform;
                transform.localScale = Vector3.one;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                gameObject.SetActive(false);
                _treveri.Add(GetComponent<PhotonView>().OwnerActorNr,this);
            }
            else
            {
                //gameObject.AddComponent<WorldGenerator>();
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            throw new System.NotImplementedException();
        }



        public static TreverisView GetTreverisByPlayer(Player player)
        {
            if (_currentTreveris != null)
            {
                _currentTreveris.gameObject.SetActive(false);
            }

            _currentTreveris = _treveri[player.ActorNumber];
            _currentTreveris.gameObject.SetActive(true);
            return _currentTreveris;
        }

        public static void RemoveTreveris()
        {
            if (_currentTreveris != null)
            {
                _currentTreveris.gameObject.SetActive(false);
            }

            _currentTreveris = null;
        }
    }
}
