using System.Collections.Generic;
using SK8Controller.Utility;
using UnityEngine;

namespace SK8Controller.Generation
{
    [RequireComponent(typeof(MeshFilter))]
    [SelectionBase, DisallowMultipleComponent]
    public sealed class Track : MonoBehaviour
    {
        public float trackWidth;
        public float cornerRadius;
        public int resolution = 8;

        private Transform pointsContainer;
        private TrackPoint[] points;

        private void OnValidate()
        {
            ValidateHierarchy();
        }

        private void OnDrawGizmos()
        {
            OnValidate();

            Utility.Drawing.Color(Color.magenta);
            var offset = Vector3.right * trackWidth * 0.5f;
            for (var i = 0; i < Count; i++)
            {
                var a = this[i];
                var b = this[i + 1];
                
                Utility.Drawing.DrawLine(a + a.orientation * offset, b + b.orientation * offset, 5.0f);
                Utility.Drawing.DrawLine(a - a.orientation * offset, b - b.orientation * offset, 5.0f);
            }
        }

        public int Count => points.Length;

        public TrackPoint this[int i]
        {
            get
            {
                if (points == null) OnValidate();
                var c = points.Length;
                return points[(i % c + c) % c];
            }
        }

        private void ValidateHierarchy()
        {
            pointsContainer = transform.Find("Points");
            if (!pointsContainer)
            {
                pointsContainer = new GameObject("Points").transform;
                pointsContainer.SetParent(transform);
                pointsContainer.localPosition = Vector3.zero;
                pointsContainer.localRotation = Quaternion.identity;
            }

            for (var i = 0; i < pointsContainer.childCount; i++)
            {
                var child = pointsContainer.GetChild(i);
                child.name = $"Point.{i + 1}";
            }

            BakePoints();
        }

        private void BakePoints()
        {
            var radius = cornerRadius;
            for (var i = 0; i < pointsContainer.childCount; i++)
            {
                radius = Mathf.Min(radius, (get(i) - get(i + 1)).magnitude / 2.0f);
            }

            points = new TrackPoint[pointsContainer.childCount * resolution];
            for (var i = 0; i < pointsContainer.childCount; i++)
            {
                var prev = get(i - 1);
                var current = get(i);
                var next = get(i + 1);

                var dir0 = (prev - current).normalized;
                var dir1 = (next - current).normalized;

                var a = current + dir0 * radius;
                var b = current;
                var c = current + dir1 * radius;

                for (var j = 0; j < resolution; j++)
                {
                    var t = j / (resolution - 1.0f);
                    points[i * resolution + j] = new TrackPoint
                    {
                        position = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t)
                    };
                }
            }

            for (var i = 0; i < points.Length; i++)
            {
                var a = points.Wrap(i - 1);
                var b = points.Wrap(i + 1); 
                points[i].orientation = Quaternion.LookRotation(b.position - a.position, Vector3.up); 
            }

            Vector3 get(int i)
            {
                var c = pointsContainer.childCount;
                return pointsContainer.GetChild((i % c + c) % c).position;
            }
        }

        public struct TrackPoint
        {
            public Vector3 position;
            public Quaternion orientation;

            public static implicit operator Vector3(TrackPoint point) => point.position;

            public TrackPoint(Vector3 position, Quaternion orientation)
            {
                this.position = position;
                this.orientation = orientation;
            }
        }
    }
}