using Photon.Pun;
using UnityEngine;

namespace Augmentix.Scripts.VR
{
    public class StandaloneTargetManager: TargetManager
    {
        new void Start()
        {
            base.Start();

            OnConnection += () =>
            {
                VRUI.Instance.ConnectionText.text = "Connected";
                VRUI.Instance.ConnectionText.color = Color.green;
                var player = PhotonNetwork.Instantiate("Player", new Vector3(), new Quaternion());
                player.transform.parent = Camera.main.transform;
                player.transform.localPosition = Vector3.zero;
                var treveris = PhotonNetwork.Instantiate("Treveris", new Vector3(), new Quaternion());
            };

        }


    }
}

