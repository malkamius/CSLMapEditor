using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Mapper2;

internal class Drawer
{
    public class Box
    {
        public RoomWrapper wrapper { get; set; }

        public int x { get; set; }

        public int y { get; set; }

        public int width { get; set; }

        public int height { get; set; }

        public string text { get; set; } = "";


        public Brush BackColor { get; internal set; } = Brushes.White;

    }

    public static List<Box> Boxes { get; set; } = new List<Box>();


    public static Point Origin { get; set; } = Point.Empty;


    public static Bitmap Draw()
    {
        int minX = Boxes.Min((Box b) => b.x);
        int minY = Boxes.Min((Box b) => b.y);
        int width = Math.Abs(minX) + Boxes.Max((Box b) => b.x + b.width) + 100;
        int height = Math.Abs(minY) + Boxes.Max((Box b) => b.y + b.height) + 100;
        Origin = new Point(Math.Abs(minX) + 50, Math.Abs(minY) + 50);
        Bitmap bmp = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bmp);
        using Font font = new Font("Courier New", 8f);
        using Pen pen = new Pen(Color.Black);
        graphics.Clear(Color.White);
        foreach (Box box in Boxes)
        {
            int x = box.x + Origin.X;
            int y = box.y + Origin.Y;
            graphics.FillRectangle(box.BackColor, x, y, box.width, box.height);
            graphics.DrawRectangle(pen, x, y, box.width, box.height);
            graphics.DrawString(box.text, font, Brushes.Black, new RectangleF(x, y, box.width, box.height));
        }
        return bmp;
    }
}
