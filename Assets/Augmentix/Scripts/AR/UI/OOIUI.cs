using System;
using System.Collections;
using System.Collections.Generic;
using Augmentix.Scripts.OOI;
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

        public Button Text;
        public Button Video;
        public Button Animation;
        public Button Highlight;
    
        public OOIView CurrentSelected { private set; get; } = null;

        public UnityAction<OOIView> OnSelect;
        public UnityAction OnDeselect;

        public List<Button> GetButtons(OOIView ooiView)
        {
            var flags = ooiView.Flags;
            var l = new List<Button>();
            if (flags.HasFlag(OOIView.InteractionFlag.Highlight))
                l.Add(Highlight);
            if (flags.HasFlag(OOIView.InteractionFlag.Animation))
                l.Add(Animation);
            if (flags.HasFlag(OOIView.InteractionFlag.Text))
                l.Add(Text);
            if (flags.HasFlag(OOIView.InteractionFlag.Video))
                l.Add(Video);

            return l;
        }

        private OOIView _target;

        void Start()
        {
            OnSelect += (target) =>
            {
                _target = target;
                StartCoroutine(SmoothMoveButtons());
            };

            OnDeselect += () =>
            {
                _target = null;
                foreach (var button in GetComponentsInChildren<Button>())
                {
                    button.gameObject.SetActive(false);
                }
            };
        
            TangibleTarget.Instance.OnStatusChange += status =>
            {
                if (status == TrackableBehaviour.Status.NO_POSE)
                    Deselect();
            };

            TangibleTarget.Instance.LostPlayer += Deselect;

            Highlight.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOIView.InteractionFlag.Highlight);
                Deselect();
            });
            Animation.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOIView.InteractionFlag.Animation);
                Deselect();
            });
            Text.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOIView.InteractionFlag.Text);
                Deselect();
            });
            Video.onClick.AddListener(() =>
            {
                CurrentSelected.Interact(OOIView.InteractionFlag.Video);
                Deselect();
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
                if (EventSystem.current.IsPointerOverGameObject() ||
                    EventSystem.current.currentSelectedGameObject != null)
                {
                    return;
                }
#if UNITY_ANDROID && !UNITY_EDITOR
            var ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
#else
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
#endif
                RaycastHit hit = new RaycastHit();
                if (Physics.Raycast(ray, out hit) && hit.transform.GetComponent<OOIView>())
                {
                    Select(hit.transform.GetComponent<OOIView>());
                }
                else
                {
                    Deselect();
                }

            }
        }

        public void Select(OOIView Target)
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
    
        private IEnumerator SmoothMoveButtons()
        {
            List<Button> Buttons = null;
            OOIView prevtarget = null;
            Vector3 center = new Vector3();
            while (true)
            {
                if (_target == null)
                    break;
            
                if (_target != prevtarget)
                {
                    foreach (var button in GetComponentsInChildren<Button>())
                        button.gameObject.SetActive(false);

                    Buttons = GetButtons(_target);
                    foreach (var button in Buttons)
                        button.gameObject.SetActive(true);

                    center = new Vector3();
                    var renderers = _target.GetComponentsInChildren<Renderer>();
                    foreach (var child in renderers)
                        center += child.bounds.center;
                    center = center / renderers.Length;
                    center = center - _target.transform.position;
                    prevtarget = _target;
                }

                if (_target != null)
                {
                    for (var i = 0; i < Buttons.Count; i++)
                    {
                        var x = (float) (Radius * Screen.width / 100 * Math.Cos(2 * i * Math.PI / Buttons.Count));
                        var y = (float) (Radius * Screen.height / 100 * Math.Sin(2 * i * Math.PI / Buttons.Count));

                        var rectTransform = Buttons[i].GetComponent<RectTransform>();

                        var position = rectTransform.transform.position;
                        var direction = ((Camera.main.WorldToScreenPoint(_target.transform.position + center) + new Vector3(x, y, 0)) -
                                         position) * 1f;

                        position = position + direction;
                        rectTransform.transform.position = position;
                    }

                    yield return new WaitForEndOfFrame();
                }
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
