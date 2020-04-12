using Unity.Mathematics;

namespace VoxaNovus
{
    public struct RawTextureInfo
    {
        public TexturePosition Up { get; set; }
        public TexturePosition Down { get; set; }
        public TexturePosition North { get; set; }
        public TexturePosition South { get; set; }
        public TexturePosition East { get; set; }
        public TexturePosition West { get; set; }
        public TexturePosition Marched { get; set; }

        public int2[] ToInt2Array()
        {
            return new int2[]
            {
                Up.ToInt2(),
                Down.ToInt2(),
                North.ToInt2(),
                South.ToInt2(),
                East.ToInt2(),
                West.ToInt2(),
                Marched.ToInt2()
            };
        }
    }

    public struct TexturePosition
    {
        public int X { get; set; }
        public int Y { get; set; }

        public TexturePosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public TexturePosition(int2 xy)
        {
            X = xy.x;
            Y = xy.y;
        }

        public int2 ToInt2()
        {
            return new int2(X, Y);
        }
    }

    public struct TextureRect
    {
        public int Width { get; set; }
        public int Height { get; set; }

        public TextureRect(int x, int y)
        {
            Width = x;
            Height = y;
        }

        public TextureRect(int2 xy)
        {
            Width = xy.x;
            Height = xy.y;
        }

        public int2 ToInt2()
        {
            return new int2(Width, Height);
        }
    }
}