
using UnityEngine;

namespace SK8Controller.Utilities
{
    public static class Extensions
    {
        public static T Find<T>(this Transform transform, string path)
        {
            var find = transform.Find(path);
            return find ? find.GetComponent<T>() : default;
        }
    }
}