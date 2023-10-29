namespace SK8Controller.Utility
{
    public static class Extension
    {
        public static T Wrap<T>(this T[] array, int i)
        {
            var c = array.Length;
            return array[(i % c + c) % c];
        }
    }
}