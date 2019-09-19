using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Npgsql;
using System.Globalization;

public class PsmNetwork 
{
    // connection
    public string osmServerIP = "136.199.52.75";
    public string osmServerPort = "5432";
    private NpgsqlConnection dbcon = null;

    public void EstablishServerConnection()
    {
        string connectionString =
            "Server=" + osmServerIP + ";" +
            "Port=" + osmServerPort + ";" +
            "Database=gis;" +
            "User ID=postgres;" +
            "Password=postgres;";

        dbcon = new NpgsqlConnection(connectionString);
        dbcon.Open();

        Debug.Log("Successfully established connection to PXR PostgreSQL sever.");
    }

    public void CloseServerConnection()
    {
        if (dbcon != null)
        {
            dbcon.Close();
            dbcon = null;

            Debug.Log("Successfully closed connection to PXR PostgreSQL sever.");
        }
    }

    public NpgsqlDataReader CreateReaderOnQuery(string sql)
    {
        NpgsqlCommand dbcmd = dbcon.CreateCommand();
        dbcmd.CommandText = sql;
        NpgsqlDataReader reader = dbcmd.ExecuteReader();
        dbcmd.Dispose();
        dbcmd = null;
        return reader;
    }

    private void CloseReader(NpgsqlDataReader reader)
    {
        reader.Close();
        reader = null;
    }

    private Vector2 ParsePGSQL_CoordinateTupel(string tupelString, Vector2 reference)
    {
        string[] strCoords = tupelString.Split(' ');
        Vector2 coord = new Vector2();
        coord.x = (float.Parse(strCoords[0], CultureInfo.InvariantCulture) - reference.x);
        coord.y = (float.Parse(strCoords[1], CultureInfo.InvariantCulture) - reference.y);
        return coord;
    }

    private List<Vector2> ParsePGSQL_LineString(string listString, Vector2 reference)
    {
        return ParsePGSQL_String(listString, "(", ")", reference);
    }

    private List<Vector2> ParsePGSQL_PolygonString(string listString, Vector2 reference)
    {
        return ParsePGSQL_String(listString, "((", "))", reference);
    }

    private List<Vector2> ParsePGSQL_String(string listString, string start, string end, Vector2 reference)
    {
        int pFrom = listString.IndexOf(start) + start.Length;
        int pTo = listString.LastIndexOf(end);
        listString = listString.Substring(pFrom, pTo - pFrom);

        string[] strList = listString.Split(',');
        List<Vector2> v2List = new List<Vector2>();

        foreach (string item in strList)
        {
            v2List.Add(ParsePGSQL_CoordinateTupel(item, reference));
        }

        return v2List;
    }

    public List<List<Vector2>> QueryLinesWhere(string where)
    {
        List<List<Vector2>> lines = new List<List<Vector2>>();
        string sql = "SELECT ST_AsText(ST_Transform(way,31466)) AS line " +
                        "FROM planet_osm_line " +
                        "WHERE " + where + ";";

        NpgsqlDataReader reader = CreateReaderOnQuery(sql);

        while (reader.Read())
        {
            lines.Add(ParsePGSQL_LineString((string)reader["line"], WorldGenerator.worldReference));
        }

        CloseReader(reader);

        return lines;
    }

    public List<WorldGenerator.PXR_Model> QueryBuildingsWithModels()
    {
        List<WorldGenerator.PXR_Model> buildings = new List<WorldGenerator.PXR_Model>();

        string sql = "SELECT name, tags->'pxr:id' AS id, ST_AsText(ST_Transform(way,31466)) AS polygon " +
                        "FROM planet_osm_polygon " +
                        "WHERE tags ? 'pxr:id';";

        NpgsqlDataReader reader = CreateReaderOnQuery(sql);

        while (reader.Read())
        {
            WorldGenerator.PXR_Model building = new WorldGenerator.PXR_Model();

            building.id = "_" + (string)reader["id"];
       
            List<Vector2> polygon = ParsePGSQL_PolygonString((string)reader["polygon"], WorldGenerator.worldReference);

            Vector2 referencePoint = new Vector2(Mathf.Infinity, Mathf.Infinity);
            foreach (Vector2 point in polygon)
            {
                if(point.x < referencePoint.x)
                {
                    referencePoint = point;
                }
                else if(point.x == referencePoint.x && point.y < referencePoint.y)
                {
                    referencePoint = point;
                }
            }
            building.position = new Vector3(referencePoint.x, 0, referencePoint.y);
            buildings.Add(building);
        }
        CloseReader(reader);

        return buildings;
    }

    public List<WorldGenerator.PXR_PolygonModel> QueryBuildingsWithPolygons()
    {
        List<WorldGenerator.PXR_PolygonModel> polygons = new List<WorldGenerator.PXR_PolygonModel>();

        string sql = "SELECT ST_AsText(ST_Transform(way,31466)) AS polygon, tags->'height' as height " +
                        "FROM planet_osm_polygon " +
                        "WHERE NOT tags ? 'pxr:id' AND tags ? 'height';";

        NpgsqlDataReader reader = CreateReaderOnQuery(sql);

        while (reader.Read())
        {
            WorldGenerator.PXR_PolygonModel model = new WorldGenerator.PXR_PolygonModel();
            model.polygon = ParsePGSQL_PolygonString((string)reader["polygon"], WorldGenerator.worldReference);
            model.height = float.Parse((string)reader["height"], CultureInfo.InvariantCulture);

            polygons.Add(model);
        }
        CloseReader(reader);

        return polygons;
    }
}


