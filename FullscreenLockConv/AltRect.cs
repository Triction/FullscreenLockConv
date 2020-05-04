using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FullscreenLockConv
{
    public struct AltRect : IEquatable<AltRect>
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }

        public AltRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public AltRect(System.Drawing.Rectangle rectangle) : this(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom) { }

        public int X
        {
            get { return Left; }
            set { Right -= (Left - value); Left = value; }
        }

        public int Y
        {
            get { return Top; }
            set { Bottom -= (Top - value); Top = value; }
        }

        public int Height
        {
            get { return Bottom - Top; }
            set { Bottom = value + Top; }
        }

        public int Width
        {
            get { return Right - Left; }
            set { Right = value + Left; }
        }

        public System.Drawing.Point Location
        {
            get { return new System.Drawing.Point(Left, Top); }
            set { X = value.X; Y = value.Y; }
        }

        public System.Drawing.Size Size
        {
            get { return new System.Drawing.Size(Width, Height); }
            set { Width = value.Width; Height = value.Height; }
        }

        public static System.Drawing.Rectangle ToRectangle(AltRect r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static implicit operator System.Drawing.Rectangle(AltRect r)
        {
            return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
        }

        public static AltRect ToAltRect(System.Drawing.Rectangle r)
        {
            return new AltRect(r);
        }

        public static implicit operator AltRect(System.Drawing.Rectangle r)
        {
            return new AltRect(r);
        }

        public static bool operator ==(AltRect r1, AltRect r2)
        {
            return r1.Equals(r2);
        }

        public static bool operator !=(AltRect r1, AltRect r2)
        {
            return !r1.Equals(r2);
        }

        public bool Equals(AltRect r)
        {
            return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
        }

        public override bool Equals(object obj)
        {
            if (obj is AltRect)
                return Equals((AltRect)obj);
            else if (obj is System.Drawing.Rectangle)
                return Equals(new AltRect((System.Drawing.Rectangle)obj));

            return false;
        }

        public override int GetHashCode()
        {
            return ((System.Drawing.Rectangle)this).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
        }
    }
}
