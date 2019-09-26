using Photon.Pun;
using UnityEngine;

public class POI : MonoBehaviour, IPunInstantiateMagicCallback
{
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (info.photonView.InstantiationData.Length > 0)
        {
            var viewID = (int) info.photonView.InstantiationData[0];
            transform.parent = PhotonView.Find(viewID).transform;
            transform.localPosition = (Vector3) info.photonView.InstantiationData[1];
            transform.localRotation = (Quaternion) info.photonView.InstantiationData[2];
            transform.localScale = Vector3.one;
        }
    }
}
