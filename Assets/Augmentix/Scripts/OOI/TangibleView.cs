using Augmentix.Scripts.AR;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

namespace Augmentix.Scripts.OOI
{
    public class TangibleView : MonoBehaviourPunCallbacks, IPunObservable
    {
        public bool IsLocked = false;
        public bool IsEmpty = false;
        
        private float m_Distance;
        private float m_Angle;

        private PhotonView m_PhotonView;

        private Vector3 m_Direction;
        private Vector3 m_NetworkPosition;
        private Vector3 m_StoredPosition;

        private Quaternion m_NetworkRotation;

        bool m_firstTake = false;

        public void Awake()
        {
            m_PhotonView = GetComponent<PhotonView>();

            m_StoredPosition = Vector3.zero;
            m_NetworkPosition = Vector3.zero;

            m_NetworkRotation = Quaternion.identity;
            
            if (!photonView.IsMine && IsEmpty)
                foreach (var child in GetComponentsInChildren<Renderer>())
                {
                    child.enabled = false;
                }
        }

        void OnEnable()
        {
            m_firstTake = true;
        }
        
        public void Update()
        {
            if (!this.m_PhotonView.IsMine)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, this.m_NetworkPosition, this.m_Distance * (1.0f / PhotonNetwork.SerializationRate));
                transform.localRotation = Quaternion.RotateTowards(transform.localRotation, this.m_NetworkRotation, this.m_Angle * (1.0f / PhotonNetwork.SerializationRate));
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            
            if (IsEmpty)
                return;
            
            if (stream.IsWriting)
            {

                if (PickupTarget.Instance.Current == null)
                    return;
                
                stream.SendNext(IsLocked);
                
                if (IsLocked)
                    return;
                
                var localTransform =
                    Treveris.GetTreverisByPlayer(PickupTarget.Instance.Current.GetComponent<PhotonView>().Owner).transform;
                var position = localTransform.InverseTransformPoint(transform.position);

                this.m_Direction = position - this.m_StoredPosition;
                this.m_StoredPosition = position;

                stream.SendNext(position);
                stream.SendNext(this.m_Direction);

                stream.SendNext(Quaternion.Inverse(localTransform.transform.rotation) * transform.rotation);

                stream.SendNext(transform.localScale / PickupTarget.Instance.Scaler.transform.localScale.x);
            }
            else
            {
                this.IsLocked = (bool) stream.ReceiveNext();
                
                if (IsLocked)
                    return;
                
                
                this.m_NetworkPosition = (Vector3) stream.ReceiveNext();
                this.m_Direction = (Vector3) stream.ReceiveNext();

                if (m_firstTake)
                {
                    transform.localPosition = this.m_NetworkPosition;
                    this.m_Distance = 0f;
                }
                else
                {
                    float lag = Mathf.Abs((float) (PhotonNetwork.Time - info.SentServerTime));
                    this.m_NetworkPosition += this.m_Direction * lag;
                    this.m_Distance = Vector3.Distance(transform.localPosition, this.m_NetworkPosition);
                }


                this.m_NetworkRotation = (Quaternion) stream.ReceiveNext();

                if (m_firstTake)
                {
                    this.m_Angle = 0f;
                    transform.localRotation = this.m_NetworkRotation;
                }
                else
                {
                    this.m_Angle = Quaternion.Angle(transform.localRotation, this.m_NetworkRotation);
                }
                
                transform.localScale = (Vector3) stream.ReceiveNext();

                if (m_firstTake)
                {
                    m_firstTake = false;
                }
            }


            
        }
        
        
        
    }
}
