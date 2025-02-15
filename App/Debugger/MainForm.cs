using System.IO.Ports;

namespace Debugger
{
    public partial class MainForm : Form
    {

        private const int rows = 20; // 行数
        private const int cols = 10; // 列数
        private bool[,] pixelData = new bool[rows, cols]; // 存储像素状态
        private SerialPort serialPort;

        public MainForm()
        {
            InitializeComponent();

            this.Size = new Size(300, 600);
            //双帧缓冲打开
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();

            // 初始化串口
            serialPort = new SerialPort("COM2", 115200); // 修改为实际的串口号
            serialPort.DataReceived += OnDataReceived;
            serialPort.DtrEnable = true;
            serialPort.Open();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // 绘制像素矩阵
            int cellWidth = this.ClientSize.Width / cols;
            int cellHeight = this.ClientSize.Height / rows;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Brush brush = pixelData[i, j] ? Brushes.Black : Brushes.White; // 黑色表示亮，白色表示灭
                    g.FillRectangle(brush, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                    g.DrawRectangle(Pens.Gray, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                }
            }
        }

        int receivedIndex = 0;
        byte[] receivedData = new byte[3];
        byte[] buffer = new byte[1024 * 1024];

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                int bytesRead = serialPort.Read(buffer, 0, bytesToRead);
                for (int i = 0; i < bytesRead; i++)
                {
                    receivedData[receivedIndex] = buffer[i];
                    receivedIndex++;

                    if (receivedIndex == 3)
                    {
                        receivedIndex = 0;
                        UpdateUI(receivedData);
                    }
                }
            }

            //Console.WriteLine($"Data={data}, ChangeType={nextChangeType}, TetrisType={nextTetrisType}");
        }


        void UpdateUI(byte[] buffer)
        {
            var d1 = buffer[0];
            var col = d1 >> 4;
            pixelData[0, col] = (d1 >> 3 & 0b1) == 1;
            pixelData[1, col] = (d1 >> 2 & 0b1) == 1;
            pixelData[2, col] = (d1 >> 1 & 0b1) == 1;
            pixelData[3, col] = (d1 & 0b1) == 1;
            var d2 = buffer[1];
            pixelData[4, col] = (d2 >> 7 & 0b1) == 1;
            pixelData[5, col] = (d2 >> 6 & 0b1) == 1;
            pixelData[6, col] = (d2 >> 5 & 0b1) == 1;
            pixelData[7, col] = (d2 >> 4 & 0b1) == 1;
            pixelData[8, col] = (d2 >> 3 & 0b1) == 1;
            pixelData[9, col] = (d2 >> 2 & 0b1) == 1;
            pixelData[10, col] = (d2 >> 1 & 0b1) == 1;
            pixelData[11, col] = (d2 & 0b1) == 1;
            var d3 = buffer[2];
            pixelData[12, col] = (d3 >> 7 & 0b1) == 1;
            pixelData[13, col] = (d3 >> 6 & 0b1) == 1;
            pixelData[14, col] = (d3 >> 5 & 0b1) == 1;
            pixelData[15, col] = (d3 >> 4 & 0b1) == 1;
            pixelData[16, col] = (d3 >> 3 & 0b1) == 1;
            pixelData[17, col] = (d3 >> 2 & 0b1) == 1;
            pixelData[18, col] = (d3 >> 1 & 0b1) == 1;
            pixelData[19, col] = (d3 & 0b1) == 1;
            this.Invoke(new Action(() => this.Invalidate())); // 刷新窗体
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.DataReceived -= OnDataReceived; // 移除事件处理器
                serialPort.Close(); // 关闭串口
                serialPort.Dispose(); // 释放资源
            }
        }
    }
}
