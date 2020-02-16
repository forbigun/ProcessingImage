using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace ImageProcessing
{
    public partial class Form1 : Form
    {
        // список готовых картинок для всех состояний (1...100)
        private List<Bitmap> _bitmaps = new List<Bitmap>();
        private Random rand = new Random();
        CancellationTokenSource cancellationTokenSource;
        public Form1()
        {
            InitializeComponent();
        }

        private async void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var sw = Stopwatch.StartNew();
                menuStrip1.Items[0].Enabled = trackBar1.Enabled = false;
                pictureBox1.Image = null;
                _bitmaps.Clear();
                var bitmap = new Bitmap(openFileDialog1.FileName);
                cancellationTokenSource = new CancellationTokenSource();
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                await Task.Run(() =>
                {
                    this.Invoke(new Action(() =>
                    {
                        остановитьПроцессОбработкиToolStripMenuItem.Enabled = true;
                    }));
                    RunProcessing(bitmap, cancellationToken);
                });
                остановитьПроцессОбработкиToolStripMenuItem.Enabled = false;
                menuStrip1.Items[0].Enabled = trackBar1.Enabled = true;
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                Text = String.Format("На обработку изображения было затрачено {0:00}:{1:00}:{2:00}",ts.Hours,ts.Minutes,ts.Seconds);
            }
        }
        
        // Из открытой картинки получаем массив пикселей
        private List<Pixel> GetPixels(Bitmap bitmap)
        {
            var pixels = new List<Pixel>(bitmap.Width*bitmap.Height);
            for(int y = 0; y < bitmap.Height; y++)
            {
                for(int x = 0;x<bitmap.Width;x++)
                {
                    pixels.Add(new Pixel()
                    {
                        Color =  bitmap.GetPixel(x,y),
                        Point = new Point() { X=x,Y=y}
                    });
                }
            }
            return pixels;
        }

        private  void RunProcessing(Bitmap bitmap, CancellationToken token)
        {
            // получаем лист пикселей
            var pixels = GetPixels(bitmap);
            //число пикселей на каждом шаге
            var pixelsInStep = (bitmap.Width * bitmap.Height) / 100;
            var currentPixels = new List<Pixel>(pixels.Count-pixelsInStep);
            for (int i = 1; i < trackBar1.Maximum; i++) 
            {
                if (token.IsCancellationRequested)
                    return;

                for (int j = 0; j < pixelsInStep; j++)
                {
                    var index = rand.Next(pixels.Count);
                    currentPixels.Add(pixels[index]);
                    pixels.RemoveAt(index);
                }
                var currentBitmap = new Bitmap(bitmap.Width,bitmap.Height);

                foreach (var pixel in currentPixels)
                    currentBitmap.SetPixel(pixel.Point.X,pixel.Point.Y,pixel.Color);
                _bitmaps.Add(currentBitmap);
                this.Invoke(new Action(()=>
                {
                    Text = $"Идет обработка картинки {i + 1}%";
                }));
            }
            _bitmaps.Add(bitmap);
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            if (_bitmaps == null || _bitmaps.Count == 0)
                return;
            Text = $"Показано {trackBar1.Value.ToString()}% изображения";
            pictureBox1.Image = _bitmaps[trackBar1.Value-1];
        }

        private void остановитьПроцессОбработкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}
