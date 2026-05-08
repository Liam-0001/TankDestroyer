using TankDestroyer.Engine;

namespace TankDestroyer.AutoBattler;

public class StalemateDetector
{
    private readonly Dictionary<int, Queue<(int X, int Y)>> _history = new();
    private static int MaxHistory => Program.StalemateWindowSize * 10;
    private static int ConfinedThreshold => Program.StalematePositionThreshold + 1; // 3 positions for a-b-c loops

    public bool IsStalemate(Tank[] tanks)
    {
        bool anyActive = false;
        foreach (var tank in tanks)
        {
            if (tank.Destroyed)
            {
                _history.Remove(tank.OwnerId);
                continue;
            }

            anyActive = true;
            if (!_history.TryGetValue(tank.OwnerId, out var queue))
                _history[tank.OwnerId] = queue = new Queue<(int, int)>();

            queue.Enqueue((tank.X, tank.Y));
            if (queue.Count > MaxHistory) queue.Dequeue();
        }

        if (!anyActive) return false;

        // Count tanks currently "moving" (more than 1 unique position in the long window)
        int movingCount = 0;
        foreach (var queue in _history.Values)
        {
            if (GetUniqueCount(queue, MaxHistory) > 1)
                movingCount++;
        }

        // If only one tank is moving, we use a 10x larger window to filter out slow progress
        int window = (movingCount == 1) ? MaxHistory : Program.StalemateWindowSize;

        foreach (var tank in tanks)
        {
            if (tank.Destroyed) continue;
            
            var queue = _history[tank.OwnerId];
            if (queue.Count < window) return false;

            if (GetUniqueCount(queue, window) > ConfinedThreshold)
                return false;
        }

        return true;
    }

    private static int GetUniqueCount(Queue<(int X, int Y)> history, int window)
    {
        int skip = Math.Max(0, history.Count - window);
        (int X, int Y)? p1 = null, p2 = null, p3 = null;
        
        int i = 0;
        foreach (var p in history)
        {
            if (i++ < skip) continue;

            if (p1 == null) p1 = p;
            else if (p != p1 && p2 == null) p2 = p;
            else if (p != p1 && p != p2 && p3 == null) p3 = p;
            else if (p != p1 && p != p2 && p != p3) return 4;
        }
        
        if (p3 != null) return 3;
        if (p2 != null) return 2;
        return p1 != null ? 1 : 0;
    }
}
