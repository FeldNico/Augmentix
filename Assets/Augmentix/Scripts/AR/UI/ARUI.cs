using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Augmentix.Scripts.AR.UI
{
    public class ARUI : MonoBehaviour
    {
        public static ARUI Instance = null;

        public Text DebugText;
        public Text ConnectionText;
        public Slider ScaleSlider;
        public Toggle StreamToggle;
        public Toggle LockCam;
        public Button RemovePlayer;

        private PlayerSynchronizer IndicatorTarget;

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }

        void Start()
        {
            PickupTarget.Instance.GotPlayer += (player) =>
            {
                StreamToggle.gameObject.SetActive(true);
                ScaleSlider.gameObject.SetActive(true);
                RemovePlayer.gameObject.SetActive(true);
                LockCam.gameObject.SetActive(true);
            };

            PickupTarget.Instance.LostPlayer += (player) =>
            {
                StreamToggle.gameObject.SetActive(false);
                ScaleSlider.gameObject.SetActive(false);
                RemovePlayer.gameObject.SetActive(false);
                LockCam.gameObject.SetActive(false);
            };
            
            ScaleSlider.minValue = PickupTarget.Instance.Scale / 5f;
            ScaleSlider.value = PickupTarget.Instance.Scale;
            ScaleSlider.maxValue = PickupTarget.Instance.Scale * 5f;

            ScaleSlider.onValueChanged.AddListener(scale =>
            {
                PickupTarget.Instance.transform.localScale = new Vector3(scale, scale, scale);
            });

            PickupTarget.Instance.LostPlayer += (player) =>
            {
                ScaleSlider.value = PickupTarget.Instance.Scale;
            };
            
            RemovePlayer.onClick.AddListener(() =>
            {
                PickupTarget.Instance.LostPlayer.Invoke(PickupTarget.Instance.Current.gameObject);
            });
        }
    }
}
