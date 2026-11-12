using System.Windows;

namespace DesktopFishTank.Pond;

/// <summary>
/// 尾流里的一颗小气泡。用定长数组存放，避免每条鱼每帧都分配对象。
/// </summary>
public struct WakeParticle
{
    public bool Alive;

    /// <summary>世界坐标下的位置。</summary>
    public Point Position;

    public double Age;

    public double Lifetime;

    /// <summary>横向漂移速度（DIP/秒）。</summary>
    public double VelocityX;

    /// <summary>纵向漂移速度（DIP/秒），负值表示上浮。</summary>
    public double VelocityY;

    /// <summary>气泡半径（DIP）。</summary>
    public double Scale;
}
