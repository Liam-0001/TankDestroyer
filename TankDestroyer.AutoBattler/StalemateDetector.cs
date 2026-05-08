using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler;

public class StalemateDetector
{
    private readonly int _windowSize;
    private readonly int _threshold;
    private readonly Dictionary<int, Queue<(int X, int Y)>> _positionHistory = new();

    public StalemateDetector(int windowSize, int threshold)
    {
        _windowSize = windowSize;
        _threshold = threshold;
    }

    public void Track(Tank[] tanks)
    {
        foreach (var tank in tanks)
        {
            if (tank.Destroyed)
                continue;

            if (!_positionHistory.TryGetValue(tank.OwnerId, out var history))
            {
                history = new Queue<(int X, int Y)>();
                _positionHistory[tank.OwnerId] = history;
            }

            history.Enqueue((tank.X, tank.Y));
            if (history.Count > _windowSize)
                history.Dequeue();
        }
    }

    public bool IsStalemate(Tank[] tanks, Bullet[] bullets)
    {
        if (bullets.Length > 0)
            return false;

        var survivors = tanks.Where(t => !t.Destroyed).ToList();
        if (survivors.Count == 0)
            return false;

        return survivors.All(t =>
            _positionHistory.TryGetValue(t.OwnerId, out var h) && h.Count == _windowSize && h.Distinct().Count() <= _threshold
        );
    }
}
