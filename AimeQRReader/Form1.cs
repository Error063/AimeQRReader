using AForge.Video.DirectShow;
using System;
using System.Text.Json;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using ZXing;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Media;
using System.Reflection;
using System.Resources;

namespace AimeQRReader
{
    public partial class Form1 : Form
    {
        FilterInfoCollection videoDevices;
        VideoCaptureDevice videoSource;
        const int delaytime = 100;
        string aimePath = "./DEVICE/aime.txt";
        string configPath = "./config.aimeqr.json";

        public Form1()
        {
            InitializeComponent();
        }

        public class Config
        {
            public string AimePath { get; set; }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1_Click(null, null);
            if (!File.Exists(configPath))
            {
                File.WriteAllText(configPath, JsonSerializer.Serialize(new Config
                {
                    AimePath = "./DEVICE/aime.txt",
                }));
                return;
            }

            var configJson = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<Config>(configJson);

            if (string.IsNullOrEmpty(config.AimePath) || !File.Exists(config.AimePath))
            {
                aimeFilePathBtn_Click(null, null);
            }
            else
            {
                aimePath = config.AimePath;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            for (int i = 0; i < videoDevices.Count; i++)
                comboBox1.Items.Add(videoDevices[i].Name);
            comboBox1.Text = comboBox1.Items[0].ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (File.Exists(aimePath))
            {
                if (comboBox1.Text == null)
                    return;
                ShutCamera();
                Console.WriteLine(videoDevices[comboBox1.SelectedIndex].MonikerString);
                videoSource = new VideoCaptureDevice(videoDevices[comboBox1.SelectedIndex].MonikerString);
                videoSourcePlayer1.VideoSource = videoSource;
                videoSourcePlayer1.Start();
                timer1.Interval = delaytime;
                timer1.Start();
            }
            else
            {
                MessageBox.Show("aime.txt文件不存在！", "错误");
                aimeFilePathBtn_Click(null, null);
                return;
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ShutCamera();
            timer1.Stop();
        }

        public void ShutCamera()
        {
            if (videoSourcePlayer1.VideoSource != null)
            {
                videoSourcePlayer1.SignalToStop();
                videoSourcePlayer1.WaitForStop();
                videoSourcePlayer1.VideoSource = null;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Bitmap picture;
            picture = videoSourcePlayer1.GetCurrentVideoFrame();

            if (picture != null)
            {
                BarcodeReader reader = new BarcodeReader();
                reader.Options.CharacterSet = "UTF-8";
                Result resultQR = reader.Decode(picture);
                if (resultQR != null)
                {
                    ShutCamera();
                    textBox1.AppendText("QR Content: " + resultQR.Text + "\r\n");
                    PlaySound();
                    if(resultQR.Text.Length == 20 && new Regex("^[1-9]\\d*$").Match(resultQR.Text).Success)
                    {
                        File.WriteAllText(aimePath, resultQR.Text);
                    }
                    Keyboard.longPressKey(Keys.Enter, 500);
                    Thread.Sleep(2000);
                    button2_Click(null, null);
                    return;
                }
            }
        }

        private void PlaySound()
        {
            using (var memoryStream = new MemoryStream(Convert.FromBase64String(Properties.Resources.sound_bs64)))
            {
                SoundPlayer player = new SoundPlayer(memoryStream);
                player.Play();
            }
        }

        private void aimeFilePathBtn_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.Filter = "aime.txt|aime.txt";
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var fileName = openFileDialog.FileName;
                File.WriteAllText(configPath, JsonSerializer.Serialize(new Config
                {
                    AimePath = fileName,
                }));
                aimePath = fileName;
            }
        }

        private void Form1_Closing(object sender, FormClosingEventArgs e)
        {
            button3_Click(null, null);
        }
    }
    public static class Keyboard
    {

        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(Keys bVk, byte bScan, uint dwFlags, uint dwExtraInfo);
        [DllImport("user32.dll", EntryPoint = "keybd_event", SetLastError = true)]
        private static extern void keybd_event(int bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        public static Task<int> pressKey(int bVk)
        {
            keybd_event(bVk, 0, 0, 0);
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }

        public static Task<int> longPressKey(int bVk, int delay = 100)
        {
            keybd_event(bVk, 0, 0, 0);
            Thread.Sleep(delay);
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }

        public static Task<int> pressKey(Keys bVk)
        {
            keybd_event(bVk, 0, 0, 0);
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }

        public static Task<int> longPressKey(Keys bVk, int delay = 100)
        {
            for (int i = 0; i < delay; i+=5)
            {
                keybd_event(bVk, 0, 0, 0);
                Thread.Sleep(5);
            }
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }

        public static Task<int> Keydown(Keys bVk)
        {
            keybd_event(bVk, 0, 0, 0);
            return Task.FromResult(0);
        }

        public static Task<int> Keydown(int bVk)
        {
            keybd_event(bVk, 0, 0, 0);
            return Task.FromResult(0);
        }

        public static Task<int> Keyup(Keys bVk)
        {
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }

        public static Task<int> Keyup(int bVk)
        {
            keybd_event(bVk, 0, 2, 0);
            return Task.FromResult(0);
        }
    }
}
