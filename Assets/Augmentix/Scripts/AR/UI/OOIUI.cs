using System;
using System.Collections;
using System.Collections.Generic;
using Augmentix.Scripts.OOI;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vuforia;
using Outline = Augmentix.Scripts.VR.Outline;

namespace Augmentix.Scripts.AR.UI
{
    public class OOIUI : MonoBehaviour
    {
        public static OOIUI Instance = null;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        public float Radius = 100f;

        public Slider Slider;
        public TMP_Dropdown Dropdown;
        public Button Text;
        public Button Video;
        public Button Animation;
        public Button Highlight;

        public OOI.OOI CurrentSelected { private set; get; } = null;

        public UnityAction<OOI.OOI> OnSelect;
        public UnityAction OnDeselect;

        public List<Button> GetButtons(OOI.OOI ooi)
        {
            var flags = ooi.Flags;
            var l = new List<Button>();
            if (flags.HasFlag(OOI.OOI.InteractionFlag.Highlight))
                l.Add(Highlight);
            if (flags.HasFlag(OOI.OOI.InteractionFlag.Animation))
                l.Add(Animation);
            if (flags.HasFlag(OOI.OOI.InteractionFlag.Text))
                l.Add(Text);
            if (flags.HasFlag(OOI.OOI.InteractionFlag.Video))
                l.Add(Video);

            return l;
        }

        private OOI.OOI _target;

        void Start()
        {
            OnSelect += (target) => { _target = target; };

            OnDeselect += () => { _target = null; };

            PickupTarget.Instance.OnStatusChange += status =>
            {
                if (status == TrackableBehaviour.Status.NO_POSE)
                    Deselect();
            };

            PickupTarget.Instance.LostPlayer += player =>  Deselect();

            Highlight.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Highlight);
                Deselect();
            });
            Animation.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Animation);
                Deselect();
            });
            Text.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Text);
                Deselect();
            });
            Video.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Video);
                Deselect();
            });
            Slider.onValueChanged.AddListener(value =>
            {
                CurrentSelected.transform.localScale = new Vector3(value,value,value);
            });
            Dropdown.onValueChanged.AddListener(value =>
            {
                var parent = CurrentSelected.transform.parent;
                PhotonNetwork.Destroy(CurrentSelected.gameObject);
                var go =PhotonNetwork.Instantiate("Cube", Vector3.zero, Quaternion.identity,(byte) PhotonNetwork.LocalPlayer.ActorNumber);
                var scale = go.transform.localScale;
                var position = go.transform.localPosition;
                go.transform.parent = parent;
                go.transform.localScale = scale;
                go.transform.localPosition = position;
                go.transform.localRotation = Quaternion.identity;
            });
            
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
#else
            if (Input.GetMouseButtonDown(0))
            {
#endif
                if (!(EventSystem.current.IsPointerOverGameObject() ||
                      EventSystem.current.currentSelectedGameObject != null))
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    var ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
#else
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(ray, out hit) && hit.transform.GetComponent<OOI.OOI>())
                    {
                        Select(hit.transform.GetComponent<OOI.OOI>());
                    }
                    else
                    {
                        Deselect();
                    }
                }
            }

            SmoothMoveButtons();
        }

        private List<Button> _buttons = null;
        private OOI.OOI _prevtarget = null;
        private Vector3 _center = new Vector3();

        void SmoothMoveButtons()
        {
            if (_target == null)
            {
                if (_prevtarget == null) return;

                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);

                _prevtarget = null;
                return;
            }

            if (_target != _prevtarget)
            {
                if (_prevtarget != null)
                    foreach (Transform child in transform)
                        child.gameObject.SetActive(false);

                _prevtarget = _target;

                _buttons = GetButtons(_target);
                foreach (var button in _buttons)
                    button.gameObject.SetActive(true);

                if (_target.Flags.HasFlag(OOI.OOI.InteractionFlag.Scale))
                {
                    var view = _target.GetComponent<PhotonTransformView>();
                    if (view == null)
                        Debug.LogError("OOI Scale activated but OOI does not have a Transform View for Scale-Synchronization");
                    else if (!view.m_SynchronizeScale)
                        Debug.LogError("OOI Scale activated but Scale-Synchronization in Transform View not activated");
                    else
                    {
                        Slider.gameObject.SetActive(true);
                        var scale = _target.transform.localScale.x;
                        Slider.minValue = scale / 5f;
                        Slider.value = scale;
                        Slider.maxValue = scale * 5f;
                    }
                }
                if (_target.Flags.HasFlag(OOI.OOI.InteractionFlag.Changeable))
                {
                    Dropdown.gameObject.SetActive(true);
                }

                _center = new Vector3();
                var renderers = _target.GetComponentsInChildren<Renderer>();

                if (renderers.Length > 0)
                {
                    foreach (var child in renderers)
                        _center += child.bounds.center;
                    _center = _center / renderers.Length;
                    _center = _center - _target.transform.position;
                }
            }

            if (_target != null)
            {
                var worldCenter = Camera.main.WorldToScreenPoint(_target.transform.position + _center);

                for (var i = 0; i < _buttons.Count; i++)
                {
                    var rectTransform = _buttons[i].GetComponent<RectTransform>();

                    var x = (float) (Radius * Screen.width / 100 * Math.Cos(2 * i * Math.PI / _buttons.Count));
                    var y = (float) (Radius * Screen.height / 100 * Math.Sin(2 * i * Math.PI / _buttons.Count));

                    var sizeDelta = rectTransform.sizeDelta;
                    x = x > 0 ? x + sizeDelta.x / 2 : x - sizeDelta.x / 2;
                    y = y > 0 ? y + sizeDelta.y / 2 : y - sizeDelta.y / 2;


                    rectTransform.transform.position = worldCenter + new Vector3(x, y, 0);
                }

                if (_target.Flags.HasFlag(OOI.OOI.InteractionFlag.Scale))
                {
                    Slider.GetComponent<RectTransform>().transform.position =
                        worldCenter + new Vector3(0, -3 * Radius * Screen.height / 100, 0);
                }
                
                if (_target.Flags.HasFlag(OOI.OOI.InteractionFlag.Changeable))
                {
                    Dropdown.GetComponent<RectTransform>().transform.position =
                        worldCenter + new Vector3(0, 3 * Radius * Screen.height / 100, 0);
                }
            }
        }

        public void Select(OOI.OOI Target)
        {
            Deselect();
            CurrentSelected = Target;
            OnSelect.Invoke(CurrentSelected);
        }

        public void Deselect()
        {
            if (CurrentSelected != null)
            {
                OnDeselect.Invoke();
                CurrentSelected = null;
            }
        }


        private GameObject _prevHighlightTarget = null;

        public void ToggleHighlightTarget(GameObject Target)
        {
            if (_prevHighlightTarget != Target)
            {
                if (_prevHighlightTarget != null)
                    Destroy(_prevHighlightTarget.GetComponent<Outline>());
                _prevHighlightTarget = Target;
            }

            if (Target.GetComponent<Outline>())
                Destroy(Target.GetComponent<Outline>());
            else
                Target.AddComponent<Outline>();
        }
    }
}