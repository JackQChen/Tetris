using System.IO.Ports;

namespace App
{
    public partial class MainForm : Form
    {

        private const int rows = 4; // 行数
        private const int cols = 4; // 列数
        private bool[,] pixelData = new bool[rows, cols]; // 存储像素状态
        private SerialPort serialPort;
        private DataHandler dataHandler;

        public MainForm()
        {
            InitializeComponent();

            this.Size = new Size(400, 400);
            this.dataHandler = new DataHandler();
            this.dataHandler.DataReceived += DataHandler_DataReceived;

            // 初始化串口
            serialPort = new SerialPort("COM2", 9600); // 修改为实际的串口号
            serialPort.DataReceived += OnDataReceived;
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

        private void DataHandler_DataReceived(bool[,] data)
        {
            pixelData = data;
            this.Invoke(new Action(() => this.Invalidate())); // 刷新窗体
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (serialPort.BytesToRead >= 4) // 三个数据字节 + 一个结束符
                {
                    byte[] buffer = new byte[4];
                    serialPort.Read(buffer, 0, 4);

                    // 检查最后一个字节是否为结束符
                    if (buffer[3] == 0xFF)
                    {
                        byte[] dataBytes = new byte[3];
                        Array.Copy(buffer, 0, dataBytes, 0, 3);
                        dataHandler.SetData(dataBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnDataReceived: {ex.Message}");
            }
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

    public class DataHandler
    {

        public event Action<bool[,]> DataReceived;

        public DataHandler()
        {
        }

        public void SetData(byte[] bytes)
        {
            if (bytes.Length != 3 || (byte)(bytes[0] ^ bytes[1]) != bytes[2])
            {
                Console.WriteLine($"Invalid data: Length={bytes.Length}");
                return;
            }

            var count = 0;
            bool[,] arrayData = new bool[4, 4];
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    int bitIndex = row * 4 + col;
                    int byteIndex = bitIndex / 8;
                    int bitPosition = bitIndex % 8;
                    arrayData[row, col] = (bytes[byteIndex] & (1 << bitPosition)) != 0;
                    if (arrayData[row, col])
                        count++;
                }
            }
            if (count != 4)
                return;

            DataReceived?.Invoke(arrayData);

        }

    }
}
