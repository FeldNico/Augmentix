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
        public PlayerSynchronizer PlayerSync { private set; get; }
        public GameObject TreverisPrefab;
        public float Scale;
        public UnityAction<GameObject> GotPlayer;
        public UnityAction<GameObject> LostPlayer;
        
        public GameObject Scaler { private set; get; }

        private Treveris _treveris;
        private MapTarget _mapTarget;
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
            
            _mapTarget = FindObjectOfType<MapTarget>();
            Scaler = new GameObject("Scaler");
            Scaler.transform.parent = transform;
            Scaler.transform.localPosition = Vector3.zero;
            Scaler.transform.localScale = new Vector3(Scale, Scale, Scale);

            var scaleSlicer = ARUI.Instance.ScaleSlider;
            scaleSlicer.minValue = Scale / 5f;
            scaleSlicer.value = Scale;
            scaleSlicer.maxValue = Scale * 5f;

            scaleSlicer.onValueChanged.AddListener(scale =>
            {
                Scaler.transform.localScale = new Vector3(scale, scale, scale);
            });

            ARUI.Instance.RemovePlayer.onClick.AddListener(() =>
            {
                LostPlayer.Invoke(PlayerSync.gameObject);

                _locked = true;

                var t = PlayerSync.transform;
                t.parent = _mapTarget.Scaler.transform;
                t.localPosition = Vector3.zero;

                PlayerSync = null;
                Treveris.RemoveTreveris();
                _treveris = null;

                StartCoroutine(LockTimer());

                IEnumerator LockTimer()
                {
                    yield return new WaitForSeconds(TransitionTime * 2);
                    _locked = false;
                }
            });
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

            if (PlayerSync == null && !_locked )
            {
                foreach (var synchronizer in _mapTarget.GetComponentsInChildren<PlayerSynchronizer>())
                {
                    if (Vector3.Distance(transform.position, synchronizer.transform.position) < PickupDistance && _mapTarget.GetComponent<TrackableBehaviour>().CurrentStatus != TrackableBehaviour.Status.NO_POSE && GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.TRACKED)
                    {
                        PlayerSync = synchronizer;
                        StartCoroutine(AnimateTransition());
                    }
                }
            }

            if (PlayerSync != null && !_locked && ARUI.Instance.LockCam.isOn)
            {
                if (_treveris != null)
                {
                    var t = _treveris.transform;
                    var localPosition = PlayerSync.transform.localPosition;
                    t.localPosition = t.localScale.x * -new Vector3(localPosition.x,0,localPosition.z);
                }
            }

            
            if (_treveris != null && PlayerSync != null)
            {
                foreach (Transform child in _treveris.transform)
                {
                    if (Vector3.Distance(child.position, PlayerSync.transform.position) > ViewDistance)
                    {
                        foreach (var inChild in child.GetComponentsInChildren<Renderer>())
                        {
                            //inChild.enabled = false;
                        }
                    }
                }

                /*
                foreach (var childRenderer in _treveris.GetComponentsInChildren<Renderer>())
                {
                    
                    if (Vector3.Distance(childRenderer.bounds.ClosestPoint(PlayerSync.transform.position), PlayerSync.transform.position) > ViewDistance)
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

                _treveris = Treveris.GetTreverisByPlayer(PlayerSync.GetComponent<PhotonView>().Owner);
                PlayerSync.transform.parent = _treveris.transform;

                var elapsedTime = 0f;
                var startingPos = PlayerSync.transform.position;
                var startingScale = PlayerSync.transform.localScale;

                while (elapsedTime < TransitionTime)
                {
                    if (PlayerSync == null)
                    {
                        _locked = false;
                        yield break;
                    }

                    PlayerSync.transform.position = Vector3.Lerp(startingPos, transform.position, (elapsedTime / TransitionTime));
                    PlayerSync.transform.localScale = Vector3.Lerp(startingScale, Vector3.one, (elapsedTime / TransitionTime));
                    elapsedTime += Time.deltaTime;
                    yield return new WaitForEndOfFrame();
                }

                if (PlayerSync != null)
                {
                    PlayerSync.transform.localPosition = Vector3.zero;
                    PlayerSync.transform.localScale = Vector3.one;
                }

                GotPlayer.Invoke(PlayerSync.gameObject);

                _locked = false;
            }

        }
#endif
    }

}
