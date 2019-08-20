namespace EarthBot.Models
{
    public struct LocatedObject<T> : IHasLocation<T>
    {
        public T Object { get; }

        public int Zoom { get; }

        public double Latitude { get; }

        public double Longtitude { get; }

        public LocatedObject(T @object, double latitude, double longtitude, int zoom)
        {
            Object = @object;
            Latitude = latitude;
            Longtitude = longtitude;
            Zoom = zoom;
        }
    }
}