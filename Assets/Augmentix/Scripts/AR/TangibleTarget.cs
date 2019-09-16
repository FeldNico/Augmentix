using Augmentix.Scripts.OOI;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using Vuforia;

namespace Augmentix.Scripts.AR
{
    public class TangibleTarget : MonoBehaviour
    {
        public UnityAction<TrackableBehaviour.Status> OnStatusChange;
        
        public GameObject Current { private set; get; } = null;
        private void Start()
        {
            /*
            Current = PhotonNetwork.Instantiate("Dummy", Vector3.zero, Quaternion.identity);
            Current.transform.parent = transform;
            Current.transform.localPosition = Vector3.zero;
            Current.transform.localScale = Vector3.one;
            Current.transform.localRotation = Quaternion.identity;
            */
        }
    
        private TrackableBehaviour.Status _prevStatus = TrackableBehaviour.Status.NO_POSE;
        // Update is called once per frame
        void Update()
        {
            if (_prevStatus != GetComponent<TrackableBehaviour>().CurrentStatus)
            {
                //OnStatusChange.Invoke(GetComponent<TrackableBehaviour>().CurrentStatus);
                _prevStatus = GetComponent<TrackableBehaviour>().CurrentStatus;
            }

            if (GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                return;

        }
    }
}
