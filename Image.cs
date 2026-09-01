class Image(int w, int h)
{
	public readonly int Width = w, Height = h;
	public readonly uint[] Pixels = new uint[w * h];

	public uint this[int x, int y]
	{
		get => Pixels[y * w + x];
		set => Pixels[y * w + x] = value;
	}
}
