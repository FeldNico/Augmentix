using System.Collections.Generic;
using UnityEngine;

namespace Augmentix.Scripts
{
    public class Utils
    {
        public static List<T> FindAllObjectsInScene<T>(string name = null) where  T: MonoBehaviour
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            List<T> objectsInScene = new List<T>();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                if (rootObjects[i].GetComponent<T>() && (name == null || name == rootObjects[i].name))
                    objectsInScene.Add(rootObjects[i].GetComponent<T>());
            }

            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].transform.root)
                {
                    for (int i2 = 0; i2 < rootObjects.Length; i2++)
                    {
                        if (allObjects[i].transform.root == rootObjects[i2].transform && allObjects[i] != rootObjects[i2] && allObjects[i].GetComponent(typeof(T)))
                        {
                            if (name == null || name == rootObjects[i].name)
                            {
                                objectsInScene.Add(allObjects[i].GetComponent<T>());
                                break;
                            }
                        }
                    }
                }
            }
            return objectsInScene;
        }

        public static T FindObjectInScene<T>(string name = null) where T : MonoBehaviour
        {
            UnityEngine.SceneManagement.Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            List<GameObject> objectsInScene = new List<GameObject>();

            for (int i = 0; i < rootObjects.Length; i++)
            {
                objectsInScene.Add(rootObjects[i]);
            }

            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].transform.root)
                {
                    for (int i2 = 0; i2 < rootObjects.Length; i2++)
                    {
                        if (allObjects[i].transform.root == rootObjects[i2].transform && allObjects[i] != rootObjects[i2] && allObjects[i].GetComponent(typeof(T)))
                        {
                            if (name == null || name == allObjects[i].name)
                                return allObjects[i].GetComponent<T>();
                        }
                    }
                }
            }

            return null;
        }

    }
}
