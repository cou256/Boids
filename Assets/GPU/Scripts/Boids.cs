﻿using System.Runtime.InteropServices;
using UnityEngine;

namespace GPU
{
    public class Boids : MonoBehaviour
    {
        [SerializeField] int numberOfDraw;
        [SerializeField] float acceleration;
        [SerializeField] Vector3 bounds;
        [SerializeField] Mesh mesh;
        [SerializeField] Material mat;
        [SerializeField] ComputeShader kernel;

        ComputeBuffer transformBuff;
        ComputeBuffer argsBuff;

        void OnDisable()
        {
            transformBuff.Release();
            argsBuff.Release();
            transformBuff = null;
            argsBuff = null;
        }
        void Start()
        {
            transformBuff = CreateComputeBuffer(new TransformStruct[numberOfDraw]);

            var args = new uint[5] { 0, 0, 0, 0, 0 };
            args[0] = mesh.GetIndexCount(0);
            args[1] = (uint)numberOfDraw;
            argsBuff = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuff.SetData(args);
            kernel.SetBuffer(kernel.FindKernel("Compute"), "_TransformBuff", transformBuff);

            mat.SetBuffer("_TransformBuff", transformBuff);
        }
        void Update()
        {
            DrawMesh();
        }
        void DrawMesh()
        {
            kernel.SetVector("_Bounds", bounds);
            kernel.SetFloat("_Acceleration", acceleration);
            kernel.Dispatch(kernel.FindKernel("Compute"), numberOfDraw / 512 + 1, 1, 1);
            Graphics.DrawMeshInstancedIndirect(mesh, 0, mat, new Bounds(Vector3.zero, bounds), argsBuff);
        }
        struct TransformStruct
        {
            public Vector3 translate;
            public Vector3 rotation;
            public Vector3 scale;
            public Vector3 acceleration;
            public Vector3 velocity;
            public int init;
        }
        ComputeBuffer CreateComputeBuffer<T>(T[] data, ComputeBufferType type = ComputeBufferType.Default)
        {
            var computeBuffer = new ComputeBuffer(data.Length, Marshal.SizeOf(typeof(T)), type);
            computeBuffer.SetData(data);
            return computeBuffer;
        }
    }
}