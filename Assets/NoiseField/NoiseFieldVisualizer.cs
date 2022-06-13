using UnityEngine;

namespace MarchingCubes {

sealed class NoiseFieldVisualizer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] Vector3Int _dimensions = new Vector3Int(64, 32, 64);

	[SerializeField] bool animate = true;
	private float timeStampStopped;
	private float timeOffset = 0.0f;
	private bool wireframe = false;
	private float timeRun = 0.0f;

	private Renderer rend;
	public Material opaqueMat;
	public Material wireframeMat;
	[SerializeField] int moveSpeed = 3;
	private float xOffset = 0.0f;
	private float zOffset = 0.0f;

    [SerializeField] float _gridScale = 4.0f / 64;
    [SerializeField] int _triangleBudget = 65536;
    [SerializeField] float _targetValue = 0;

    #endregion

    #region Project asset references

    [SerializeField, HideInInspector] ComputeShader _volumeCompute = null;
    [SerializeField, HideInInspector] ComputeShader _builderCompute = null;

    #endregion

    #region Private members

    int VoxelCount => _dimensions.x * _dimensions.y * _dimensions.z;

    ComputeBuffer _voxelBuffer;
    MeshBuilder _builder;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _voxelBuffer = new ComputeBuffer(VoxelCount, sizeof(float));
        _builder = new MeshBuilder(_dimensions, _triangleBudget, _builderCompute);

		// get shaders
		rend = GetComponent<Renderer>();
	}

    void OnDestroy()
    {
        _voxelBuffer.Dispose();
        _builder.Dispose();
    }

    void Update()
    {
		// Time manipulation
		if (Input.GetKeyUp(KeyCode.Space))
		{
			animate = !animate;
		}
		if (animate)
		{
			timeRun += Time.deltaTime;
		}

		// Offset
		if (Input.GetKey(KeyCode.A))
		{
			xOffset += moveSpeed * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.D))
		{
			xOffset -= moveSpeed * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.W))
		{
			zOffset -= moveSpeed * Time.deltaTime;
		}
		if (Input.GetKey(KeyCode.S))
		{
			zOffset += moveSpeed * Time.deltaTime;
		}

		if (Input.GetKeyUp(KeyCode.Tab))
		{
			wireframe = !wireframe;
			rend.material = wireframe ? wireframeMat : opaqueMat;
		}

		// Noise field update
		_volumeCompute.SetInts("Dims", _dimensions);
        _volumeCompute.SetFloat("Scale", _gridScale);

        _volumeCompute.SetFloat("Time", timeRun);
		_volumeCompute.SetFloat("xOffset", xOffset);
		_volumeCompute.SetFloat("zOffset", zOffset);

		_volumeCompute.SetBuffer(0, "Voxels", _voxelBuffer);
        _volumeCompute.DispatchThreads(0, _dimensions);

        // Isosurface reconstruction
        _builder.BuildIsosurface(_voxelBuffer, _targetValue, _gridScale);
        GetComponent<MeshFilter>().sharedMesh = _builder.Mesh;
    }

    #endregion
}

} // namespace MarchingCubes
