using System.Runtime.InteropServices;
using UnityEngine;

namespace GPU
{
    public class Boids : MonoBehaviour
    {
        [SerializeField] int numberOfDraw;
        [SerializeField] Vector3 scale;
        [SerializeField] float acceleration;
        [SerializeField] float separateWeight;
        [SerializeField] float cohesionWeight;
        [SerializeField] float alignWeight;
        [SerializeField] float boundaryWeight;
        [SerializeField] float separateNeighborDistance;
        [SerializeField] float cohesionNeighborDistance;
        [SerializeField] float alignNeighborDistance;
        [SerializeField] Vector3 bounds;
        [SerializeField] Mesh mesh;
        [SerializeField] Material mat;
        [SerializeField] ComputeShader kernel;

        ComputeBuffer transformBuff;
        ComputeBuffer argsBuff;

        const string INIT = "Initialize";
        const string FORCE = "ForceCompute";
        const string BOIDS = "BoidsCompute";
        const string BUFF = "_TransformBuff";
        int thread32, thread1024;

        TransformStruct[] data;

        void Awake()
        {
            transformBuff = CreateComputeBuffer(new TransformStruct[numberOfDraw]);
            thread32 = numberOfDraw / 32 + 1;
            thread1024 = numberOfDraw / 1024 + 1;
            SetData();
            SetBuffers();
            data = new TransformStruct[numberOfDraw];
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
            kernel.Dispatch(kernel.FindKernel(INIT), thread1024, 1, 1);
        }
        void Update()
        {
            SetArgs();
            kernel.Dispatch(kernel.FindKernel(FORCE), thread32, thread32, 1);
            kernel.Dispatch(kernel.FindKernel(BOIDS), thread1024, 1, 1);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, new Bounds(Vector3.zero, bounds), argsBuff);
        }
        ComputeBuffer CreateComputeBuffer<T>(T[] data, ComputeBufferType type = ComputeBufferType.Default)
        {
            var computeBuffer = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(T)), type);
            computeBuffer.SetData(data);
            return computeBuffer;
        }
        void SetData()
        {
            var args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)numberOfDraw;
            argsBuff = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuff.SetData(args);
        }
        void SetBuffer(string kernelName, string buffName, ComputeBuffer buff)
        {
            kernel.SetBuffer(kernel.FindKernel(kernelName), buffName, buff);
            mat.SetBuffer(buffName, buff);
        }
        void SetArgs()
        {
            kernel.SetVector("_Bounds", bounds);
            kernel.SetVector("_Scale", scale);
            kernel.SetFloat("_Acceleration", acceleration);
            kernel.SetFloat("_SeparateWeight", separateWeight);
            kernel.SetFloat("_CohesionWeight", cohesionWeight);
            kernel.SetFloat("_AlignWeight", alignWeight);
            kernel.SetFloat("_BoundaryWeight", boundaryWeight);
            kernel.SetFloat("_SeparateNeighborDistance", separateNeighborDistance);
            kernel.SetFloat("_CohesionNeighborDistance", cohesionNeighborDistance);
            kernel.SetFloat("_AlignNeighborDistance", alignNeighborDistance);
            mat.SetVector("_Scale", scale);
        }
        void SetBuffers()
        {
            SetBuffer(INIT, BUFF, transformBuff);
            SetBuffer(FORCE, BUFF, transformBuff);
            SetBuffer(BOIDS, BUFF, transformBuff);
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
