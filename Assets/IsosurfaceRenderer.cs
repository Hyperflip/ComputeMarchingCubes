using UnityEngine;
using UnityEngine.Rendering;

namespace MarchingCube {

sealed class IsosurfaceRenderer : MonoBehaviour
{
    [SerializeField] float _targetValue = 0;

    [SerializeField] Material _material = null;
    [SerializeField] ComputeShader _meshConstructor = null;
    [SerializeField] ComputeShader _meshConverter = null;

    const int Size = 32;
    const int VertexBudget = 64 * 3 * 1000;

    ComputeBuffer _triangleTable;
    ComputeBuffer _voxelBuffer;
    ComputeBuffer _triangleBuffer;
    ComputeBuffer _countBuffer;

    Mesh _mesh;
    GraphicsBuffer _vertexBuffer;
    GraphicsBuffer _indexBuffer;

    void Start()
    {
        _mesh = new Mesh();

        // We want GraphicsBuffer access as Raw (ByteAddress) buffers.
        _mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        _mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;

        // Vertex position: float32 x 3
        var vp = new VertexAttributeDescriptor
          (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        // Vertex normal: float32 x 3
        var vn = new VertexAttributeDescriptor
          (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        // Vertex/index buffer formats
        _mesh.SetVertexBufferParams(VertexBudget, vp, vn);
        _mesh.SetIndexBufferParams(VertexBudget, IndexFormat.UInt32);

        // Submesh initialization
        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, VertexBudget),
                         MeshUpdateFlags.DontRecalculateBounds);

        // Big bounds to avoid getting culled
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

        // GraphicsBuffer references
        _vertexBuffer = _mesh.GetVertexBuffer(0);
        _indexBuffer = _mesh.GetIndexBuffer();

        //
        //

        _triangleTable = new ComputeBuffer(256, 8);
        _triangleTable.SetData(PrecalculatedData.TriangleTable);

        _voxelBuffer = new ComputeBuffer(Size * Size * Size * Size, 4);
        _voxelBuffer.SetData(Util.GenerateDummyData(Size));

        _triangleBuffer = new ComputeBuffer
          (10000, sizeof(float) * 3 * 3, ComputeBufferType.Append);

        _countBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        _meshConstructor.SetInts("Dims", Size, Size, Size);
        _meshConstructor.SetFloat("IsoValue", _targetValue);
        _meshConstructor.SetBuffer(0, "TriangleTable", _triangleTable);
        _meshConstructor.SetBuffer(0, "Voxels", _voxelBuffer);
        _meshConstructor.SetBuffer(0, "Output", _triangleBuffer);
        _meshConstructor.Dispatch(0, Size / 8, Size / 8, Size / 8);

        ComputeBuffer.CopyCount(_triangleBuffer, _countBuffer, 0);

        _meshConverter.SetBuffer(0, "Input", _triangleBuffer);
        _meshConverter.SetBuffer(0, "Count", _countBuffer);
        _meshConverter.SetBuffer(0, "VertexBuffer", _vertexBuffer);
        _meshConverter.SetBuffer(0, "IndexBuffer", _indexBuffer);
        _meshConverter.Dispatch(0, VertexBudget / 3 / 64, 1, 1);
    }

    void OnDestroy()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        Destroy(_mesh);

        _triangleTable.Dispose();
        _voxelBuffer.Dispose();
        _triangleBuffer.Dispose();
        _countBuffer.Dispose();
    }

    void Update()
    {
        Graphics.DrawMesh
          (_mesh, transform.localToWorldMatrix, _material, gameObject.layer);
        
    }
}

} // namespace MarchingCube