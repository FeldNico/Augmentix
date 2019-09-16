using Augmentix.Scripts.AR.UI;
using UnityEngine;

namespace Augmentix.Scripts.AR
{
    public class AndroidTargetManager : TargetManager
    {
        new void Start()
        {
            base.Start();

            OnConnection += () =>
            {
                ARUI.Instance.ConnectionText.text = "Connected";
                ARUI.Instance.ConnectionText.color = Color.green;
            };
        }
    }
}

