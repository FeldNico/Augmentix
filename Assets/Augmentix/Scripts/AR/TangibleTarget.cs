using System;
using Augmentix.Scripts.OOI;
using Photon.Pun;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_ANDROID
using System.Collections.Generic;
using Augmentix.Scripts.AR.UI;
using Vuforia;
#endif

namespace Augmentix.Scripts.AR
{
    #if UNITY_ANDROID
    public class TangibleTarget : MonoBehaviour
    {
        public static List<TangibleTarget> AllTangibles { private set; get; } = new List<TangibleTarget>();
        
        public UnityAction<TrackableBehaviour.Status> OnStatusChange;
        public GameObject Current { private set; get; } = null;
        
        private TrackableBehaviour _trackableBehaviour;

        private void Awake()
        {
            _trackableBehaviour = GetComponent<TrackableBehaviour>();
            AllTangibles.Add(this);
        }

        private void Start()
        {
            var targetManager = (AndroidTargetManager) TargetManager.Instance;
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
            if (_prevStatus != _trackableBehaviour.CurrentStatus)
            {
                OnStatusChange.Invoke(_trackableBehaviour.CurrentStatus);
                _prevStatus = _trackableBehaviour.CurrentStatus;
            }

            if (_trackableBehaviour.CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                return;

        }
    }
#endif
}
