using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ColliderGenerator: Editor
{

    const int MAXTRISCOUNT = 65536;

    [MenuItem("GameObject/Combine Mesh Collider", false, 0)]
    static void CombineCollider()
    {
        if ( Selection.activeGameObject == null )
            return;

        MeshFilter[] meshFilters = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length == 0)
            return;

        List<CombineInstance> combine = new List<CombineInstance>();

        var delete = EditorUtility.DisplayDialog("Collider Generator", "Delete Child Collider?", "Yes", "No");

        var hadFilter = true;

        if (Selection.activeGameObject.GetComponent<MeshFilter>() == null)
        {
            hadFilter = false;
            Selection.activeGameObject.AddComponent<MeshFilter>();
        }

        var prevMesh = Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh;

        var count = 0;

        if (Selection.activeGameObject.GetComponent<MeshCollider>())
            DestroyImmediate(Selection.activeGameObject.GetComponent<MeshCollider>());

        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i].sharedMesh == null)
                if (delete)
                {
                    DestroyImmediate(meshFilters[i]);
                    continue;
                }
            
            var c = new CombineInstance();
            c.mesh = meshFilters[i].sharedMesh;
            c.transform = Selection.activeGameObject.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            combine.Add(c);
            if (meshFilters[i].GetComponent<Collider>() && delete)
                DestroyImmediate(meshFilters[i].GetComponent<Collider>());

            count += meshFilters[i].sharedMesh.vertexCount;

            if (i + 1 == meshFilters.Length || meshFilters[i+1].sharedMesh.vertexCount + count > MAXTRISCOUNT)
            {
                if (Selection.activeGameObject.GetComponent<MeshFilter>() == null)
                    Selection.activeGameObject.AddComponent<MeshFilter>();
                Selection.activeGameObject.transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                Selection.activeGameObject.transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine.ToArray());

                Selection.activeGameObject.AddComponent<MeshCollider>();

                combine.Clear();
                count = 0;
            }
        }

        if (hadFilter)
            Selection.activeGameObject.GetComponent<MeshFilter>().sharedMesh = prevMesh;
        else
            DestroyImmediate(Selection.activeGameObject.GetComponent<MeshFilter>());
    }
}
