namespace EarthBot.Models
{
    public struct LocatedObject<T> : IHasLocation<T>
    {
        private readonly T _object;
        private readonly double _latitude;
        private readonly double _longtitude;

        public LocatedObject(T @object, double latitude, double longtitude)
        {
            _object = @object;
            _latitude = latitude;
            _longtitude = longtitude;
        }

        public T GetObject()
        {
            return _object;
        }

        public double GetLatitude()
        {
            return _latitude;
        }

        public double GetLongitude()
        {
            return _longtitude;
        }
    }
}