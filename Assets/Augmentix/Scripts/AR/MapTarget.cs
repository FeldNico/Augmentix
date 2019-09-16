using UnityEngine;

namespace Augmentix.Scripts.AR
{
    public class MapTarget : MonoBehaviour
    {

        public static MapTarget Instance = null;

        public float PlayerScale;
        public float Scale;
        public GameObject Scaler { private set; get; }

        void Awake()
        {
            if (Instance == null)
                Instance = this;
        }


        // Start is called before the first frame update
        void Start()
        {
            PlayerSynchronizer.InstanceAction += info =>
            {
                if (!info.photonView.IsMine)
                {
                    info.photonView.gameObject.transform.parent = Scaler.transform;
                    info.photonView.gameObject.transform.localPosition = Vector3.zero;
                    info.photonView.gameObject.transform.localScale = new Vector3(PlayerScale,PlayerScale,PlayerScale);
                }
            };

            Scaler = new GameObject("Scaler");
            Scaler.transform.parent = transform;
            Scaler.transform.localPosition = Vector3.zero;
            Scaler.transform.localScale = new Vector3(Scale,Scale,Scale);
        }

    }
}
