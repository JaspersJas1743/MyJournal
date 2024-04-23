using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;

namespace MyJournal.Desktop.Assets.Resources.Transitions;

public class MyPageSlide : PageSlide
{
    public enum Direction
    {
        Left,
        Right
    }

    public Direction AnimationDirection { get; set; }

    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        => await base.Start(from: from, to: to, forward: forward && AnimationDirection == Direction.Left, cancellationToken: cancellationToken);
}