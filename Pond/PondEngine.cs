using System;
using System.Collections.Generic;
using System.Windows;
using DesktopFish.Rendering;

namespace DesktopFish.Pond;

/// <summary>
/// 鱼群运动模拟：随机漫步 + 边界转向 + 交互模式下的避让。
/// 所有坐标都是窗口 DIP 坐标。
/// </summary>
public sealed class PondEngine
{
    private const double EdgeMargin = 150.0;
    private const double FleeRadius = 200.0;
    private const double MinDt = 0.0001;

    /// <summary>巡游基准速度区间（DIP/秒，乘以远近层级后生效）。</summary>
    private const double CruiseSpeedMin = 105.0;
    private const double CruiseSpeedSpan = 75.0;

    /// <summary>吸引模式的速度倍率，明显快于巡游才看得出「冲过来」。</summary>
    private const double AttractSpeedFactor = 3.0;

    /// <summary>进入该半径后由直冲改为环绕，避免整群鱼叠在光标上。</summary>
    private const double AttractOrbitRadius = 120.0;

    private readonly Random _random = new();
    private readonly List<MechFish> _fish = new();
    private Rect _bounds = new(0, 0, 1024, 768);
    private IReadOnlyList<FishPalette> _palettes = FishPaletteCatalog.All;

    /// <summary>所有鱼的整体不透明度。</summary>
    private double _fishOpacity = 1.0;

    /// <summary>是否绘制鱼周围的光晕。</summary>
    private bool _showHalo = true;

    /// <summary>所有鱼的绘制缩放上限；<see cref="double.MaxValue"/> 表示不限制。</summary>
    private double _maxRenderScale = double.MaxValue;

    public IReadOnlyList<MechFish> Fish => _fish;

    public Rect Bounds => _bounds;

    /// <summary>当前是否处于 Ctrl+Alt 交互状态。</summary>
    public bool MouseReacting { get; set; }

    public Point? MousePosition { get; set; }

    /// <summary>吸引模式：按住快捷键时所有鱼游向 <see cref="AttractTarget"/>。</summary>
    public bool AttractMode { get; set; }

    public Point? AttractTarget { get; set; }

    public MechFish? DraggedFish { get; set; }

    /// <summary>设置所有鱼的整体不透明度，立即作用到已在游动的鱼。</summary>
    public void SetFishOpacity(double opacity)
    {
        _fishOpacity = Math.Clamp(opacity, 0.15, 1.0);

        foreach (MechFish fish in _fish)
        {
            fish.Opacity = _fishOpacity;
        }
    }

    /// <summary>开关鱼周围那层暗淡的荧光光晕。</summary>
    public void SetShowHalo(bool show)
    {
        _showHalo = show;

        foreach (MechFish fish in _fish)
        {
            fish.ShowHalo = _showHalo;
        }
    }

    /// <summary>设置所有鱼的体型上限，立即作用到已在游动的鱼。</summary>
    public void SetMaxRenderScale(double maxRenderScale)
    {
        _maxRenderScale = Math.Clamp(maxRenderScale, 0.4, 3.0);

        foreach (MechFish fish in _fish)
        {
            fish.MaxRenderScale = _maxRenderScale;
        }
    }

    public void SetBounds(Rect bounds)
    {
        if (bounds.Width < 1 || bounds.Height < 1)
        {
            return;
        }

        _bounds = bounds;

        foreach (MechFish fish in _fish)
        {
            fish.Position = new Point(
                Math.Clamp(fish.Position.X, 20, bounds.Width - 20),
                Math.Clamp(fish.Position.Y, 20, bounds.Height - 20));
        }
    }

    /// <summary>设置鱼的数量，保留已存在的鱼，避免数量微调时整群重排。</summary>
    public void SetFishCount(int count)
    {
        count = Math.Clamp(count, 0, 64);

        while (_fish.Count > count)
        {
            _fish.RemoveAt(_fish.Count - 1);
        }

        while (_fish.Count < count)
        {
            _fish.Add(CreateFish());
        }
    }

    public void Update(double dt)
    {
        if (dt <= MinDt || _fish.Count == 0)
        {
            return;
        }

        // 帧间隔异常时（例如窗口最小化后恢复）截断，避免鱼瞬移。
        dt = Math.Min(dt, 0.06);

        foreach (MechFish fish in _fish)
        {
            if (fish.IsDragged)
            {
                fish.Phase += dt * 9.0;
                continue;
            }

            if (AttractMode && AttractTarget is { } target)
            {
                // 吸引优先于巡游与边界避让，否则鱼会被边界拉回去。
                ApplyAttract(fish, target, dt);
            }
            else
            {
                ApplyWander(fish, dt);
                ApplyBoundarySteer(fish, dt);

                if (MouseReacting && MousePosition is { } mouse)
                {
                    ApplyFlee(fish, mouse, dt);
                }
            }

            fish.Heading = NormalizeAngle(fish.Heading);
            fish.Speed += (fish.CruiseSpeed - fish.Speed) * Math.Min(1.0, dt * 3.2);
            fish.Position = new Point(
                fish.Position.X + Math.Cos(fish.Heading) * fish.Speed * dt,
                fish.Position.Y + Math.Sin(fish.Heading) * fish.Speed * dt);
            fish.Phase += dt * (4.2 + fish.Speed * 0.045);

            ClampInside(fish);
        }
    }

    public MechFish? FindFishAt(Point point)
    {
        MechFish? best = null;
        double bestDistance = double.MaxValue;

        foreach (MechFish fish in _fish)
        {
            double dx = fish.Position.X - point.X;
            double dy = fish.Position.Y - point.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance <= fish.HitRadius && distance < bestDistance)
            {
                best = fish;
                bestDistance = distance;
            }
        }

        return best;
    }

    private MechFish CreateFish()
    {
        var fish = new MechFish
        {
            Palette = _palettes[_random.Next(_palettes.Count)],
            OrbitDirection = _random.Next(2) == 0 ? 1 : -1,
            Depth = 0.70 + _random.NextDouble() * 0.30,
            Heading = _random.NextDouble() * Math.PI * 2,
            WanderPhase = _random.NextDouble() * Math.PI * 2,
            Phase = _random.NextDouble() * Math.PI * 2,
            Size = 0.90 + _random.NextDouble() * 0.75,
            MaxRenderScale = _maxRenderScale,
            Opacity = _fishOpacity,
            ShowHalo = _showHalo,
            Position = new Point(
                _random.NextDouble() * Math.Max(1, _bounds.Width),
                _random.NextDouble() * Math.Max(1, _bounds.Height)),
        };

        fish.CruiseSpeed = (CruiseSpeedMin + _random.NextDouble() * CruiseSpeedSpan) * fish.Depth;
        fish.Speed = fish.CruiseSpeed;
        return fish;
    }

    private static void ApplyWander(MechFish fish, double dt)
    {
        fish.WanderPhase += dt * 1.55;
        fish.Heading += Math.Sin(fish.WanderPhase) * 0.62 * dt;
        fish.CruiseSpeed = Math.Max(24, fish.CruiseSpeed);
    }

    private void ApplyBoundarySteer(MechFish fish, double dt)
    {
        double width = _bounds.Width;
        double height = _bounds.Height;
        double steerX = 0;
        double steerY = 0;

        if (fish.Position.X < EdgeMargin)
        {
            steerX += (EdgeMargin - fish.Position.X) / EdgeMargin;
        }
        else if (fish.Position.X > width - EdgeMargin)
        {
            steerX -= (fish.Position.X - (width - EdgeMargin)) / EdgeMargin;
        }

        if (fish.Position.Y < EdgeMargin)
        {
            steerY += (EdgeMargin - fish.Position.Y) / EdgeMargin;
        }
        else if (fish.Position.Y > height - EdgeMargin)
        {
            steerY -= (fish.Position.Y - (height - EdgeMargin)) / EdgeMargin;
        }

        if (Math.Abs(steerX) < 0.0001 && Math.Abs(steerY) < 0.0001)
        {
            return;
        }

        double target = Math.Atan2(steerY, steerX);
        fish.Heading = BlendAngle(fish.Heading, target, Math.Min(1.0, dt * 2.4));
    }

    /// <summary>
    /// 吸引到鼠标：远处直冲目标，进入环绕半径后改为绕着目标转，
    /// 避免所有鱼精确叠在一个像素上抖动。
    /// </summary>
    private static void ApplyAttract(MechFish fish, Point target, double dt)
    {
        double dx = target.X - fish.Position.X;
        double dy = target.Y - fish.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.5)
        {
            return;
        }

        double desired = Math.Atan2(dy, dx);

        if (distance < AttractOrbitRadius)
        {
            double orbit = desired + Math.PI * 0.5 * fish.OrbitDirection;
            fish.Heading = BlendAngle(fish.Heading, orbit, Math.Min(1.0, dt * 3.0));
            fish.Speed = Math.Max(fish.Speed, fish.CruiseSpeed);
            return;
        }

        fish.Heading = BlendAngle(fish.Heading, desired, Math.Min(1.0, dt * 6.0));
        fish.Speed = Math.Max(fish.Speed, fish.CruiseSpeed * AttractSpeedFactor);
    }

    private static void ApplyFlee(MechFish fish, Point mouse, double dt)
    {
        double dx = fish.Position.X - mouse.X;
        double dy = fish.Position.Y - mouse.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 0.01 || distance > FleeRadius)
        {
            return;
        }

        double urgency = 1.0 - distance / FleeRadius;
        double away = Math.Atan2(dy, dx);
        fish.Heading = BlendAngle(fish.Heading, away, Math.Min(1.0, dt * (3.0 + urgency * 7.0)));
        fish.Speed = Math.Max(fish.Speed, fish.CruiseSpeed * (1.8 + urgency * 1.6));
    }

    private void ClampInside(MechFish fish)
    {
        double margin = 8.0;
        fish.Position = new Point(
            Math.Clamp(fish.Position.X, margin, Math.Max(margin, _bounds.Width - margin)),
            Math.Clamp(fish.Position.Y, margin, Math.Max(margin, _bounds.Height - margin)));
    }

    private static double BlendAngle(double from, double to, double t)
    {
        double delta = NormalizeAngle(to - from);
        return from + delta * t;
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle > Math.PI)
        {
            angle -= Math.PI * 2;
        }

        while (angle < -Math.PI)
        {
            angle += Math.PI * 2;
        }

        return angle;
    }
}
