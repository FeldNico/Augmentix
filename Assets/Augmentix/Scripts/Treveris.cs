using System.Collections.Generic;
using System.Linq;
using Augmentix.Scripts.AR;
using Photon.Pun;
using UnityEngine;
using Valve.VR.InteractionSystem;
using Player = Photon.Realtime.Player;

namespace Augmentix.Scripts
{
    [RequireComponent(typeof(PhotonView))]
    public class Treveris : MonoBehaviour
    {
        private static Dictionary<int,Treveris> _treveri = new Dictionary<int, Treveris>();
        private static Treveris _currentTreveris = null;

        public Material GroundMaterial;
        
        private PhotonView view;
        // Start is called before the first frame update
        void Start()
        {
            view = GetComponent<PhotonView>();

            if (!view.IsMine && TargetManager.Instance.Type == TargetManager.PlayerType.Primary)
            {
                transform.parent = PickupTarget.Instance.Scaler.transform;
                transform.localScale = Vector3.one;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                gameObject.SetActive(false);
                _treveri.Add(view.OwnerActorNr, this);
            }
            else
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.parent = transform;
                plane.transform.localRotation = Quaternion.identity;
                plane.transform.localPosition = new Vector3(-318, 0, -500);
                plane.transform.localScale = new Vector3(220, 1, 300);
                plane.AddComponent<TeleportArea>();
                plane.GetComponent<Renderer>().material = GroundMaterial;

            }

        }

        public static Treveris GetTreverisByPlayer(Player player)
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
