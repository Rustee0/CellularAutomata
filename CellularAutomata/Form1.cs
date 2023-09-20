using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace CellularAutomata
{
    public partial class Form1 : Form
    {
        public Graphics g;
        Bitmap bitmap;
        Random rand;

        public SolidBrush[] brushes = { new SolidBrush(Color.SlateGray), new SolidBrush(Color.SandyBrown) };
        public Color[] colors = new Color[] { Color.SlateGray, Color.FromArgb(255, 245, 214, 181), Color.FromArgb(255, 0, 105, 148), Color.Chocolate, Color.FromArgb(255, 171, 191, 26) };

        public int[] map;

        public int currentCell = 1;
        private int activeCells = 0;
        int penSize = 100;

        bool updateWorldLTR = true; // används för att byta hållet världen uppdateras från

        bool getFps = true; // behöver vara true för att få fps
        int frameCount = 0;
        Stopwatch stopwatch = new Stopwatch();
        double fps = 0;

        public Form1()
        {
            InitializeComponent();
            map = new int[(Width) * (Height)];
            Console.WriteLine((Width * Height).ToString());
            this.DoubleBuffered = true;
            rand = new Random();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            bitmap = new Bitmap(Width, Height);
            this.KeyPress += new KeyPressEventHandler(Form1_KeyPress);
            this.KeyPreview = true;
            MessageBox.Show("Sand(And other stuff) V.6(inte turbo)\nControls:\n1-4 for different cells\nLeftclick to place rightclick to erase");

            Task.Factory.StartNew(() =>
            {
                while (true)//main game loop
                {
                    UpdateWorld();
                    DrawWorld();
                    GetMouseLeft();
                    Console.WriteLine((Math.Round(fps).ToString()));
                    Console.WriteLine(activeCells.ToString());
                }
            });
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            g = e.Graphics;

            lock (bitmap)
            {
                using (Graphics g = this.CreateGraphics())
                {
                    e.Graphics.DrawImage(bitmap, new Point(0, 0));
                }
            }

            if (getFps)
            {
                if (!stopwatch.IsRunning)
                {
                    stopwatch.Start();
                }

                frameCount++;

                if (frameCount >= 10)
                {
                    stopwatch.Stop();
                    fps = frameCount / stopwatch.Elapsed.TotalSeconds;
                    frameCount = 0;
                    stopwatch.Reset();
                }
            }
        }

        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 48 && e.KeyChar <= 57)
            {
                currentCell = Int32.Parse(e.KeyChar.ToString());
            }
        }

        private void GetMouseLeft()
        {
            if ((Control.MouseButtons & MouseButtons.Left) != 0)
            {
                int[] mousePos = GetMousePos(this);
                for (int i = -penSize; i <= penSize; i++)
                {
                    int x = mousePos[0] + i;
                    for (int j = -penSize; j <= penSize; j++)
                    {
                        int y = mousePos[1] + j;
                        int index = y * Width + x;
                        if (index >= 0 && index < map.Length && map[index] == 0)
                        {
                            map[index] = currentCell;
                        }
                    }
                }
            }
            else if ((Control.MouseButtons & MouseButtons.Right) != 0)
            {
                int[] mousePos = GetMousePos(this);
                for (int i = -penSize; i <= penSize; i++)
                {
                    int x = mousePos[0] + i;
                    for (int j = -penSize; j <= penSize; j++)
                    {
                        int y = mousePos[1] + j;
                        int index = y * Width + x;
                        if (index >= 0 && index < map.Length)
                        {
                            map[index] = 0;
                        }
                    }
                }
            }
        }

        private int[] GetMousePos(Control control) // klar
        {
            if (control.InvokeRequired)
            {
                return (int[])control.Invoke(new Func<int[]>(() => GetMousePos(control)));
            }
            else
            {
                Point position = control.PointToClient(Cursor.Position);
                return new int[] { position.X, position.Y };
            }
        }

        private void DrawWorld()
        {
            lock (bitmap)
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.SlateGray);
                }

                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                IntPtr pointer = bitmapData.Scan0;
                int bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
                byte[] rgba = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(pointer, rgba, 0, bytes);

                Parallel.For(0, Height, y =>
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (map[(Height - y - 1) * Width + x] == 0)
                            continue;

                        Color color = colors[map[(Height - y - 1) * Width + x]];
                        int index = ((Height - y - 1) * bitmapData.Stride) + (x * 4);
                        rgba[index] = color.B;
                        rgba[index + 1] = color.G;
                        rgba[index + 2] = color.R;
                        rgba[index + 3] = color.A;
                    }
                });
                System.Runtime.InteropServices.Marshal.Copy(rgba, 0, pointer, bytes);
                bitmap.UnlockBits(bitmapData);
            }
            Invalidate();
        }

        void UpdateWorld()
        {
            activeCells = 0;

            if (updateWorldLTR)
            {
                for (int y = Height - 1; y >= 0; y--)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var cell = map[y * Width + x];
                        if (cell == 0)
                            continue;
                        else if (cell == 1)
                        {
                            activeCells++;
                            Sand(x, y);
                        }
                        else if (cell == 2)
                        {
                            activeCells++;
                            Water(x, y);
                        }
                        else if (cell == 4)
                        {
                            activeCells++;
                            Acid(x, y);
                        }
                    }
                }
            }
            else
            {
                for (int y = Height - 1; y >= 0; y--)
                {
                    for (int x = Width - 1; x >= 0; x--)
                    {
                        var cell = map[y * Width + x];

                        if (cell == 0)
                            continue;
                        else if (cell == 1)
                        {
                            activeCells++;
                            Sand(x, y);
                        }
                        else if (cell == 2)
                        {
                            activeCells++;
                            Water(x, y);
                        }
                        else if (cell == 4)
                        {
                            activeCells++;
                            Acid(x, y);
                        }
                    }
                }
            }

            updateWorldLTR = !updateWorldLTR;
        }

        public int GetCellByPos(int x, int y)
        {
            return (map[y * Width + x]);
        }

        void Swap(int mainX, int mainY, int secondX, int secondY)
        {
            int currentCell = map[mainY * Width + mainX];

            map[mainY * Width + mainX] = map[secondY * Width + secondX];
            map[secondY * Width + secondX] = currentCell;
        }

        void Sand(int x, int y)
        {
            if (x < Width && x >= 0 && y < Height && y >= 0 && y + 1 < Height - 39)
            {
                var down = map[(y + 1) * Width + x];
                var downRight = map[(y + 1) * Width + x + 1];
                var downLeft = map[(y + 1) * Width + x - 1];

                if (down + downLeft + downRight == 3) // ändra senare till fasta ämnen
                    return;

                if (down == 0 || down == 2)
                {
                    Swap(x, y, x, y + 1);
                    return;
                }

                if (downLeft == 0 || downLeft == 2 || (downRight == 0 && downLeft == 0))
                {
                    if (downRight == 0 && downLeft == 0)
                    {
                        int choice = rand.Next(2);
                        if (choice == 0)
                        {
                            Swap(x, y, x + 1, y + 1);
                        }
                        else
                        {
                            Swap(x, y, x - 1, y + 1);
                        }
                    }
                    else
                    {
                        Swap(x, y, x - 1, y + 1);
                    }
                }

                if (downRight == 0 || downRight == 2)
                {
                    Swap(x, y, x + 1, y + 1);
                }
            }
        }

        void Water(int x, int y)
        {
            if (x < Width && x >= 0 && y < Height && y >= 0 && y + 1 < Height - 39)
            {
                var down = map[(y + 1) * Width + x];
                var right = map[y * Width + x + 1];
                var left = map[y * Width + x - 1];

                if (down == 2 && right == 2 && left == 2)
                    return;

                if (down == 0)
                {
                    Swap(x, y, x, y + 1);
                    return;
                }

                int rightDistance = 0;
                int leftDistance = 0;

                if (right == 0)
                {
                    for (int i = 1; i <= 7; i++)
                    {
                        if (map[y * Width + x + i] == 0)
                        {
                            rightDistance = i;
                        }
                    }
                }

                if (left == 0)
                {
                    for (int i = 1; i <= 7; i++)
                    {
                        if (map[y * Width + x - i] == 0)
                        {
                            leftDistance = i;
                        }
                    }
                }

                if (rightDistance > 0 && leftDistance > 0)
                {
                    int choice = rand.Next(2);
                    if (choice == 0)
                    {
                        Swap(x, y, x + rightDistance, y);
                    }
                    else if (choice == 1)
                    {
                        Swap(x, y, x - leftDistance, y);
                    }
                }
                else
                {
                    if (rightDistance > 0)
                    {
                        Swap(x, y, x + rightDistance, y);
                    }
                    else
                    {
                        Swap(x, y, x - leftDistance, y);
                    }
                }
            }
        }

        void Acid(int x, int y)
        {
            if (x < Width && x >= 0 && y < Height && y >= 0 && y + 1 < Height - 39)
            {

                int down = map[(y + 1) * Width + x];
                int right = map[y * Width + x + 1];
                int left = map[y * Width + x - 1];
                int up = map[(y - 1) * Width + x];

                if (down + right + left + up == 16)
                    return;

                if (down != 0 && down != 4)
                {
                    map[y * Width + x] = 0;
                    map[(y + 1) * Width + x] = 0;
                    return;
                }
                else if (up != 0 && up != 4)
                {
                    map[y * Width + x] = 0;
                    map[(y - 1) * Width + x] = 0;
                    return;
                }
                else if (left != 0 && left != 4)
                {
                    map[y * Width + x] = 0;
                    map[y * Width + x - 1] = 0;
                    return;
                }
                else if (right != 0 && right != 4)
                {
                    map[y * Width + x] = 0;
                    map[y * Width + x + 1] = 0;
                    return;
                }


                if (down == 0)
                {
                    Swap(x, y, x, y + 1);
                    return;
                }

                int rightDistance = 0;
                int leftDistance = 0;

                if (right == 0)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        if (GetCellByPos(x + i, y) == 0)
                        {
                            rightDistance = i;
                        }
                    }
                }

                if (left == 0)
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        if (GetCellByPos(x - i, y) == 0)
                        {
                            leftDistance = i;
                        }
                    }
                }

                if (rightDistance > 0 && leftDistance > 0)
                {
                    int choice = rand.Next(2);
                    if (choice == 0)
                    {
                        Swap(x, y, x + rightDistance, y);
                    }
                    else if (choice == 1)
                    {
                        Swap(x, y, x - leftDistance, y);
                    }
                }
                else
                {
                    if (rightDistance > 0)
                    {
                        Swap(x, y, x + rightDistance, y);
                    }
                    else
                    {
                        Swap(x, y, x - leftDistance, y);
                    }
                }
            }
        }
    }
}
