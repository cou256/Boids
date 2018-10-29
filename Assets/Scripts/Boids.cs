using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boids : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] int count;
    [SerializeField] float maxSpeed;
    [SerializeField] float distance;

    [SerializeField] float bounds;
    [SerializeField] Transform centerObject;
    [SerializeField] Transform boundsWall;

    [SerializeField] Transform up, down, left, right, front, back;

    List<Boid> boids = new List<Boid>();

    void Start()
    {
        for (var i = 0; i < count; i++)
        {
            var t = Instantiate(prefab).transform;
            t.parent = transform;
            t.localPosition = Vector3.zero;
            var b = new Boid(t, maxSpeed);
            boids.Add(b);
        }
        boundsWall.localScale = Vector3.one * bounds;
    }
    void Update()
    {
        var center = ComputeCenter();
        centerObject.position = center;

        ComputeCenterDirection(center);
        CertainRange();
        ComputeAverageVector();
        CheckBound();

        for (var i = 0; i < boids.Count; i++)
        {
            boids[i].Update();
        }
    }
    Vector3 ComputeCenter()
    {
        var center = Vector3.zero;
        for (var i = 0; i < boids.Count; i++)
        {
            if (boids[i].Boss) continue;
            center += boids[i].Transform.position;
        }
        center /= boids.Count - 1;
        center += boids[0].Transform.position;
        center *= 0.5f;
        return center;
    }
    void ComputeCenterDirection(Vector3 center)
    {
        for (var i = 0; i < boids.Count; i++)
        {
            if (boids[i].Boss) continue;
            var dir = (center - boids[i].Transform.position);
            boids[i].ApplyForce(dir);
        }
    }
    void CertainRange()
    {
        for (var i = 0; i < boids.Count; i++)
        {
            for (var j = 0; j < boids.Count; j++)
            {
                var a = boids[i];
                var b = boids[j];
                if (a == b) continue;
                var diff = a.Transform.position - b.Transform.position;
                var dis = Random.Range(0, distance);
                if (diff.magnitude < dis)
                {
                    boids[i].ApplyForce(diff);
                }
            }
        }
    }
    void ComputeAverageVector()
    {
        var average = Vector3.zero;
        for (var i = 0; i < boids.Count; i++)
        {
            average += boids[i].Velocity;
        }
        var dir = (average / boids.Count);
        for (var i = 0; i < boids.Count; i++)
        {
            if (boids[i].Boss) continue;
            boids[i].ApplyForce(dir);
        }
    }
    void CheckBound()
    {
        for (var i = 0; i < boids.Count; i++)
        {
            var b = boids[i];
            if (b.X > right.position.x)
            {
                b.X = right.position.x;
                b.Reflection(0);
            }
            if (b.X < left.position.x)
            {
                b.X = left.position.x;
                b.Reflection(0);
            }
            if (b.Y > up.position.y)
            {
                b.Y = up.position.y;
                b.Reflection(1);
            }
            if (b.Y < down.position.y)
            {
                b.Y = down.position.y;
                b.Reflection(1);
            }
            if (b.Z > back.position.z)
            {
                b.Z = back.position.z;
                b.Reflection(2);
            }
            if (b.Z < front.position.z)
            {
                b.Z = front.position.z;
                b.Reflection(2);
            }
        }
    }
    Vector3 RandV(float seed = 1.0f)
    {
        return new Vector3(
                Random.Range(-seed, seed),
                Random.Range(-seed, seed),
                Random.Range(-seed, seed));
    }
    class Boid
    {
        public Transform Transform { get; }
        public Vector3 Velocity { get { return velocity; } }
        public bool Boss;
        public float X { get { return Transform.position.x; } set { SetPos(0, value); } }
        public float Y { get { return Transform.position.y; } set { SetPos(1, value); } }
        public float Z { get { return Transform.position.z; } set { SetPos(2, value); } }

        float maxSpeed;
        Vector3 acceleration;
        Vector3 velocity;
        public Boid(Transform transform, float maxSpeed)
        {
            Transform = transform;
            this.maxSpeed = Random.Range(0.3f, maxSpeed);
            ApplyForce(Random.rotation * transform.forward);
        }
        public void Update()
        {
            velocity += acceleration.normalized * Time.deltaTime;
            velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
            velocity.y = Mathf.Clamp(velocity.y, -maxSpeed, maxSpeed);
            velocity.z = Mathf.Clamp(velocity.z, -maxSpeed, maxSpeed);
            Transform.LookAt(Transform.position + velocity);
            Transform.position += velocity;
            acceleration = Vector3.zero;
        }
        public void ApplyForce(Vector3 force)
        {
            acceleration += force;
        }
        public void Reflection(int index)
        {
            velocity[index] *= -1.0f;
        }
        void SetPos(int index, float val)
        {
            var p = Transform.position;
            p[index] = val;
            Transform.position = p;
        }
    }
}