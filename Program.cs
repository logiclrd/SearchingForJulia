using System.IO.Compression;
using System.Numerics;

using SkiaSharp;

class Program
{
	const int OutputWidth = 1280;
	const int OutputHeight = 720;

	const double OutputX1 = -1.25 * OutputWidth / OutputHeight;
	const double OutputY1 = -1.25;
	const double OutputX2 = +1.25 * OutputWidth / OutputHeight;
	const double OutputY2 = +1.25;

	const double FlashlightImageSize = 0.1;

	const int MaxIterations = 30;

	const bool Traces = true;

	static void Main()
	{
		var flashlightImage = new Image(64, 64);

		var flashlightColour = new Colour(0xFF, 0xFF, 0xD8); // very bright yellow

		for (int y=0; y < 64; y++)
		{
			double yy = 2 * ((y / 63.0) - 0.5);

			for (int x=0; x < 64; x++)
			{
				double xx = 2 * ((x / 63.0) - 0.5);

				double rr = Math.Sqrt(xx * xx + yy * yy);

				if (rr <= 1)
				{
					var pixelColour = flashlightColour;

					if (rr > 0.92)
					{
						int dropoff = (int)(256 * ((rr - 0.92) / 0.08));

						pixelColour.R -= dropoff;
						pixelColour.G -= dropoff;
						pixelColour.B -= dropoff;
					}

					flashlightImage[x, y] = pixelColour.ToPackedBGRA();
				}
			}
		}

		var frameInfo = new SKImageInfo(OutputWidth, OutputHeight, SKColorType.Bgra8888, SKAlphaType.Opaque);

		using (var frameSurface = SKSurface.Create(frameInfo))
		{
			var frameCanvas = frameSurface.Canvas;

			double t = 0;
			int frameNumber = 0;

			var rnd = new Random();

			double A1 = rnd.NextDouble() * 0.06 + 0.01;
			double A2 = rnd.NextDouble() * 0.06 + 0.01;
			double B1 = rnd.NextDouble() * 0.06 + 0.01;
			double B2 = rnd.NextDouble() * 0.06 + 0.01;

			double F1 = rnd.NextDouble() * 3 + 7;
			double F2 = rnd.NextDouble() * 3 + F1;
			double G1 = rnd.NextDouble() * 3 + 7;
			double G2 = rnd.NextDouble() * 3 + G1;

			Complex Cardioid(double t)
				=> 0.25 * (2 * Complex.Exp(new Complex(0, t)) - Complex.Exp(new Complex(0, 2 * t)));

			Complex Deviation(double t)
			{
				return new Complex(
					A1 * Math.Sin(F1 * t) + A2 * Math.Cos(F2 * t),
					B1 * Math.Cos(G1 * t) + B2 * Math.Sin(G2 * t));
			}

			Queue<Rectangle> flashlightBoxes = new Queue<Rectangle>();

			while (true)
			{
				// Advance flashlight along path
				var c = Cardioid(t);
				var d = Deviation(t);

				var flashlightPt = c + d;

				double flashlightX = flashlightPt.Real;
				double flashlightY = flashlightPt.Imaginary;

				var flashlightBox = new Rectangle(
					flashlightX - 0.5 * FlashlightImageSize,
					flashlightY - 0.5 * FlashlightImageSize,
					flashlightX + 0.5 * FlashlightImageSize,
					flashlightY + 0.5 * FlashlightImageSize);

				if (flashlightBoxes.Count == MaxIterations)
					flashlightBoxes.Dequeue();

				flashlightBoxes.Enqueue(flashlightBox);

				t += 0.02;

				// Draw frame
				frameCanvas.Clear();

				const double XR = (OutputX2 - OutputX1) / OutputWidth;
				const double YR = (OutputY2 - OutputY1) / OutputHeight;

				const double FR = 1.0 / FlashlightImageSize;

				for (int y=0; y < OutputHeight; y++)
					for (int x=0; x < OutputWidth; x++)
					{
						var z = new Complex(
							real: x * XR + OutputX1,
							imaginary: OutputY2 - y * YR);

						// One iteration for "free"
						z = z * z + flashlightPt;

						foreach (var iterBox in flashlightBoxes)
						{
							z = z * z + flashlightPt;

							if (z.Real > 4.8)
								break;

							double xx = z.Real;
							double yy = z.Imaginary;

							var box = Traces ? iterBox : flashlightBox;

							if (box.Contains(z))
							{
								var (px, py) = box.GetNormalizedPoint(z);

								int pxi = (int)(px * flashlightImage.Width);
								int pyi = (int)(py * flashlightImage.Height);

								if ((pxi >= 0) && (pxi < flashlightImage.Width)
								 && (pyi >= 0) && (pyi < flashlightImage.Height))
								{
									var colour = new SKColor(flashlightImage[pxi, pyi]);

									if (colour.Alpha > 0)
									{
										frameCanvas.DrawPoint(x, y, colour);
										break;
									}
								}
							}
						}
					}

				// Output frame
				string frameFileName = "frame" + frameNumber.ToString("d4") + ".png";

				Console.WriteLine(frameFileName);

				frameNumber++;

				using (var frameImage = frameSurface.Snapshot())
				using (var frameBitmap = SKBitmap.FromImage(frameImage))
				using (var frameStream = File.OpenWrite(frameFileName))
					frameBitmap.Encode(frameStream, SKEncodedImageFormat.Png, default);
			}
		}
	}
}
