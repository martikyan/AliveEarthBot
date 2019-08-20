namespace EarthBot.Models
{
    public interface IHasLocation<T>
    {
        T Object { get; }

        double Latitude { get; }

        double Longtitude { get; }

        int Zoom { get; }
    }
}