namespace Debugger
{
    public partial class MainForm : Form
    {
        private int txtHeight = 24;

        private const int rows = 20; // 行数
        private const int cols = 10; // 列数
        private bool[,] pixelData = new bool[rows, cols]; // 存储像素状态

        public MainForm()
        {
            InitializeComponent();

            this.Size = new Size(300, 600 + txtHeight);
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
            int cellHeight = (this.ClientSize.Height - txtHeight) / rows;

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

        private void txtData_TextChanged(object sender, EventArgs e)
        {
            var dataArray = txtData.Text.Split(',');
            for (int i = 0; i < dataArray.Length; i++)
                UpdateUI(i, Convert.ToInt32(dataArray[i]));
        }
    }
}
