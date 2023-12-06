using System;
using System.Collections.Generic;
using UnityEngine;

namespace SK8Controller.Generation
{
    public class Track : MonoBehaviour
    {
        public bool closed;
        
        private Transform container;

        public static readonly List<Track> Instances = new();

        public Transform this[int i]
        {
            get
            {
                var c = Count;
                return Container.GetChild((i % c + c) % c);
            }
        }
        public int Count => Container.childCount;
        public Transform Container
        {
            get
            {
                if (!container) container = transform.Find("Points");
                if (!container)
                {
                    container = new GameObject("Points").transform;
                    container.transform.SetParent(transform);
                    container.localPosition = Vector3.zero;
                    container.localRotation = Quaternion.identity;
                    container.localScale = Vector3.one;
                }
                return container;
            }
        }

        private void OnEnable() { Instances.Add(this); }

        private void OnDisable() { Instances.Remove(this); }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.magenta;
            for (var i = 0; i < (closed ? Count : Count - 1); i++)
            {
                Gizmos.DrawLine(this[i].position, this[i + 1].position);
            }
        }
    }
}