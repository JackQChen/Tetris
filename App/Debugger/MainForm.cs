namespace Debugger
{
    public partial class MainForm : Form
    {

        private const int rows = 20; // 行数
        private const int cols = 10; // 列数
        private bool[,] pixelData = new bool[rows, cols]; // 存储像素状态

        public MainForm()
        {
            InitializeComponent();

            this.Size = new Size(300, 600);
            //双帧缓冲打开
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
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

        void UpdateUI(int column, int data)
        {
            for (int i = 0; i < 20; i++)
                pixelData[19 - i, 9 - column] = (data >> i & 0b1) == 1;
            this.Invoke(new Action(() => this.Invalidate())); // 刷新窗体
        }

        bool isPlay;
        int playIndex;

        string[] logs = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"));

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    isPlay = !isPlay;
                    break;
                case Keys.Left:
                    isPlay = false;
                    playIndex--;
                    break;
                case Keys.Right:
                    isPlay = false;
                    playIndex++;
                    break;
                default: break;
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (playIndex >= logs.Length)
                playIndex = 0;
            else if (playIndex < 0)
                playIndex = logs.Length - 1;
            var dataArray = logs[playIndex].Split(',');
            for (int i = 0; i < dataArray.Length; i++)
                UpdateUI(i, Convert.ToInt32(dataArray[i]));
            this.Text = $"Index : {playIndex}";
            if (isPlay)
                playIndex++;
        }
    }
}
