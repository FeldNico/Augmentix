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
                while (true)
                {
                    Vector3 center = new Vector3();
                    var renderers = _target.GetComponentsInChildren<Renderer>();
                    foreach (var child in renderers)
                        center += child.bounds.center;
                    center = center / renderers.Length;
                
                    _indicator.transform.LookAt(center);
                    if (Quaternion.Angle(_indicator.transform.rotation, Camera.main.transform.rotation) < 20f)
                    {
                        _indicator.gameObject.SetActive(false);
                        break;
                    }
                
                    _indicator.transform.localRotation = _indicator.transform.localRotation * Quaternion.Euler(80, 0, 0);

                    yield return new WaitForEndOfFrame();
                }
            }
        }
    }
}