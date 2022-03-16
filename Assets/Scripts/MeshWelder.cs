using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//based on https://answers.unity.com/questions/1382854/welding-vertices-at-runtime.html
//slightly adjusted to naming convention and improved readibility
public enum EVertexAttribute
{
    Position = 0x0001,
    Normal = 0x0002,
    Tangent = 0x0004,
    Color = 0x0008,
    UV1 = 0x0010,
    UV2 = 0x0020,
    UV3 = 0x0040,
    UV4 = 0x0080,
    BoneWeight = 0x0100,
}
public class Vertex
{
    public Vector3 pos;
    public Vector3 normal;
    public Vector4 tangent;
    public Color color;
    public Vector2 uv1;
    public Vector2 uv2;
    public Vector2 uv3;
    public Vector2 uv4;
    public BoneWeight bWeight;
    public Vertex(Vector3 aPos)
    {
        pos = aPos;
    }
}

/// <summary>
/// Static utility class for welding vertices in a mesh
/// </summary>
public static class MeshWelder
{

    private static Mesh _mesh;
    private static Vertex[] _vertices;
    private static List<Vertex> _newVertices;
    private static int[] _vertexMap;
    private static EVertexAttribute _meshAttributes;

    public static float MaxUVDelta = 0.0001f;
    public static float MaxPositionDelta = 0.001f;
    public static float MaxAngleDelta = 0.01f;
    public static float MaxColorDelta = 1f / 255f;
    public static float MaxBWeightDelta = 0.01f;

    private static List<EVertexAttribute> _attributesToCheck;

    /// <summary>
    /// Check if the mesh has the current attribute and if we want to compare this attribute
    /// </summary>
    /// <param name="attribute">The current attribute to check</param>
    /// <returns>true if attribute should be compared</returns>
    private static bool ShouldCompareAttribute(EVertexAttribute attribute)
    {
        return (_meshAttributes & attribute) != 0 && _attributesToCheck.Contains(attribute);
    }

    /// <summary>
    /// Compare if the color of two vertices are similar withing a tollerance
    /// </summary>
    /// <param name="c1">color of the first vertex</param>
    /// <param name="c2">color of the second vertex</param>
    /// <returns>true if within tolerance</returns>
    private static bool CompareColor(Color c1, Color c2)
    {
        return
            (Mathf.Abs(c1.r - c2.r) <= MaxColorDelta) &&
            (Mathf.Abs(c1.g - c2.g) <= MaxColorDelta) &&
            (Mathf.Abs(c1.b - c2.b) <= MaxColorDelta) &&
            (Mathf.Abs(c1.a - c2.a) <= MaxColorDelta);
    }

    private static bool CompareBoneWeight(BoneWeight v1, BoneWeight v2)
    {
        if (v1.boneIndex0 != v2.boneIndex0 || v1.boneIndex1 != v2.boneIndex1 ||
            v1.boneIndex2 != v2.boneIndex2 || v1.boneIndex3 != v2.boneIndex3) return false;
        if (Mathf.Abs(v1.weight0 - v2.weight0) > MaxBWeightDelta) return false;
        if (Mathf.Abs(v1.weight1 - v2.weight1) > MaxBWeightDelta) return false;
        if (Mathf.Abs(v1.weight2 - v2.weight2) > MaxBWeightDelta) return false;
        if (Mathf.Abs(v1.weight3 - v2.weight3) > MaxBWeightDelta) return false;
        return true;
    }

    /// <summary>
    /// Weld all the vertices of a mesh and remap the triangles if they are duplicates according to the given attributes
    /// </summary>
    /// <param name="mesh">The mesh to weld</param>
    /// <param name="attributesToCheck">The attributes that must be equal for a vertex to weld</param>
    public static void WeldMesh(Mesh mesh, List<EVertexAttribute> attributesToCheck)
    {


        _mesh = mesh;
        _attributesToCheck = attributesToCheck;

        CreateVertexList();
        RemoveDuplicates();
        RemapTriangles();
        AssignNewVertexArray();
    }

    /// <summary>
    /// Find all the duplicates vertices of a mesh according to the given criteria and store them in a mapping collection
    /// </summary>
    public static void RemoveDuplicates()
    {
        _newVertices = new List<Vertex>();
        _vertexMap = new int[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            bool duplicate = false;

            int newVertexIndex = 0;
            //keep searching for duplicates untill it found one or it checked all the new vertices
            while (!duplicate && newVertexIndex < _newVertices.Count)
            {
                //Check if the distance between te two verices squared is smaller than the allowed tollerance
                if (Compare(_vertices[i], _vertices[newVertexIndex]))
                {
                    _vertexMap[i] = newVertexIndex;
                    duplicate = true;
                }
                newVertexIndex++;
            }

            //If the vertex is unique to the rest of ther vertices, store the unique vertex and map to the original vertex list
            if (!duplicate)
            {
                _vertexMap[i] = _newVertices.Count;
                _newVertices.Add(_vertices[i]);
            }
        }
    }

    /// <summary>
    /// Check if two vertices are equal according to preset attributes
    /// </summary>
    /// <param name="v1">Vertex 1</param>
    /// <param name="v2">Vertex 2</param>
    /// <returns></returns>
    private static bool Compare(Vertex v1, Vertex v2)
    {
        if ((v1.pos - v2.pos).sqrMagnitude > MaxPositionDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.Normal) && Vector3.Angle(v1.normal, v2.normal) > MaxAngleDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.Tangent) && Vector3.Angle(v1.tangent, v2.tangent) > MaxAngleDelta || v1.tangent.w != v2.tangent.w) return false;
        if (ShouldCompareAttribute(EVertexAttribute.Color) && !CompareColor(v1.color, v2.color)) return false;
        if (ShouldCompareAttribute(EVertexAttribute.UV1) && (v1.uv1 - v2.uv1).sqrMagnitude > MaxUVDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.UV2) && (v1.uv2 - v2.uv2).sqrMagnitude > MaxUVDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.UV3) && (v1.uv3 - v2.uv3).sqrMagnitude > MaxUVDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.UV4) && (v1.uv4 - v2.uv4).sqrMagnitude > MaxUVDelta) return false;
        if (ShouldCompareAttribute(EVertexAttribute.BoneWeight) && !CompareBoneWeight(v1.bWeight, v2.bWeight)) return false;
        return true;
    }

    /// <summary>
    /// Create a new list of vertices using the mapped vertex information
    /// </summary>
    private static void CreateVertexList()
    {
        var Positions = _mesh.vertices;
        var Normals = _mesh.normals;
        var Tangents = _mesh.tangents;
        var Colors = _mesh.colors;
        var Uv1 = _mesh.uv;
        var Uv2 = _mesh.uv2;
        var Uv3 = _mesh.uv3;
        var Uv4 = _mesh.uv4;
        var BWeights = _mesh.boneWeights;
        _meshAttributes = EVertexAttribute.Position;
        if (Normals != null && Normals.Length > 0) _meshAttributes |= EVertexAttribute.Normal;
        if (Tangents != null && Tangents.Length > 0) _meshAttributes |= EVertexAttribute.Tangent;
        if (Colors != null && Colors.Length > 0) _meshAttributes |= EVertexAttribute.Color;
        if (Uv1 != null && Uv1.Length > 0) _meshAttributes |= EVertexAttribute.UV1;
        if (Uv2 != null && Uv2.Length > 0) _meshAttributes |= EVertexAttribute.UV2;
        if (Uv3 != null && Uv3.Length > 0) _meshAttributes |= EVertexAttribute.UV3;
        if (Uv4 != null && Uv4.Length > 0) _meshAttributes |= EVertexAttribute.UV4;
        if (BWeights != null && BWeights.Length > 0) _meshAttributes |= EVertexAttribute.BoneWeight;

        _vertices = new Vertex[Positions.Length];
        for (int i = 0; i < Positions.Length; i++)
        {
            var v = new Vertex(Positions[i]);
            if (ShouldCompareAttribute(EVertexAttribute.Normal)) v.normal = Normals[i];
            if (ShouldCompareAttribute(EVertexAttribute.Tangent)) v.tangent = Tangents[i];
            if (ShouldCompareAttribute(EVertexAttribute.Color)) v.color = Colors[i];
            if (ShouldCompareAttribute(EVertexAttribute.UV1)) v.uv1 = Uv1[i];
            if (ShouldCompareAttribute(EVertexAttribute.UV2)) v.uv2 = Uv2[i];
            if (ShouldCompareAttribute(EVertexAttribute.UV3)) v.uv3 = Uv3[i];
            if (ShouldCompareAttribute(EVertexAttribute.UV4)) v.uv4 = Uv4[i];
            if (ShouldCompareAttribute(EVertexAttribute.BoneWeight)) v.bWeight = BWeights[i];
            _vertices[i] = v;
        }
    }

    /// <summary>
    /// Remap the triangle indices to the new welded vertices
    /// </summary>
    public static void RemapTriangles()
    {
        //Check all submeshes
        for (int n = 0; n < _mesh.subMeshCount; n++)
        {
            //Get the triangles for the specific submesh
            var tris = _mesh.GetTriangles(n);

            //remap the triangle vertex to the new found unique vertex map
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = _vertexMap[tris[i]];
            }
            _mesh.SetTriangles(tris, n);
        }
    }

    /// <summary>
    /// Assign the new generated vertices to the original mesh
    /// </summary>
    public static void AssignNewVertexArray()
    {
        _mesh.vertices = _newVertices.Select(v => v.pos).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.Normal))
            _mesh.normals = _newVertices.Select(v => v.normal).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.Tangent))
            _mesh.tangents = _newVertices.Select(v => v.tangent).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.Color))
            _mesh.colors = _newVertices.Select(v => v.color).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.UV1))
            _mesh.uv = _newVertices.Select(v => v.uv1).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.UV2))
            _mesh.uv2 = _newVertices.Select(v => v.uv2).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.UV3))
            _mesh.uv3 = _newVertices.Select(v => v.uv3).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.UV4))
            _mesh.uv4 = _newVertices.Select(v => v.uv4).ToArray();
        if (ShouldCompareAttribute(EVertexAttribute.BoneWeight))
            _mesh.boneWeights = _newVertices.Select(v => v.bWeight).ToArray();
    }
}

