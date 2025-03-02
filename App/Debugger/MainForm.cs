namespace Debugger
{
    public partial class MainForm : Form
    {

        private const int rows = 20; // 行数
        private const int cols = 10; // 列数
        private bool[,] gridData = new bool[rows, cols]; // 存储像素状态

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
                    if (maxRow == 19 - i)
                        g.FillRectangle(Brushes.Red, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                    if (rectGrid.Top == 19 - i)
                        g.FillRectangle(Brushes.DarkGreen, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                    if (rectGrid.Bottom == 19 - i)
                        g.FillRectangle(Brushes.LightGreen, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                    if (rectGrid.Left == 9 - j)
                        g.FillRectangle(Brushes.DeepSkyBlue, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                    if (rectGrid.Right == 9 - j)
                        g.FillRectangle(Brushes.LightSkyBlue, j * cellWidth, i * cellHeight, cellWidth, cellHeight);

                    if (gridData[i, j])
                        g.FillRectangle(Brushes.Black, j * cellWidth, i * cellHeight, cellWidth, cellHeight);

                    g.DrawRectangle(Pens.Gray, j * cellWidth, i * cellHeight, cellWidth, cellHeight);
                }
            }
        }

        bool isPlay;
        int playIndex;

        int maxRow;
        Rectangle rectGrid;

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
            var log = logs[playIndex];
            if (char.IsLetter(log[0]))
                this.Text = log;
            else
            {
                var dataArray = log.Split(',').Select(int.Parse).ToArray();
                var combined = 0;
                for (int i = 0; i < dataArray.Length; i++)
                    combined |= dataArray[i];

                var range = GetRange(combined);
                maxRow = range.Item1 == 0 ? range.Item2 : -1;

                combined &= 0x00ffffff << (maxRow + 1);
                range = GetRange(combined);
                var startRow = range.Item1;
                var endRow = range.Item2;

                combined = 0;
                for (int i = 0; i < dataArray.Length; i++)
                {
                    if (((0x00ffffff << (maxRow + 1)) & dataArray[i]) > 0)
                        combined |= 1 << i;
                }
                range = GetRange(combined);
                var startColumn = range.Item1;
                var endColumn = range.Item2;
                rectGrid = Rectangle.FromLTRB(startColumn, startRow, endColumn, endRow);

                for (int col = 0; col < dataArray.Length; col++)
                    for (int row = 0; row < 20; row++)
                        gridData[19 - row, 9 - col] = (dataArray[col] >> row & 0b1) == 1;
                this.Invalidate();
                this.Text = $"Frame: {playIndex}";
            }
            if (isPlay)
                playIndex++;
        }

        Tuple<int, int> GetRange(int data)
        {
            var start = -1;
            var end = -1;

            for (int i = 0; i < 20; i++)
            {
                if ((data >> i & 0b1) == 1)
                {
                    if (start == -1)
                        start = i;
                    end = i;
                }
                else
                {
                    if (start != -1)
                        break;
                }
            }
            return Tuple.Create(start, end);
        }
    }
}
