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
        public int resolution;

        private Transform pointsContainer;
        private List<Vector3> points = new();

        public int Count => points.Count;

        public Vector3 this[int i]
        {
            get
            {
                var c = Count;
                if (c == 0) return default;
                return points[(i % c + c) % c];
            }
        }

        private void OnValidate()
        {
            ValidateHierarchy();
        }

        private void OnDrawGizmos()
        {
            OnValidate();

            Utility.Drawing.Color(Color.magenta);
            for (var i = 0; i < points.Count + 1; i++)
            {
                var c = this[i];
                var n = this[i + 1];

                Utility.Drawing.DrawLine(c, n, 5.0f);
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
            var count = pointsContainer.childCount;
            points.Clear();

            for (var i = 0; i < resolution; i++)
            {
                var percent = i / (float)resolution;
                var i0 = Mathf.FloorToInt(percent * count);
                var i1 = i0 + 1;

                var h0 = get(i0);
                var h1 = get(i1);

                var a = h0.position;
                var b = h0.position + h0.forward * h0.localScale.z * cornerRadius;
                var c = h1.position - h1.forward * h1.localScale.z * cornerRadius;
                var d = h1.position;
                var t = Mathf.InverseLerp(i0, i1, percent * count);

                var bezier = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(c, d, t), t);
                points.Add(bezier);
            }

            Transform get(int i)
            {
                var c = pointsContainer.childCount;
                return pointsContainer.GetChild((i % c + c) % c);
            }
        }
    }
}