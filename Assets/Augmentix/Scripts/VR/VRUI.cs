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
                _target = Target;
                var outline = _target.GetComponent<Outline>();
                if (!outline)
                {
                    outline = _target.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineVisible;
                }
                outline.enabled = true;
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
                var outline = _target.GetComponent<Outline>();
                if (!outline)
                {
                    outline = _target.AddComponent<Outline>();
                    outline.OutlineMode = Outline.Mode.OutlineVisible;
                }
                outline.enabled = true;
                _indicator.SetActive(true);
                _indicatorRotate = StartCoroutine(RotateIndicator());
            }
            else
            {
                _indicator.SetActive(false);
                _target.GetComponent<Outline>().enabled = false;
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
                var ooi = _target.GetComponent<OOI.OOI>();
                while (true)
                {
                    var closedPoint = _target.transform.position;
                    var playerPos = Camera.main.transform.position;
                    foreach (var meshCollider in ooi.ConvexCollider)
                    {
                        var tmp = meshCollider.ClosestPoint(playerPos);
                        if (Vector3.Distance(tmp, playerPos) < Vector3.Distance(closedPoint, playerPos))
                            closedPoint = tmp;
                    }

                    var indicatorTransform = _indicator.transform;
                    indicatorTransform.LookAt(closedPoint);

                    if (Quaternion.Angle(indicatorTransform.rotation, Camera.main.transform.rotation) < 20f && Vector3.Distance(closedPoint, playerPos) < HighlightDistance)
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