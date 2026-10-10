namespace LearnSomething.Client.Views;

[Flags]
public enum ResizeEdges
{
    None = 0,
    Left = 1,
    Right = 2,
    Bottom = 4,
}

public static class WindowResizeMath
{
    // dx/dy: how far the pointer moved since the drag began, in DIPs.
    // Left-edge drags keep the window's right edge fixed: the left side moves
    // by exactly the width that was gained or lost (position is in pixels,
    // size in DIPs, hence the scale).
    public static (double Width, double Height, int PositionX) Compute(
        ResizeEdges edges,
        double startWidth,
        double startHeight,
        int startPositionX,
        double dx,
        double dy,
        double minWidth,
        double minHeight,
        double scale)
    {
        var width = startWidth;
        var height = startHeight;
        var positionX = startPositionX;

        if (edges.HasFlag(ResizeEdges.Right))
        {
            width = Math.Max(minWidth, startWidth + dx);
        }

        if (edges.HasFlag(ResizeEdges.Bottom))
        {
            height = Math.Max(minHeight, startHeight + dy);
        }

        if (edges.HasFlag(ResizeEdges.Left))
        {
            width = Math.Max(minWidth, startWidth - dx);
            positionX = startPositionX + (int)Math.Round((startWidth - width) * scale);
        }

        return (width, height, positionX);
    }
}
