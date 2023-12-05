using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SK8Controller.Generation
{
    [RequireComponent(typeof(MeshFilter))]
    [SelectionBase, DisallowMultipleComponent]
    public sealed class Track : MonoBehaviour
    {
        public bool bakeTrack;
        public Mesh baseMesh;
        public float trackWidth = 30.0f;
        public int cornerResolution = 16;
        public bool closed;

        public List<(Vector3 point, Vector3 normal)> points = new();

        private void OnValidate()
        {
            if (bakeTrack)
            {
                bakeTrack = false;
                BakeTrack();
            }
        }

        private void BakePoints(bool drawGizmos = false)
        {
            this.points.Clear();

            var points = new Transform[transform.childCount];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = transform.GetChild(i);
            }

            for (var i = 0; i < points.Length; i++)
            {
                var p0 = getPoint(i);
                var p1 = getPoint(i + 1);
                var p2 = getPoint(i + 2);

                var a = p0.position;
                var b = p1.position;
                var c = p2.position;

                var d0 = (b - a).normalized;
                var d1 = (c - b).normalized;

                var radius = p1.transform.localScale.magnitude;
                var angle0 = Mathf.Atan2(-d0.z, -d0.x);
                var angle1 = Mathf.Atan2(d1.z, d1.x);
                var deltaAngle = Mathf.Abs(Mathf.DeltaAngle(angle0 * Mathf.Rad2Deg, angle1 * Mathf.Rad2Deg) * Mathf.Deg2Rad);
                var offset = radius / Mathf.Tan(deltaAngle * 0.5f);

                a += d0 * offset;
                b -= d0 * offset;
                c = p1.position + d1 * offset;

                this.points.Add((b, d0));

                var tangent = new Vector3(-d0.z, d0.y, d0.x);
                var flip = Vector3.Dot((c - a).normalized, tangent) < 0.0f;
                if (flip) tangent *= -1.0f;

                var center = b + tangent.normalized * radius;
                if (drawGizmos)
                {
                    Gizmos.DrawLine(center, b);
                    Gizmos.DrawLine(center, c);
                }

                d0 = (c - center).normalized;
                d1 = (b - center).normalized;
                
                angle0 = Mathf.Atan2(d0.z, d0.x);
                angle1 = Mathf.Atan2(d1.z, d1.x);
                
                for (var j = 0; j < cornerResolution; j++)
                {
                    var p = j / (cornerResolution - 1.0f);
                    var angle = Mathf.LerpAngle(angle1 * Mathf.Rad2Deg, angle0 * Mathf.Rad2Deg, p) * Mathf.Deg2Rad;
                    var v = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * radius;
                    var t = new Vector3(-v.z, v.y, v.x) * (flip ? -1.0f : 1.0f);
                    
                    this.points.Add((center + v, t));
                }
            }

            if (closed) this.points.Add(this.points[0]);

            (Vector3 point, Vector3 tangent) spline(Vector3 p0, Vector3 p1, Vector3 p2, int i)
            {
                var t = i / (float)cornerResolution;
                var t2 = t * t;

                var point = p0 +
                            t * (-2 * p0 + 2 * p1) +
                            t2 * (1 * p0 - 2 * p1 + 1 * p2);

                var tangent = 1 * (-2 * p0 + 2 * p1) +
                              2 * t * (1 * p0 - 2 * p1 + 1 * p2);

                return (point, tangent);
            }

            Transform getPoint(int i)
            {
                var c = points.Length;
                return points[(i % c + c) % c];
            }
        }


        private void BakeTrack()
        {
            BakePoints();

            var meshFilter = GetComponent<MeshFilter>();
            if (!meshFilter) return;

            var mesh = meshFilter.sharedMesh;
            if (!mesh)
            {
                mesh = new Mesh();
                meshFilter.sharedMesh = mesh;
            }

            var collider = GetComponent<MeshCollider>();
            if (collider)
            {
                collider.sharedMesh = mesh;
            }

            mesh.name = "TrackMesh";

            var segmentHead = 0;

            var vertices = new Vector3[points.Count * baseMesh.vertexCount];
            var normals = new Vector3[points.Count * baseMesh.vertexCount];
            var uvs = new Vector2[points.Count * baseMesh.vertexCount];
            var indices = new int[points.Count * baseMesh.triangles.Length];

            for (var i = 0; i < points.Count - 1; i++)
            {
                var (p0, t0) = getPoint(i);
                var (p1, t1) = getPoint(i + 1);

                appendSegment(p0, p1, t0, t1);
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();

            (Vector3, Vector3) getPoint(int i) => points[i];

            void appendSegment(Vector3 p0, Vector3 p1, Vector3 t0, Vector3 t1)
            {
                var a = Matrix4x4.TRS(p0, Quaternion.LookRotation(t0, Vector3.up), Vector3.one);
                var b = Matrix4x4.TRS(p1, Quaternion.LookRotation(t1, Vector3.up), Vector3.one);

                var basis = baseMesh.vertexCount * segmentHead;
                for (var i = 0; i < baseMesh.vertexCount; i++)
                {
                    var vertex = baseMesh.vertices[i];
                    var p = Mathf.InverseLerp(baseMesh.bounds.min.z, baseMesh.bounds.max.z, vertex.z);
                    vertex.z = 0.0f;

                    var normal = baseMesh.normals[i];

                    vertices[basis + i] = Vector3.Lerp(a.MultiplyPoint(vertex), b.MultiplyPoint(vertex), p);
                    normals[basis + i] = Vector3.Lerp(a.MultiplyVector(normal), b.MultiplyVector(normal), p);
                    uvs[basis + i] = baseMesh.uv[i];
                }

                for (var i = 0; i < baseMesh.triangles.Length; i++)
                {
                    indices[baseMesh.triangles.Length * segmentHead + i] = basis + baseMesh.triangles[i];
                }

                segmentHead++;
            }
        }

        private void OnDrawGizmos()
        {
            BakePoints(true);

            Gizmos.color = Color.yellow;
            for (var i = 0; i < points.Count - 1; i++)
            {
                var (p0, n0) = points[i];
                var (p1, n1) = points[i + 1];

                var t0 = new Vector3(-n0.z, n0.y, n0.x);
                var t1 = new Vector3(-n1.z, n1.y, n1.x);

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p0 + t0.normalized * trackWidth * 0.5f, p1 + t1.normalized * trackWidth * 0.5f);
                Gizmos.DrawLine(p0 - t0.normalized * trackWidth * 0.5f, p1 - t1.normalized * trackWidth * 0.5f);
            }
        }
    }
}