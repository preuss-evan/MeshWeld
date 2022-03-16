using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WeldingManager : MonoBehaviour
{
    public GameObject ObjectA;
    public GameObject ObjectB;

    private bool[,] connections;

    // Start is called before the first frame update
    void Start()
    {
        // Getting all the meshes from all game objects
        //List<MeshFilter> meshAFilters = new List<MeshFilter>();
        //List<MeshFilter> meshBFilters = new List<MeshFilter>();
        //int aChildren = ObjectA.transform.childCount;
        //int bChildren = ObjectB.transform.childCount;

        //for (int i = 0; i < aChildren; i++)
        //{
        //    meshAFilters.Add(ObjectA.transform.GetChild(i).GetComponent<MeshFilter>());
        //}

        //for (int i = 0; i < bChildren; i++)
        //{
        //    meshBFilters.Add(ObjectB.transform.GetChild(i).GetComponent<MeshFilter>());
        //}

        // Get matching vertices
        Vector3 direction = (ObjectB.transform.position - ObjectA.transform.position).normalized;


        // Manipulate vertices
        //List<Vector3> aEdgeVertices = new List<Vector3>();

        // Get edge of mesh A
        MeshFilter filterA = ObjectA.GetComponent<MeshFilter>();
        var meshA = filterA.mesh;
        //var triangles = mesh.vertices;

        var verticesA = meshA.vertices;
        var worldVerticesA = verticesA.Select(v => filterA.transform.localToWorldMatrix.MultiplyPoint3x4(v)).ToArray();
        Dictionary<int, Vector3> edgeVerticesA = GetEdgeVerticesAtDirection(worldVerticesA, direction);
        bool[,] connectionsA = GetConnectionArray(worldVerticesA, meshA.triangles);
        List<int> orderedEdgeListA = GetOrderedEdgeList(edgeVerticesA, connectionsA);



        MeshFilter filterB = ObjectB.GetComponent<MeshFilter>();
        var meshB = filterB.mesh;
        //var triangles = mesh.vertices;

        var verticesB = meshB.vertices;
        var worldVerticesB = verticesB.Select(v => filterB.transform.localToWorldMatrix.MultiplyPoint3x4(v)).ToArray();
        Dictionary<int, Vector3> edgeVerticesB = GetEdgeVerticesAtDirection(worldVerticesB, -direction);
        bool[,] connectionsB = GetConnectionArray(worldVerticesB, meshB.triangles);
        List<int> orderedEdgeListB = GetOrderedEdgeList(edgeVerticesB, connectionsB);

        MoveVerticesEdge(filterA, filterB, orderedEdgeListA, orderedEdgeListB);

        //foreach (var filter in meshAFilters)
        //{
        //var mesh = filter.sharedMesh;
        ////var triangles = mesh.vertices;

        //var vertices = mesh.vertices;
        //var worldVertices = vertices.Select(v => filter.transform.localToWorldMatrix.MultiplyPoint3x4(v)).ToList();
        //Dictionary<int, Vector3> edgeVertices = new Dictionary<int, Vector3>();
        //aEdgeVertices.AddRange(GetEdgeVerticesAtDirection(worldVertices, direction, edgeVertices));
        //}

        //List<Vector3> bEdgeVertices = new List<Vector3>();
        //foreach (var filter in meshBFilters)
        //{
        //    bEdgeVertices.AddRange(GetEdgeVerticesAtDirection(filter, -direction));
        //}
        //var filter = meshFilters[0];

        //Debug.Log(startEdge);





        //CombineInstance[] combine = new CombineInstance[meshAFilters.Count + meshBFilters.Count];
        //List<MeshFilter> allFilters = new List<MeshFilter>();
        //allFilters.AddRange(meshAFilters);
        //allFilters.AddRange(meshBFilters);
        //for (int i = 0; i < combine.Length; i++)
        //{
        //    combine[i].mesh = allFilters[i].sharedMesh;
        //    combine[i].transform = allFilters[i].transform.localToWorldMatrix;

        //}

        //ObjectA.SetActive(false);
        //ObjectB.SetActive(false);
        //Mesh combined = new Mesh();
        //combined.CombineMeshes(combine);

        //// Try to weld them 
        //var attributes = new List<EVertexAttribute>()
        //{
        //    EVertexAttribute.Position,
        //    EVertexAttribute.Tangent,
        //    EVertexAttribute.Normal
        //};
        //MeshWelder.WeldMesh(combined, attributes);

        //// Add to a new game object
        //GameObject goCombined = new GameObject();
        //goCombined.name = "Combined meshes";
        //goCombined.AddComponent<MeshFilter>();
        //goCombined.AddComponent<MeshRenderer>();
        //goCombined.GetComponent<MeshFilter>().mesh = combined;

    }

    float GetPositionInDirection(Vector3 direction, Vector3 position)
    {
        Vector3 resultPosition = Vector3.Scale(direction, position);
        return resultPosition.x + resultPosition.y + resultPosition.z;

    }

    private bool[,] GetConnectionArray(Vector3[] vertices, int[] triangles)
    {
        connections = new bool[vertices.Length, vertices.Length];

        Debug.Log(triangles.Max());

        //Triangles are stored in an array. The values of this array are the indexes for the indices of the vertex array
        //Every 3 consecutive indices, starting from 0 make 1 triangle
        //for exampel triangles[0], triangles[1] and triangles[2] define the vertex indices for the first triangle
        //the second triangls is defined by triangles[3], triangles[4] and triangles[5]
        for (int i = 0; i < triangles.Length; i += 3)
        {
            List<int> vertexIndices = new List<int>();
            vertexIndices.Add(triangles[i]);
            vertexIndices.Add(triangles[i + 1]);
            vertexIndices.Add(triangles[i + 2]);

            CrossReferenceSelf(vertexIndices, connections);
        }

        return connections;

    }

    List<int> GetOrderedEdgeList(Dictionary<int, Vector3> edge, bool[,] connections)
    {
        List<int> vertexIndices = new List<int>();
        List<int> verticesToCheck = new List<int>(edge.Keys);

        Vector3 maxVert = edge.Values.OrderBy(p => p.y).First();

        int maxVertIndex = edge.First(p => p.Value == maxVert).Key;

        vertexIndices.Add(maxVertIndex);
        verticesToCheck.Remove(maxVertIndex);

        int counter = 0;
        while (verticesToCheck.Count > 0 && counter < 9999)
        {
            int indexToCheck = verticesToCheck[counter];
            bool connected = connections[vertexIndices.Last(), indexToCheck];
            if (connected)
            {
                vertexIndices.Add(verticesToCheck[counter]);
                verticesToCheck.Remove(verticesToCheck[counter]);
                counter = 0;
            }
            else counter++;
        }

        return vertexIndices;
    }

    void MoveVerticesEdge(MeshFilter meshFilterA, MeshFilter meshFilterB, List<int> edgeA, List<int> edgeB)
    {
        Mesh meshA = meshFilterA.mesh;
        Mesh meshB = meshFilterB.mesh;

        if (edgeA.Count != edgeB.Count)
        {
            Debug.Log("the edges don't have the same amount of vertices");
            return;
        }
        Vector3[] verticesA = meshA.vertices.Select(v => meshFilterA.transform.localToWorldMatrix.MultiplyPoint3x4(v)).ToArray();
        Vector3[] verticesB = meshB.vertices.Select(v => meshFilterB.transform.localToWorldMatrix.MultiplyPoint3x4(v)).ToArray();
        for (int i = 0; i < edgeA.Count; i++)
        {
            Vector3 vertexA = meshA.vertices[edgeA[i]];
            Vector3 vertexB = meshB.vertices[edgeB[i]];

            Vector3 vertexAverage = (vertexA - vertexB) / 2;

            verticesA[edgeA[i]] = meshFilterA.transform.worldToLocalMatrix.MultiplyPoint3x4(vertexAverage);
            verticesB[edgeB[i]] = meshFilterB.transform.worldToLocalMatrix.MultiplyPoint3x4(vertexAverage); ;
        }
        meshA.vertices = verticesA;
        meshB.vertices = verticesB;

        meshA.RecalculateBounds();
        meshB.RecalculateBounds();
    }

    /// <summary>
    /// Crossreference the elements of a list with themselves, only once and not with the element itself
    /// </summary>
    /// <param name="triangleIndices">list to crossreference</param>
    private void CrossReferenceSelf(List<int> triangleIndices, bool[,] connections)
    {
        for (int i = 0; i < triangleIndices.Count; i++)
        {
            //Crossreference with the next index from i. The indexes before i have allready been checked and we don't want to check i against i
            for (int j = i + 1; j < triangleIndices.Count; j++)
            {
                //Action on the two items in the list
                connections[triangleIndices[i], triangleIndices[j]] = true;
                connections[triangleIndices[j], triangleIndices[i]] = true;

            }
        }
    }

    Dictionary<int, Vector3> GetEdgeVerticesAtDirection(Vector3[] worldVertices, Vector3 direction)
    {
        //var mesh = filter.sharedMesh;
        ////var triangles = mesh.vertices;

        //var vertices = mesh.vertices;
        //var worldVertices = vertices.Select(v => filter.transform.localToWorldMatrix.MultiplyPoint3x4(v));
        Dictionary<int, Vector3> resultVertices = new Dictionary<int, Vector3>();


        float max = worldVertices.Max(v => GetPositionInDirection(direction, v));

        for (int i = 0; i < worldVertices.Length; i++)
        {
            if (Mathf.Abs(GetPositionInDirection(direction, worldVertices[i]) - max) < 0.001f)
            {
                resultVertices.Add(i, worldVertices[i]);
            }
        }



        foreach (var vert in resultVertices.Values)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.transform.position = vert;
            node.transform.localScale = Vector3.one * 0.1f;
        }

        return resultVertices;
    }


    // Update is called once per frame
    void Update()
    {

    }


    //public static Mesh MergeMeshes(Mesh meshA, Mesh meshB)
    //{
    //    // get the meshes vertices
    //    // transform the vertices
    //    // weld
    //    // return
    //}
}
