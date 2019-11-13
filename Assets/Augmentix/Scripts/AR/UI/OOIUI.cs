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
using System.Runtime.InteropServices.WindowsRuntime;
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

        public float Radius = 5f;

        public RectTransform Slider;
        public Vector2 SliderOffset = new Vector2(0f,-3f);
        public RectTransform Dropdown;
        public Vector2 DropdownOffset = new Vector2(0f,3f);
        public RectTransform TangibleLock;
        public Vector2 TangibleLockOffset = new Vector2(-3f,3.2f);
        public float TangibleLockDistance;
        public RectTransform Delete;
        public Vector2 DeleteOffset= new Vector2(3f,3.2f);
        public RectTransform Text;
        public RectTransform Video;
        public RectTransform Animation;
        public RectTransform Highlight;
        
       
        
        
        public float HeightLineThreshold = 0.05f;
        public Material HeightLineMaterial;

        public OOI.OOI CurrentSelected { private set; get; } = null;

        public UnityAction<OOI.OOI> OnSelect;
        public UnityAction OnDeselect;

        public List<RectTransform> GetButtons(OOI.OOI ooi)
        {
            var flags = ooi.Flags;
            var l = new List<RectTransform>();
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

            Highlight.GetComponent<Button>().onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Highlight);
                Deselect();
            });
            Animation.GetComponent<Button>().onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Animation);
                Deselect();
            });
            Text.GetComponent<Button>().onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Text);
                Deselect();
            });
            Video.GetComponent<Button>().onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOI.OOI.InteractionFlag.Video);
                Deselect();
            });
            Slider.GetComponent<Slider>().onValueChanged.AddListener(value =>
            {
                if (CurrentSelected != null)
                    CurrentSelected.transform.localScale = new Vector3(value,value,value);
            });
            TangibleLock.GetComponent<Toggle>().onValueChanged.AddListener(value =>
            {
                if (CurrentSelected == null)
                    return;
                
                if (value)
                {
                    var target = CurrentSelected.GetComponentInParent<TangibleTarget>();
                    CurrentSelected.transform.parent =
                        Treveris.GetTreverisByPlayer(PickupTarget.Instance.Current.photonView.Owner).transform;
                    CurrentSelected.GetComponent<TangibleView>().IsLocked = true;
                    target.AddOOI("Tangibles/"+((AndroidTargetManager) TargetManager.Instance).EmptyTangible.name);
                    Delete.gameObject.SetActive(true);
                }
                else
                {
                    var targets = FindObjectsOfType<TangibleTarget>().Where(target => target.GetComponent<ImageTargetBehaviour>().CurrentStatus !=
                                                                                      TrackableBehaviour.Status.NO_POSE && target.Current.GetComponent<TangibleView>().IsEmpty).ToList();
                    
                    TangibleTarget closest = null;

                    foreach (var target in targets)
                    {
                        if (closest == null ||
                            Vector3.Distance(closest.transform.position, CurrentSelected.transform.position) >
                            Vector3.Distance(target.transform.position, CurrentSelected.transform.position))
                        {
                            closest = target;
                        }
                    }

                    PhotonNetwork.Destroy(closest.GetComponentInChildren<TangibleView>().gameObject);
                    
                    CurrentSelected.transform.parent = closest.transform;
                    CurrentSelected.transform.localPosition = Vector3.zero;
                    CurrentSelected.transform.localRotation = Quaternion.identity;
                    CurrentSelected.GetComponent<TangibleView>().IsLocked = false;
                    Delete.gameObject.SetActive(false);
                }
                //Deselect();
            });
            Dropdown.GetComponent<TMP_Dropdown>().onValueChanged.AddListener(value =>
            {
                if (CurrentSelected == null)
                    return;

                var current = CurrentSelected;
                Deselect();
                var target = current.GetComponentInParent<TangibleTarget>();
                PhotonNetwork.Destroy(current.gameObject);
                target.AddOOI("Tangibles/"+Dropdown.GetComponent<TMP_Dropdown>().options[value].text);
            });
            
            var options = ((AndroidTargetManager) AndroidTargetManager.Instance).TangiblePrefabs.Select(o => o.name)
                .ToList();
            options.Insert(0,((AndroidTargetManager) AndroidTargetManager.Instance).EmptyTangible.name);
            Dropdown.GetComponent<TMP_Dropdown>().AddOptions(options);

            Delete.GetComponent<Button>().onClick.AddListener(() =>
            {
                var current = CurrentSelected.gameObject;
                Deselect();
                PhotonNetwork.Destroy(current);
            });
            
            OnSelect += ooi =>
            {

                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);

                if (PickupTarget.Instance.Current == null)
                    return;
                
                _buttons?.Clear();
                _buttons = GetButtons(ooi);

                if (ooi.Flags.HasFlag(OOI.OOI.InteractionFlag.Scale))
                {
                    Slider.gameObject.SetActive(true);
                    var scale = ooi.transform.localScale.x;
                    var slider = Slider.GetComponent<Slider>();
                    slider.minValue = scale / 5f;
                    slider.value = scale;
                    slider.maxValue = scale * 5f;
                }
                if (ooi.Flags.HasFlag(OOI.OOI.InteractionFlag.Changeable))
                {
                    var dropdown = Dropdown.GetComponent<TMP_Dropdown>();
                    var o = dropdown.options.First(data => ooi.gameObject.name.Equals(data.text+"(Clone)"));
                    dropdown.value = dropdown.options.IndexOf(o);
                    Dropdown.gameObject.SetActive(true);
                }

                if (ooi.GetComponent<TangibleView>() && ooi.Flags.HasFlag(OOI.OOI.InteractionFlag.Lockable))
                {
                    TangibleLock.gameObject.SetActive(true);
                    TangibleLock.GetComponent<Toggle>().isOn = ooi.GetComponent<TangibleView>().IsLocked;
                }
                
                if (ooi.GetComponent<TangibleView>() && ooi.GetComponent<TangibleView>().IsLocked)
                {
                    Delete.gameObject.SetActive(true);
                }

                _center = new Vector3();
                var renderers = ooi.GetComponentsInChildren<MeshRenderer>();

                if (renderers.Length > 0)
                {
                    foreach (var child in renderers)
                        _center += child.bounds.center;
                    _center = _center / renderers.Length;
                    _center = _center - ooi.transform.position;
                }
            };

            OnDeselect += () =>
            {
                foreach (Transform child in transform)
                    child.gameObject.SetActive(false);
            };
        }

        void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
#else
            if (Input.GetMouseButtonUp(0))
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

        private List<RectTransform> _buttons = null;
        private OOI.OOI _prevtarget = null;
        private Vector3 _center = new Vector3();

        void SmoothMoveButtons()
        {
            if (CurrentSelected == null)
                return;
            
            var worldCenter = Camera.main.WorldToScreenPoint(CurrentSelected.transform.position + _center);

            for (var i = 0; i < _buttons.Count; i++)
            {
                var rectTransform = _buttons[i];

                var x = (float) (Radius * Screen.width / 100 * Math.Cos(2 * i * Math.PI / _buttons.Count));
                var y = (float) (Radius * Screen.height / 100 * Math.Sin(2 * i * Math.PI / _buttons.Count));

                var sizeDelta = rectTransform.sizeDelta;
                x = x > 0 ? x + sizeDelta.x / 2 : x - sizeDelta.x / 2;
                y = y > 0 ? y + sizeDelta.y / 2 : y - sizeDelta.y / 2;
                
                rectTransform.transform.position = worldCenter + new Vector3(x, y, 0);
                
                _buttons[i].gameObject.SetActive(true);
            }
            
            Slider.transform.position =
                worldCenter + new Vector3(SliderOffset.x * Radius * Screen.width / 100, SliderOffset.y * Radius * Screen.height / 100, 0);

            Dropdown.transform.position =
                worldCenter + new Vector3(DropdownOffset.x * Radius * Screen.width / 100, DropdownOffset.y * Radius * Screen.height / 100, 0);

            TangibleLock.transform.position =
                worldCenter + new Vector3(TangibleLockOffset.x * Radius * Screen.width / 100, TangibleLockOffset.y * Radius * Screen.height / 100, 0);
            
            Delete.transform.position =
                worldCenter + new Vector3(DeleteOffset.x * Radius * Screen.width / 100, DeleteOffset.y * Radius * Screen.height / 100, 0);

            var toggle = TangibleLock.GetComponent<Toggle>();
            if (CurrentSelected.GetComponent<TangibleView>() && toggle.isOn)
            {
                var closestDistance = float.MaxValue;
                foreach (var tangibleTarget in TangibleTarget.AllTangibles)
                {
                    if (!tangibleTarget.Current.GetComponent<TangibleView>().IsEmpty || tangibleTarget.GetComponent<TrackableBehaviour>().CurrentStatus == TrackableBehaviour.Status.NO_POSE)
                        continue;
                    
                    if (Vector3.Distance(tangibleTarget.transform.position, CurrentSelected.transform.position) < closestDistance)
                    {
                        closestDistance = Vector3.Distance(tangibleTarget.transform.position, CurrentSelected.transform.position);
                    }
                }

                if (closestDistance < TangibleLockDistance && CurrentSelected.Flags.HasFlag(OOI.OOI.InteractionFlag.Lockable))
                    toggle.gameObject.SetActive(true);
                else
                    toggle.gameObject.SetActive(false);
            }
        }

        public void Select(OOI.OOI Target)
        {
            Deselect();
            OnSelect?.Invoke(Target);
            CurrentSelected = Target;
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
                    _prevHighlightTarget.GetComponent<Outline>().enabled = false;
                _prevHighlightTarget = Target;
            }

            var outline = Target.GetComponent<Outline>();
            if (!outline)
            {
                outline = Target.AddComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineVisible;
            }

            outline.enabled = !outline.enabled;
            
        }
    }

}