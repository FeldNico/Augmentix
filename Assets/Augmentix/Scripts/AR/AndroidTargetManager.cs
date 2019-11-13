using Augmentix.Scripts.AR.UI;
using UnityEngine;
#if UNITY_ANDROID
using Vuforia;
#endif

namespace Augmentix.Scripts.AR
{
    public class AndroidTargetManager : TargetManager
    {
        
        public GameObject EmptyTangible;
        public GameObject[] TangiblePrefabs;
        
        
        
        new void Start()
        {
#if UNITY_ANDROID
            CameraDevice.Instance.SetFocusMode(CameraDevice.FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
#endif
            base.Start();

            OnConnection += () =>
            {
                ARUI.Instance.ConnectionText.text = "Connected";
                ARUI.Instance.ConnectionText.color = Color.green;

                ARUI.Instance.ConnectionText.enabled = false;
            };
        }
    }
}

