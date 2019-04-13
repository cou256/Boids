using System.Runtime.InteropServices;
using UnityEngine;

namespace GPU
{
    public class Boids : MonoBehaviour
    {
        [SerializeField] int numberOfDraw;
        [SerializeField] Vector3 scale;
        [SerializeField] float acceleration;
        [SerializeField] float boundaryWeight;
        [SerializeField] Vector3 bounds;
        [SerializeField] Param Separate;
        [SerializeField] Param Cohesion;
        [SerializeField] Param Align;
        [SerializeField] Param Feed;
        [SerializeField] BoidObject boidObject;
        [SerializeField] ComputeShader kernel;
        [SerializeField] Predator predator;

        ComputeBuffer transformBuff;
        ComputeBuffer argsBuff;

        const string INIT = "Initialize";
        const string FORCE = "ForceCompute";
        const string BOIDS = "BoidsCompute";
        const string BUFF = "_TransformBuff";
        int thread0, thread1;
        [SerializeField] bool mouseDown;

        void Awake()
        {
            transformBuff = CreateComputeBuffer(new TransformStruct[numberOfDraw]);
            thread0 = numberOfDraw / 32 + 1;
            thread1 = numberOfDraw / 8 + 1;
            SetData();
            SetBuffers();
        }
        void OnDisable()
        {
            transformBuff.Release();
            argsBuff.Release();
            transformBuff = null;
            argsBuff = null;
        }
        void Start()
        {
            kernel.SetVector("_Bounds", bounds);
            kernel.Dispatch(kernel.FindKernel(INIT), thread0, 1, 1);
        }
        void Update()
        {
            MouseInput();
            SetArgs();
            kernel.Dispatch(kernel.FindKernel(FORCE), thread1, thread1, 1);
            kernel.Dispatch(kernel.FindKernel(BOIDS), thread0, 1, 1);
            Graphics.DrawMeshInstancedIndirect(boidObject.Mesh, 0, boidObject.Material, new Bounds(Vector3.zero, bounds), argsBuff);
        }
        ComputeBuffer CreateComputeBuffer<T>(T[] data, ComputeBufferType type = ComputeBufferType.Default)
        {
            var computeBuffer = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(T)), type);
            computeBuffer.SetData(data);
            return computeBuffer;
        }
        void MouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                mouseDown = true;
            }
            if (Input.GetMouseButtonUp(0))
            {
                mouseDown = false;
            }
        }
        void SetData()
        {
            var args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = boidObject.Mesh.GetIndexCount(0);
            args[1] = (uint)numberOfDraw;
            argsBuff = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuff.SetData(args);
        }
        void SetBuffer(string kernelName, string buffName, ComputeBuffer buff)
        {
            kernel.SetBuffer(kernel.FindKernel(kernelName), buffName, buff);
            boidObject.Material.SetBuffer(buffName, buff);
        }
        void SetArgs()
        {
            kernel.SetVector("_Bounds", bounds);
            kernel.SetFloat("_Speed", acceleration);
            kernel.SetFloat("_SeparateWeight", Separate.Weight);
            kernel.SetFloat("_CohesionWeight", Cohesion.Weight);
            kernel.SetFloat("_AlignWeight", Align.Weight);
            kernel.SetFloat("_BoundaryWeight", boundaryWeight);
            kernel.SetFloat("_SeparateNeighborDistance", Separate.NeighborDistance);
            kernel.SetFloat("_CohesionNeighborDistance", Cohesion.NeighborDistance);
            kernel.SetFloat("_AlignNeighborDistance", Align.NeighborDistance);
            boidObject.Material.SetVector("_Scale", scale);
            if (predator.boids)
            {
                kernel.SetInt("_PredatorCount", predator.boids.numberOfDraw);
                kernel.SetFloat("_EscapeRadius", predator.escapeRadius);
                kernel.SetFloat("_EscapeWeight", predator.escapeWeight);
            }
            if (Feed.Weight > 0 && mouseDown)
            {
                var mpos = Input.mousePosition;
                mpos.z = (bounds.z - Camera.main.transform.position.z) * 0.5f;
                var pos = Camera.main.ScreenToWorldPoint(mpos);
                kernel.SetVector("_FeedPosition", pos);
                kernel.SetFloat("_FeedWeight", Feed.Weight);
                kernel.SetFloat("_FeedNeighborDistance", Feed.NeighborDistance);
            }else{
                kernel.SetFloat("_FeedWeight", 0.0f);
            }
        }
        void SetBuffers()
        {
            SetBuffer(INIT, BUFF, transformBuff);
            SetBuffer(FORCE, BUFF, transformBuff);
            SetBuffer(BOIDS, BUFF, transformBuff);
            if (predator.boids)
            {
                SetBuffer(BOIDS, "_PredatorBuff", predator.boids.transformBuff);
            }
        }
        [System.Serializable]
        struct Param
        {
            public float Weight;
            public float NeighborDistance;
        }
        [System.Serializable]
        struct BoidObject
        {
            public Mesh Mesh;
            public Material Material;
        }
        [System.Serializable]
        struct Predator
        {
            public Boids boids;
            public float escapeRadius;
            public float escapeWeight;
        }
        struct TransformStruct
        {
            Vector3 translate;
            Vector3 rotation;
            Vector3 velocity;
            Vector3 center;
            uint centerCount;
            Vector3 separate;
            uint separateCount;
            Vector3 velocitySum;
            uint velocitySumCount;
        }
    }
}
