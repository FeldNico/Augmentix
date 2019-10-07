using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Augmentix.Scripts.VR
{
    public class VRUI : MonoBehaviour
    {
        public static VRUI Instance = null;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        public GameObject IndicationPrefab;
        public Image VideoImage;
        public Text ConnectionText;
        public float HighlightDistance;

        private GameObject _target;
        private GameObject _indicator;
        private Coroutine _indicatorRotate;

        public void ToggleHighlightTarget(GameObject Target)
        {
            if (_target != null && _target != Target)
            {
                Destroy(_target.GetComponent<Outline>());
                _target = Target;
                var outline = _target.AddComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                return;
            }

            if (_target == null)
            {
                if (_indicator == null)
                {
                    _indicator = Instantiate(IndicationPrefab,Vector3.zero, Quaternion.Euler(80, 0, 0),
                        Camera.main.transform);
                    _indicator.transform.localPosition = new Vector3(0,-0.15f,0.5f);
                    _indicator.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
                }

                _target = Target;
                var outline= _target.AddComponent<Outline>();
                outline.OutlineMode = Outline.Mode.OutlineVisible;
                _indicator.SetActive(true);
                _indicatorRotate = StartCoroutine(RotateIndicator());
            }
            else
            {
                _indicator.SetActive(false);
                Destroy(_target.GetComponent<Outline>());
                _target = null;
                try
                {
                    StopCoroutine(_indicatorRotate);
                }
                catch (Exception e)
                {
                    // ignored
                }
            }

            IEnumerator RotateIndicator()
            {
                Renderer[] renderers = null;
                GameObject prev = null;
                var center = new Vector3();
                while (true)
                {
                    var trans = _target.transform;
                    var distance = float.MaxValue;
                    if (_target != prev)
                    {
                        renderers = _target.GetComponentsInChildren<Renderer>();
                        prev = _target;
                        
                        center = new Vector3();
                        
                        foreach (var child in renderers)
                        {
                            center += child.bounds.center;
                        }
                        center = center / renderers.Length;
                    }
                    foreach (var child in renderers)
                    {
                        var tmp = Vector3.Distance(child.bounds.ClosestPoint(Camera.main.transform.position),Camera.main.transform.position);
                        if (tmp < distance)
                        {
                            distance = tmp;
                        }
                    }

                    var indicatorTransform = _indicator.transform;
                    indicatorTransform.LookAt(center);

                    if (Quaternion.Angle(indicatorTransform.rotation, Camera.main.transform.rotation) < 20f && distance < HighlightDistance)
                    {
                        _indicator.gameObject.SetActive(false);
                        break;
                    }
                    indicatorTransform.localRotation = indicatorTransform.localRotation * Quaternion.Euler(80, 0, 0);

                    yield return new WaitForEndOfFrame();
                }
            }
        }
    }
}