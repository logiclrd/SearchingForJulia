struct Colour
{
	public int R, G, B;

	public Colour() {}

	public Colour(int r, int g, int b)
	{
		R = r;
		G = g;
		B = b;
	}

	public Colour(double r, double g, double b)
	{
		R = (int)Math.Floor(r * 256);
		G = (int)Math.Floor(g * 256);
		B = (int)Math.Floor(b * 256);
	}

	public Colour(int packed)
	{
		R = (packed & 0xFF0000) >> 16;
		G = (packed & 0x00FF00) >> 8;
		B = (packed & 0x0000FF);
	}

	public Colour(uint packed)
		: this(unchecked((int)packed))
	{
	}

	public uint ToPackedBGRA()
	{
		unchecked
		{
			return
				0xFF000000 | // full alpha
				((uint)int.Clamp(R, 0, 255) << 16) |
				((uint)int.Clamp(G, 0, 255) << 8) |
				((uint)int.Clamp(B, 0, 255));
		}
	}
}
