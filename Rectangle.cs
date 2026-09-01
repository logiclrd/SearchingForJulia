using System.Numerics;

struct Rectangle(double x1, double y1, double x2, double y2)
{
	public readonly double X1 = x1, Y1 = y1;
	public readonly double X2 = x2, Y2 = y2;

	public readonly double XR = 1.0 / (x2 - x1);
	public readonly double YR = 1.0 / (y2 - y1);

	public bool Contains(double x, double y)
	{
		return
			(x >= X1) && (x <= X2) &&
			(y >= Y1) && (y <= Y2);
	}

	public (double X, double Y) GetNormalizedCoordinates(double x, double y)
		=> ((x - X1) * XR, (y - Y1) * YR );

	public bool Contains(Complex pt)
		=> Contains(pt.Real, pt.Imaginary);
	public (double X, double Y) GetNormalizedPoint(Complex pt)
		=> GetNormalizedCoordinates(pt.Real, pt.Imaginary);
}
