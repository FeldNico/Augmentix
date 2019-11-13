using System;
using Augmentix.Scripts.OOI;
using Photon.Pun;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
#if UNITY_ANDROID
using Augmentix.Scripts.AR.UI;
using Vuforia;
#endif

namespace Augmentix.Scripts.AR
{
   
    public class TangibleTarget : MonoBehaviour
    {
        public static List<TangibleTarget> AllTangibles { private set; get; } = new List<TangibleTarget>();

       
        public GameObject Current { private set; get; } = null;
        public GameObject Scaler { private set; get; } = null;
        
#if UNITY_ANDROID   
         public UnityAction<TrackableBehaviour.Status> OnStatusChange;
        private TrackableBehaviour _trackableBehaviour;

        private void Awake()
        {
            _trackableBehaviour = GetComponent<TrackableBehaviour>();
            AllTangibles.Add(this);
        }

        private void Start()
        {
            var targetManager = (AndroidTargetManager) TargetManager.Instance;
            
            Scaler = new GameObject("Scaler");
            Scaler.transform.parent = transform;
            Scaler.transform.localPosition = Vector3.zero;
            Scaler.transform.localScale = new Vector3(1, 1, 1);
            
            TargetManager.Instance.OnConnection += () =>
            {
                AddOOI("Tangibles/"+targetManager.EmptyTangible.name);
                Current.SetActive(false);
            };
            
            PickupTarget.Instance.LostPlayer += player =>
            {
                if (Current)
                {
                    PhotonNetwork.Destroy(Current);
                    AddOOI("Tangibles/"+targetManager.EmptyTangible.name);
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
            go.transform.parent = Scaler.transform;
            go.transform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            Current = go;
        }
    
        private TrackableBehaviour.Status _prevStatus = TrackableBehaviour.Status.NO_POSE;
        // Update is called once per frame
        void Update()
        {
            if (_prevStatus != _trackableBehaviour.CurrentStatus)
            {
                OnStatusChange.Invoke(_trackableBehaviour.CurrentStatus);
                _prevStatus = _trackableBehaviour.CurrentStatus;
            }

            if (_trackableBehaviour.CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                return;

        }
#endif
    }
}
