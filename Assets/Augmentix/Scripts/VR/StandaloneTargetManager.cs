using Photon.Pun;
using UnityEngine;
using Valve.VR.InteractionSystem;

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
                var player = PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
                player.transform.parent = Camera.main.transform;
                player.transform.localPosition = Vector3.zero;
                player.transform.localRotation = Quaternion.identity;
                PhotonNetwork.Instantiate("Treveris", new Vector3(), new Quaternion());
            };
        }


    }
}

