using System.Collections;
using Augmentix.Scripts.AR.UI;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_ANDROID
using Vuforia;
#endif

namespace Augmentix.Scripts.AR
{

    public class PickupTarget : MonoBehaviour
    {
        public static PickupTarget Instance = null;

        public float PickupDistance = 0.02f;
        public float TransitionTime = 1;
        public float ViewDistance = 0.75f;
        public PlayerSynchronizer Current;
        public GameObject TreverisPrefab;
        public float Scale;
        public UnityAction<GameObject> GotPlayer;
        public UnityAction<GameObject> LostPlayer;
        
        public GameObject Scaler { private set; get; }

        private Treveris _treveris;
        private bool _locked = false;
#if UNITY_ANDROID  
        private TrackableBehaviour _trackableBehaviour;
        public UnityAction<TrackableBehaviour.Status> OnStatusChange;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

            _trackableBehaviour = GetComponent<TrackableBehaviour>();
        }

        // Start is called before the first frame update
        void Start()
        {
            TargetManager.Instance.OnConnection += () =>
            {
                PhotonNetwork.SetInterestGroups((byte) PhotonNetwork.LocalPlayer.ActorNumber, true);
            };
            
            Scaler = new GameObject("Scaler");
            Scaler.transform.parent = transform;
            Scaler.transform.localPosition = Vector3.zero;
            Scaler.transform.localScale = new Vector3(Scale, Scale, Scale);

            LostPlayer += (player) =>
            {
                var t = Current.transform;
                t.parent = MapTarget.Instance.Scaler.transform;
                t.localPosition = Vector3.zero;
                Current = null;

                StartCoroutine(removeTreverisNextFrame());
                IEnumerator removeTreverisNextFrame()
                {
                    yield return new WaitForEndOfFrame();
                    Treveris.RemoveTreveris();
                    _treveris = null;
                }
                
            };
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

            if (Current == null && !_locked )
            {
                foreach (var synchronizer in MapTarget.Instance.GetComponentsInChildren<PlayerSynchronizer>())
                {
                    if (Vector3.Distance(transform.position, synchronizer.transform.position) < PickupDistance && MapTarget.Instance.GetComponent<TrackableBehaviour>().CurrentStatus != TrackableBehaviour.Status.NO_POSE && GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
                    {
                        Current = synchronizer;
                        StartCoroutine(AnimateTransition());
                    }
                }
            }

            if (Current != null && !_locked && ARUI.Instance.LockCam.isOn)
            {
                if (_treveris != null)
                {
                    var t = _treveris.transform;
                    var localPosition = Current.transform.localPosition;
                    t.localPosition = t.localScale.x * -new Vector3(localPosition.x,0,localPosition.z);
                }
            }

            
            if (_treveris != null && Current != null)
            {
                /*
                foreach (Transform child in _treveris.transform)
                {
                    if (Vector3.Distance(child.position, Current.transform.position) > ViewDistance)
                    {
                        foreach (var inChild in child.GetComponentsInChildren<Renderer>())
                        {
                            inChild.enabled = false;
                        }
                    }
                }

                
                foreach (var childRenderer in _treveris.GetComponentsInChildren<Renderer>())
                {
                    
                    if (Vector3.Distance(childRenderer.bounds.ClosestPoint(Current.transform.position), Current.transform.position) > ViewDistance)
                    {
                        if (childRenderer.enabled)
                            childRenderer.enabled = false;
                    }
                    else
                    {
                        if (!childRenderer.enabled)
                            childRenderer.enabled = true;
                    }
                }
                */
            }
            

            IEnumerator AnimateTransition()
            {
                _locked = true;

                _treveris = Treveris.GetTreverisByPlayer(Current.GetComponent<PhotonView>().Owner);
                Current.transform.parent = _treveris.transform;

                var elapsedTime = 0f;
                var startingPos = Current.transform.position;
                var startingScale = Current.transform.localScale;

                while (elapsedTime < TransitionTime)
                {
                    if (Current == null)
                    {
                        _locked = false;
                        yield break;
                    }

                    Current.transform.position = Vector3.Lerp(startingPos, transform.position, (elapsedTime / TransitionTime));
                    Current.transform.localScale = Vector3.Lerp(startingScale, Vector3.one, (elapsedTime / TransitionTime));
                    elapsedTime += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }

                if (Current != null)
                {
                    Current.transform.localPosition = Vector3.zero;
                    Current.transform.localScale = Vector3.one;
                }

                GotPlayer.Invoke(Current.gameObject);

                _locked = false;
            }

        }
#endif
    }

}
