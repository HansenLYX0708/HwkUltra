using HWKUltra.Motion.Abstractions;

public class ZAxis : ISingleAxis
{
    public void Init()
    {
        Console.WriteLine("ZAxis Init");
    }

    public void MoveTo(double pos)
    {
        Console.WriteLine($"ZAxis MoveTo {pos}");
    }

    public void Stop() { }

    public bool IsBusy() => false;
}