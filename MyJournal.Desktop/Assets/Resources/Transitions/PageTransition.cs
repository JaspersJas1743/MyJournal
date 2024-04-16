using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace MyJournal.Desktop.Assets.Resources.Transitions;

public sealed class PageTransition : IPageTransition
{
    public enum Direction
    {
        Left,
        Right
    }

    public PageTransition()
    { }

    public PageTransition(TimeSpan duration, Direction direction = Direction.Left)
    {
        Duration = duration;
        AnimationDirection = direction;
    }

    public TimeSpan Duration { get; set; }
    public Direction AnimationDirection { get; set; }
    public Easing SlideInEasing { get; set; } = new LinearEasing();
    public Easing SlideOutEasing { get; set; } = new LinearEasing();

   public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        forward = forward && AnimationDirection == Direction.Left;
        List<Task> tasks = new List<Task>();
        Visual parent = GetVisualParent(from: from, to: to);
        double distance = parent.Bounds.Width;
        StyledProperty<double> translateProperty = TranslateTransform.XProperty;

        if (from is not null)
        {
            Animation animation = new Animation
            {
                Easing = SlideOutEasing,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = translateProperty,
                                Value = 0d
                            }
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = translateProperty,
                                Value = forward ? -distance : distance
                            }
                        },
                        Cue = new Cue(1d)
                    }
                },
                Duration = Duration
            };
            tasks.Add(item: animation.RunAsync(control: from, cancellationToken: cancellationToken));
        }

        if (to is not null)
        {
            to.IsVisible = true;
            Animation animation = new Animation
            {
                FillMode = FillMode.Forward,
                Easing = SlideInEasing,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = translateProperty,
                                Value = forward ? distance : -distance
                            }
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = translateProperty,
                                Value = 0d
                            }
                        },
                        Cue = new Cue(1d)
                    }
                },
                Duration = Duration
            };
            tasks.Add(item: animation.RunAsync(control: to, cancellationToken: cancellationToken));
        }

        await Task.WhenAll(tasks: tasks);

        if (from is not null && !cancellationToken.IsCancellationRequested)
            from.IsVisible = false;
    }

    private static Visual GetVisualParent(Visual? from, Visual? to)
    {
        Visual? p1 = (from ?? to)?.GetVisualParent();
        Visual? p2 = (to ?? from)?.GetVisualParent();

        if (p1 != null && p2 != null && p1 != p2)
            throw new ArgumentException(message: "Controls for PageSlide must have same parent.");
        return p1;
    }
}