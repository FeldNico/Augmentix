using UnityEngine;

namespace Augmentix.Scripts.AR
{
    public class MapTarget : MonoBehaviour
    {
        public static MapTarget Instance = null;

        public float PlayerScale;
        public float Scale;
        public Vector3 MapOffset = new Vector3(1.694f,0f,1.261f);
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
            Scaler.transform.localPosition = MapOffset;
            Scaler.transform.localScale = new Vector3(Scale,Scale,Scale);
        }

    }
}
