using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Augmentix.Scripts.OOI;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Outline = Augmentix.Scripts.VR.Outline;
#if UNITY_ANDROID
using Vuforia;
#endif

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

#if UNITY_ANDROID
        void Start()
        {

            PickupTarget.Instance.OnStatusChange += status =>
            {
                if (status == TrackableBehaviour.Status.NO_POSE)
                    Deselect();
            };

            PickupTarget.Instance.LostPlayer += player =>
            {
                Deselect();
            };

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
                var current = CurrentSelected;
                Deselect();
                var parent = current.transform.parent;
                PhotonNetwork.Destroy(current.gameObject);
                parent.GetComponent<TangibleTarget>().AddOOI("Tangibles/"+Dropdown.options[value].text);
            });
            
            var options = ((AndroidTargetManager) AndroidTargetManager.Instance).TangiblePrefabs.Select(o => o.name)
                .ToList();
            Dropdown.AddOptions(options);
            
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
                    if (Physics.Raycast(ray, out hit))
                    {
                        var transform = hit.transform;
                        var ooi = transform.GetComponent<OOI.OOI>()
                            ? transform.GetComponent<OOI.OOI>()
                            : transform.GetComponentInParent<OOI.OOI>();
                        
                        if (ooi != null)
                            Select(ooi);
                        else
                            Deselect();
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
            if (CurrentSelected == null)
            {
                if (_prevtarget == null) return;

                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);

                _prevtarget = null;
                return;
            }

            if (CurrentSelected != _prevtarget)
            {
                if (_prevtarget != null)
                    foreach (Transform child in transform)
                        child.gameObject.SetActive(false);

                _prevtarget = CurrentSelected;

                _buttons?.Clear();
                _buttons = GetButtons(CurrentSelected);
                
                if (CurrentSelected.Flags.HasFlag(OOI.OOI.InteractionFlag.Scale))
                {
                    Slider.gameObject.SetActive(true);
                    var scale = CurrentSelected.transform.localScale.x;
                    Slider.minValue = scale / 5f;
                    Slider.value = scale;
                    Slider.maxValue = scale * 5f;
                }
                if (CurrentSelected.Flags.HasFlag(OOI.OOI.InteractionFlag.Changeable))
                {
                    Dropdown.gameObject.SetActive(true);
                }

                _center = new Vector3();
                var renderers = CurrentSelected.GetComponentsInChildren<Renderer>();

                if (renderers.Length > 0)
                {
                    foreach (var child in renderers)
                        _center += child.bounds.center;
                    _center = _center / renderers.Length;
                    _center = _center - CurrentSelected.transform.position;
                }
            }

            if (CurrentSelected != null)
            {
                var worldCenter = Camera.main.WorldToScreenPoint(CurrentSelected.transform.position + _center);

                for (var i = 0; i < _buttons.Count; i++)
                {
                    var rectTransform = _buttons[i].GetComponent<RectTransform>();

                    var x = (float) (Radius * Screen.width / 100 * Math.Cos(2 * i * Math.PI / _buttons.Count));
                    var y = (float) (Radius * Screen.height / 100 * Math.Sin(2 * i * Math.PI / _buttons.Count));

                    var sizeDelta = rectTransform.sizeDelta;
                    x = x > 0 ? x + sizeDelta.x / 2 : x - sizeDelta.x / 2;
                    y = y > 0 ? y + sizeDelta.y / 2 : y - sizeDelta.y / 2;


                    rectTransform.transform.position = worldCenter + new Vector3(x, y, 0);
                    
                    _buttons[i].gameObject.SetActive(true);
                }
                
                    

                if (CurrentSelected.Flags.HasFlag(OOI.OOI.InteractionFlag.Scale))
                {
                    Slider.GetComponent<RectTransform>().transform.position =
                        worldCenter + new Vector3(0, -3 * Radius * Screen.height / 100, 0);
                }
                
                if (CurrentSelected.Flags.HasFlag(OOI.OOI.InteractionFlag.Changeable))
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
            OnSelect?.Invoke(CurrentSelected);
        }

        public void Deselect()
        {
            if (CurrentSelected != null)
            {
                OnDeselect?.Invoke();
                CurrentSelected = null;
            }
        }
#endif
        
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