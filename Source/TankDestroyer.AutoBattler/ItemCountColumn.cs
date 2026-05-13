using Spectre.Console;
using Spectre.Console.Rendering;

namespace TankDestroyer.AutoBattler;

public class ItemCountColumn : ProgressColumn
{
    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        return new Text($"{task.Value}/{task.MaxValue}", new Style(Color.Grey));
    }
}