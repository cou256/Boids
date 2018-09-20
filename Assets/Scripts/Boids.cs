using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boids : MonoBehaviour
{
    [SerializeField] GameObject prefab;
    [SerializeField] int count;
    [SerializeField] float speed;
    [SerializeField] float distance;

    [SerializeField] int bounds;
    [SerializeField] GameObject centerObject;
    List<Boid> boids = new List<Boid>();

    void Start()
    {
        for (var i = 0; i < count; i++)
        {
            var t = Instantiate(prefab).transform;
            t.parent = transform;
            t.localPosition = RandV(bounds);
            var b = new Boid();
            b.transform = t;
            boids.Add(b);
        }
    }
    void Update()
    {
        var center = ComputeCenter();
        centerObject.transform.position = center;
        ComputeCenterDirection(center);
        CertainRange();
        ComputeAverageVector();
        for (var i = 0; i < boids.Count; i++)
        {
            var b = boids[i];
            b.transform.position += b.dir.normalized * speed;
            b.transform.LookAt(b.dir);
        }
    }
    Vector3 ComputeCenter()
    {
        var center = Vector3.zero;
        for (var i = 0; i < boids.Count; i++)
        {
            center += boids[i].transform.position;
        }
        center /= boids.Count;
        return center;
    }
    void ComputeCenterDirection(Vector3 center)
    {
        for (var i = 0; i < boids.Count; i++)
        {
            var b = boids[i];
            var dir = (center - b.transform.position).normalized;
            boids[i].dir += dir;
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
                var diff = a.transform.position - b.transform.position;
                var dis = Random.Range(0, distance);
                if (diff.magnitude < dis)
                {
                    boids[i].dir += diff.normalized;
                }
            }
        }
    }
    void ComputeAverageVector()
    {
        var average = Vector3.zero;
        for (var i = 0; i < boids.Count; i++)
        {
            average += boids[i].dir;
        }
        var dir = (average / boids.Count).normalized;
        for (var i = 0; i < boids.Count; i++)
        {
            boids[i].dir += dir;
            boids[i].dir += RandV(10.0f).normalized;
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
        public Transform transform;
        public Vector3 dir;
    }
}