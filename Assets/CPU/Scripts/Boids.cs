using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CPU
{
    public class Boids : MonoBehaviour
    {
        [SerializeField] GameObject prefab;
        [SerializeField] bool noise;
        [SerializeField] int count;
        [SerializeField] float minSpeed;
        [SerializeField] float maxSpeed;
        [SerializeField] float fieldOfVision;
        [SerializeField] float cohesion;
        [SerializeField] float separate;
        [SerializeField] float align;

        [SerializeField] float distance;

        [SerializeField] float bounds;
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
                var b = new Boid(t, minSpeed, maxSpeed);
                boids.Add(b);
            }
            boundsWall.localScale = Vector3.one * bounds;
        }
        void Update()
        {
            if (noise) Noise();
            Cohesion();
            Separate();
            Align();
            CheckBound();
            for (var i = 0; i < boids.Count; i++)
            {
                boids[i].Update();
            }
        }
        float dt;
        void Noise()
        {
            cohesion = Mathf.PerlinNoise(10 + dt, 100 + dt);
            separate = Mathf.PerlinNoise(1000 + dt, 10000 + dt);
            align = Mathf.PerlinNoise(10000 + dt, 100000 + dt);
            dt += Time.deltaTime;
        }
        void Cohesion()
        {
            var center = Vector3.zero;
            for (var i = 0; i < boids.Count; i++)
            {
                center += boids[i].Transform.position;
            }
            center /= boids.Count;
            for (var i = 0; i < boids.Count; i++)
            {
                var dir = (center - boids[i].Transform.position);
                boids[i].ApplyForce(dir.normalized * cohesion);
            }
        }
        void Separate()
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
                        boids[i].ApplyForce(diff.normalized * separate);
                    }
                }
            }
        }
        void Align()
        {
            var average = Vector3.zero;
            for (var i = 0; i < boids.Count; i++)
            {
                average += boids[i].Velocity;
            }
            var dir = (average / boids.Count);
            for (var i = 0; i < boids.Count; i++)
            {
                boids[i].ApplyForce(dir.normalized * align);
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
            public float X { get { return Transform.position.x; } set { SetPos(0, value); } }
            public float Y { get { return Transform.position.y; } set { SetPos(1, value); } }
            public float Z { get { return Transform.position.z; } set { SetPos(2, value); } }

            float minSpeed;
            float maxSpeed;
            Vector3 acceleration;
            Vector3 velocity;
            public Boid(Transform transform, float minSpeed, float maxSpeed)
            {
                Transform = transform;
                this.maxSpeed = Random.Range(minSpeed, maxSpeed);
                ApplyForce(Random.rotation * transform.forward);
            }
            public void Update()
            {
                velocity += acceleration * Time.deltaTime;
                Transform.LookAt(Transform.position + velocity.normalized);
                velocity.x = Mathf.Clamp(velocity.x, -maxSpeed, maxSpeed);
                velocity.y = Mathf.Clamp(velocity.y, -maxSpeed, maxSpeed);
                velocity.z = Mathf.Clamp(velocity.z, -maxSpeed, maxSpeed);
                Transform.position += velocity;
                acceleration = Vector3.zero;
            }
            public void ApplyForce(Vector3 force)
            {
                acceleration += force.normalized;
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
}
