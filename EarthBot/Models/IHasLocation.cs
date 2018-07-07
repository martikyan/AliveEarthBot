namespace EarthBot.Models
{
    public interface IHasLocation<T>
    {
        T GetObject();
        double GetLatitude();
        double GetLongitude();
    }
}