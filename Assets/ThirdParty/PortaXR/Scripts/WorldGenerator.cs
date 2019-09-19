using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Npgsql;

public class WorldGenerator : MonoBehaviour
{
    public class PXR_Model
    {
        public string id = "_00000";
        public Vector3 position = Vector3.zero;
    }

    public class PXR_PolygonModel
    {
        public List<Vector2> polygon;
        public float height;
    }

    private PsmNetwork net = null;

    // map settings
    public static Vector2 worldReference = new Vector2(2546400f, 5513500f); // EPSG31466

    // models
    public Material streetMat;
    public Material buildingMat;

    public Func<PXR_Model, Transform, bool> OnBuildingCreation = (model, root) => false;

    public void Generate()
    {
        net = new PsmNetwork();

        net.EstablishServerConnection();
        GenerateWorld();
        net.CloseServerConnection();
    }

    private void GenerateWorld()
    {
        Debug.Log("Rendering ways...");
        GenerateStreets();

        Debug.Log("Rendering walls...");
        GenerateWalls();

        Debug.Log("Rendering buildings...");
        GenerateBuildings();
    }

    // TODO: 
    // - care about different street width and types
    // - get rid of the line rederer or at least project to the surface
    private void GenerateStreets()
    {
        List<List<Vector2>> ways = net.QueryLinesWhere("highway = 'residential'");
        GameObject streetRoot = new GameObject("Streets");
        streetRoot.transform.parent = transform;
        streetRoot.transform.localScale = Vector3.one;

        foreach (List<Vector2> way in ways)
        {
            GameObject street = new GameObject("Street");
            street.transform.parent = streetRoot.transform;
            street.transform.localRotation = Quaternion.Euler(90, 0, 0);
            street.transform.localScale = Vector3.one;

            LineRenderer line = street.AddComponent<LineRenderer>();
            line.sortingLayerName = "OnTop";
            line.sortingOrder = 5;
            line.positionCount = way.Count;
            line.startWidth = 5f;
            line.endWidth = 5f;
            line.useWorldSpace = true;
            line.material = streetMat;
            
            // nessesary to not be automatically oriented to the camera
            line.alignment = LineAlignment.TransformZ;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            int i = 0;
            foreach (Vector2 point in way)
            {
                line.SetPosition(i, new Vector3(point.x, 0.01f, point.y));
                i++;
            }
        }
    }

    private void GenerateWalls()
    {
        List<List<Vector2>> walls = net.QueryLinesWhere("barrier = 'city_wall'");
        GameObject wallsRoot = new GameObject("Walls");
        wallsRoot.transform.parent = transform;
        wallsRoot.transform.localScale = Vector3.one;

        foreach (List<Vector2> wall in walls)
        {
            GameObject wallRoot = new GameObject("Wall");
            wallRoot.transform.parent = wallsRoot.transform;
            wallRoot.transform.localScale = Vector3.one;
            
            for (int i = 0; i + 1 < wall.Count; ++i)
            {
                Vector2 mid = (wall[i + 1] + wall[i]) / 2;
                Vector2 direction = wall[i + 1] - wall[i];
                float hight = 15f;

                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.parent = wallRoot.transform;

                cube.transform.localPosition = new Vector3(mid.x, hight / 2, mid.y);
                cube.transform.localScale = new Vector3(direction.magnitude, hight, 1f);
                cube.transform.localRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);
                cube.transform.Rotate(0, 90, 0);
            }
        }
    }

    private void GenerateBuildings()
    {
        // Get buildings with models
        List<PXR_Model> buildings = net.QueryBuildingsWithModels();
        GameObject buildingsRoot = new GameObject("Buildings");
        buildingsRoot.transform.parent = transform;
        buildingsRoot.transform.localScale = Vector3.one;

        foreach (PXR_Model building in buildings)
        {
            var created = OnBuildingCreation.Invoke(building,buildingsRoot.transform);

            if (!created)
            {
                // TODO:
                // else if (in central shared model container) load it
                // else error
                Debug.LogError("Building "+building.id+" not created!");
            }
            
        }

        // generate models for buildings with just polygons
        List<PXR_PolygonModel> polygons = net.QueryBuildingsWithPolygons();
       
        foreach(PXR_PolygonModel item in polygons)
        {
            GameObject building = new GameObject("Generated Building");
            building.transform.parent = buildingsRoot.transform;
            building.transform.localScale = Vector3.one;

            MeshFilter meshFilter = building.AddComponent<MeshFilter>();
            item.polygon.RemoveAt(item.polygon.Count - 1);
            meshFilter.mesh = CreateExtrudedMeshFromPXR_PolygonModel(item);
            Renderer rend = building.AddComponent<MeshRenderer>();
            rend.material = buildingMat;
        }       
    }

    private Mesh CreateExtrudedMeshFromPXR_PolygonModel(PXR_PolygonModel polygon)
    {
        return Triangulator.CreateExtrudedMeshFromPolygon(polygon.polygon, polygon.height);
    }
}