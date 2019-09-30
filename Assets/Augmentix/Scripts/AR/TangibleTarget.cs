using Augmentix.Scripts.OOI;
using Photon.Pun;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_ANDROID
using Vuforia;
#endif

namespace Augmentix.Scripts.AR
{
    #if UNITY_ANDROID
    public class TangibleTarget : MonoBehaviour
    {
        public UnityAction<TrackableBehaviour.Status> OnStatusChange;
        public GameObject Current { private set; get; } = null;
        private void Start()
        {
            TargetManager.Instance.OnConnection += () =>
            {
                AddOOI("Tangibles/Empty");
                Current.SetActive(false);
            };
            
            PickupTarget.Instance.LostPlayer += player =>
            {
                if (Current)
                {
                    PhotonNetwork.Destroy(Current);
                    AddOOI("Tangibles/Empty");
                    Current.SetActive(false);
                }
            };

            OnStatusChange += status =>
            {
                if (Current == null)
                    return;
                
                if (status != TrackableBehaviour.Status.NO_POSE)
                {
                    Current.SetActive(true);
                }
                else
                {
                    Current.SetActive(false);
                }
            };
        }

        public void AddOOI(string name)
        {
            var go =PhotonNetwork.Instantiate(name, Vector3.zero, Quaternion.identity,(byte) PhotonNetwork.LocalPlayer.ActorNumber);
            var scale = go.transform.localScale;
            go.transform.parent = transform;
            go.transform.localScale = scale;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Current = go;
        }
    
        private TrackableBehaviour.Status _prevStatus = TrackableBehaviour.Status.NO_POSE;
        // Update is called once per frame
        void Update()
        {
            if (_prevStatus != GetComponent<TrackableBehaviour>().CurrentStatus)
            {
                OnStatusChange.Invoke(GetComponent<TrackableBehaviour>().CurrentStatus);
                _prevStatus = GetComponent<TrackableBehaviour>().CurrentStatus;
            }

            if (GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                return;

        }
    }
#endif
}
