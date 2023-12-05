using System.Collections.Generic;
using UnityEngine;

namespace SK8Controller.Generation
{
    [RequireComponent(typeof(MeshFilter))]
    [SelectionBase, DisallowMultipleComponent]
    public sealed class Track : MonoBehaviour
    {
        public bool bakeTrack;
        public Mesh baseMesh;
        public float cornerRadius = 30.0f;
        public int cornerResolution = 16;

        public List<(Vector3 point, Vector3 normal)> points = new();
        
        private void OnValidate()
        {
            if (bakeTrack)
            {
                bakeTrack = false;
                BakeTrack();
            }
        }

        private void BakePoints()
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

                var radius = cornerRadius * p1.transform.localScale.magnitude;
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(b, -d0 * 50.0f);
                Gizmos.DrawRay(b, d1 * 50.0f);
                var a0 = Mathf.Atan2(-d0.y, -d0.x);
                var a1 = Mathf.Atan2(d1.y, d1.x);
                var deltaA = Mathf.Abs(Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, a1 * Mathf.Rad2Deg) * Mathf.Deg2Rad);
                Debug.Log($"{a0}, {a1}, {deltaA}");
                var offset = radius / Mathf.Tan(deltaA * 0.5f);
                
                a += d0 * offset;
                b -= d0 * offset;
                c = p1.position + d1 * offset;
                
                this.points.Add((a, d0));
                this.points.Add((b, d0));

                // for (var j = 0; j < cornerResolution + 1; j++)
                // {
                //     this.points.Add(spline(b, p1.position, c, j));
                // }
            }

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

            mesh.name = "TrackMesh";

            var segmentHead = 0;

            var vertices = new Vector3[points.Count * baseMesh.vertexCount];
            var normals = new Vector3[points.Count * baseMesh.vertexCount];
            var uvs = new Vector2[points.Count * baseMesh.vertexCount];
            var indices = new int[points.Count * baseMesh.triangles.Length];

            for (var i = 0; i < points.Count; i++)
            {
                var (p0, t0) = getPoint(i);
                var (p1, t1) = getPoint(i + 1);

                appendSegment(p0, p1, t0, t1);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();

            (Vector3, Vector3) getPoint(int i)
            {
                var c = points.Count;
                return points[(i % c + c) % c];
            }

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
            BakePoints();
            
            var c = points.Count;
            Gizmos.color = Color.yellow;
            for (var i = 0; i < c; i++)
            {
                var a = points[i].point;
                var b = points[((i + 1) % c + c) % c].point;
                Gizmos.DrawLine(a, b);
            }
        }
    }
}