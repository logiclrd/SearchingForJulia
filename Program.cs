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

	const double ReflectionRatio = 0.95;

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

			double A1 = 0.5 * (rnd.NextDouble() + 1);
			double A2 = 0.5 * (rnd.NextDouble() + 1);
			double B1 = 0.5 * (rnd.NextDouble() + .6);
			double B2 = 0.5 * (rnd.NextDouble() + .6);

			double F1 = rnd.NextDouble() * 3 + 7;
			double F2 = rnd.NextDouble() * 3 + F1;
			double G1 = rnd.NextDouble() * 3 + 7;
			double G2 = rnd.NextDouble() * 3 + G1;

			Complex Cardioid(double t)
				=> 0.25 * (2 * Complex.Exp(new Complex(0, t)) - Complex.Exp(new Complex(0, 2 * t)));

			Complex FlashlightPosition(double t)
			{
				return new Complex(
					A1 * Math.Sin(F1 * t) + A2 * Math.Cos(F2 * t),
					B1 * Math.Cos(G1 * t) + B2 * Math.Sin(G2 * t));
			}

			var flashlightBoxes = new Rectangle[MaxIterations];

			int newestFlashlightBoxIndex = 0;

			while (true)
			{
				// Advance flashlight along path
				var c = Cardioid(t * .01);

				var flashlightPt = FlashlightPosition(t);

				double flashlightX = flashlightPt.Real;
				double flashlightY = flashlightPt.Imaginary;

				var flashlightBox = new Rectangle(
					flashlightX - 0.5 * FlashlightImageSize,
					flashlightY - 0.5 * FlashlightImageSize,
					flashlightX + 0.5 * FlashlightImageSize,
					flashlightY + 0.5 * FlashlightImageSize);

				newestFlashlightBoxIndex--;
				if (newestFlashlightBoxIndex < 0)
					newestFlashlightBoxIndex = MaxIterations - 1;

				flashlightBoxes[newestFlashlightBoxIndex] = flashlightBox;

				t += 0.005;

				// Draw frame
				frameCanvas.Clear();

				const double XR = (OutputX2 - OutputX1) / OutputWidth;
				const double YR = (OutputY2 - OutputY1) / OutputHeight;

				for (int y=0; y < OutputHeight; y++)
					for (int x=0; x < OutputWidth; x++)
					{
						var z = new Complex(
							real: x * XR + OutputX1,
							imaginary: OutputY2 - y * YR);

						double reflectionIntensity = 1.0;

						var flashlightIndex = newestFlashlightBoxIndex;

						for (int iter = 0; iter < MaxIterations; iter++)
						{
							double xx = z.Real;
							double yy = z.Imaginary;

							var box = Traces ? flashlightBoxes[flashlightIndex] : flashlightBox;

							flashlightIndex++;
							if (flashlightIndex >= MaxIterations)
								flashlightIndex = 0;

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
										colour = ScaleColour(colour, reflectionIntensity);

										frameCanvas.DrawPoint(x, y, colour);
										break;
									}
								}
							}

							z = z * z + c;

							if (z.Real > 4.8)
								break;

							reflectionIntensity *= ReflectionRatio;
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

	static SKColor ScaleColour(SKColor colour, double magnitude)
	{
		byte r = (byte)double.Clamp(Math.Round(colour.Red * magnitude, MidpointRounding.ToPositiveInfinity), 0, 255);
		byte g = (byte)double.Clamp(Math.Round(colour.Green * magnitude, MidpointRounding.ToPositiveInfinity), 0, 255);
		byte b = (byte)double.Clamp(Math.Round(colour.Blue * magnitude, MidpointRounding.ToPositiveInfinity), 0, 255);

		return new SKColor(r, g, b);
	}
}
