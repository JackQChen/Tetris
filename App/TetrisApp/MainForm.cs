using System.IO.Ports;
using System.Runtime.InteropServices;

namespace TetrisApp
{
    public partial class MainForm : Form
    {
        class Grid
        {
            public bool show;
            public bool running;//下落中的方块
            public int sceneX;
            public int sceneY;
            public Rectangle rect;
        }

        class SceneOffset
        {
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public int X3;
            public int Y3;
            public int X4;
            public int Y4;
        }

        enum TetrisType
        {
            I, J, L, O, S, T, Z
        }
        //网格大小
        const int kGridSize = 32;
        //画布起点
        Point kScenePoint = new Point(10, 20);
        //画布网格数 10x20
        const int kSceneWidth = 10;
        const int kSceneHeight = 20;
        //像素大小
        Size kSceneSize = new Size(kSceneWidth * kGridSize, kSceneHeight * kGridSize);
        //预览框起点
        Point kPreviewPoint = new Point(kSceneWidth * kGridSize + 20, 10);
        //得分栏起点
        Point kScorePoint = new Point(10, 2);
        //预览框大小 4x4
        const int kPreviewWidth = 4;
        const int kPreviewHeight = 4;
        //像素大小
        Size kPreviewSize = new Size(kPreviewWidth * kGridSize, kPreviewHeight * kGridSize);
        //随机数生成器
        Random randGen = new Random();
        //全部网格
        Grid[,] allGrids = new Grid[kSceneWidth, kSceneHeight];
        //预览网格
        Grid[,] preGrids = new Grid[kPreviewWidth, kPreviewHeight];
        //正在下落的方块
        Grid[] runGrids = null;
        //预览方块组
        Grid[] nextPreGrids = new Grid[4];
        //7种组合，在第一块固定的时候，其他块的偏移
        List<SceneOffset>[] tetrisOffset = new List<SceneOffset>[7];
        //局部坐标系，以左上角为原点
        List<SceneOffset>[] localOffset = new List<SceneOffset>[7];
        //变体数量
        int[] changeNum = new int[7];
        //出生点
        int kRunGridBirthX = kSceneWidth / 2 - 2;
        int kRunGridBirthY = 0;
        //掉落位置
        int currentRunGridX = 0;
        int currentRunGridY = 0;
        //预览位置
        int kPreBornX = 0;
        int kPreBornY = 0;
        //预览区的offset
        SceneOffset nextOffset = null;
        //当前的offset
        SceneOffset currentOffset = null;
        //下落速度
        const int dropSpeed = 5;
        const int timerInterval = 10;
        //当前的选型
        int curChangeType;
        int curTetrisType;
        //下次的选型
        int nextChangeType;
        int nextTetrisType = -1;
        //积分系数
        int[] scoreParam = new int[4] { 10, 15, 20, 15 };
        int GameScore = 0;

        //是否使用AI
        bool IsAiControl { get; set; } = true;

        enum GameState
        {
            NormalDrop,//正在掉落
            FastDrop,//快速掉落
            Change,//变换形态
            Destroy,//消除
            Fall,//消除后 上方方块下落
            NextRound,//结束一轮下落，1停止掉落后不消除，2或者消除后方块下落完成
            GameOver,
        }

        //当前游戏状态
        GameState gameState = GameState.NextRound;

        SerialPort serialPort;
        bool isWindows = false;

        DateTime dtLastReceived = DateTime.MinValue;

        public MainForm()
        {
            InitializeComponent();
            InitForm();
            InitGrids();
            InitTetrisType();
        }

        void InitForm()
        {
            //窗口居中
            this.StartPosition = FormStartPosition.CenterScreen;
            //去掉最大化窗口
            this.MaximizeBox = false;
            //禁止拖动
            this.FormBorderStyle = FormBorderStyle.Fixed3D;
            //窗口大小
            this.ClientSize = new Size((kSceneWidth + kPreviewWidth) * kGridSize + 30, (kSceneHeight) * kGridSize + 40);
            //窗口背景
            this.BackColor = SystemColors.Control;
            //双帧缓冲打开
            this.SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
            //定时器
            this.UITimer.Enabled = true;
            this.UITimer.Interval = timerInterval;
            this.UITimer.Tick += OnTimer;
            // 自动重置
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (dtLastReceived != DateTime.MinValue && ((DateTime.Now - dtLastReceived).TotalSeconds > 30))
                    {
                        // 重置
                        serialPort.Write(new byte[] { 0xff }, 0, 1);
                        Restart();
                    }
                    Thread.Sleep(30000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        protected override void OnLoad(EventArgs e)
        {
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // 初始化串口
            serialPort = new SerialPort(isWindows ? "COM2" : "/dev/ttyUSB0", 115200); // 修改为实际的串口号
            serialPort.DataReceived += OnDataReceived;

            serialPort.WriteTimeout = 1;
            serialPort.WriteBufferSize = 1;

            receivedBuffer = new byte[serialPort.ReadBufferSize];

            serialPort.Open();

            base.OnLoad(e);
            Restart();
        }

        int receivedIndex = 0;
        byte[] receivedData = new byte[3];
        byte[] receivedBuffer = Array.Empty<byte>();

        byte[] tetrisData = new byte[2];
        bool[,] bufferGrid = new bool[10, 20];


        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                int bytesRead = serialPort.Read(receivedBuffer, 0, bytesToRead);
                for (int i = 0; i < bytesRead; i++)
                {
                    receivedData[receivedIndex] = receivedBuffer[i];
                    receivedIndex++;

                    if (receivedIndex == 3)
                    {
                        receivedIndex = 0;
                        if (!UpdateGridData(receivedData)) i++;
                    }
                }
            }

            //Console.WriteLine($"Data={data}, ChangeType={nextChangeType}, TetrisType={nextTetrisType}");
        }

        private bool UpdateGridData(byte[] buffer)
        {
            var d1 = buffer[0];
            var col = d1 >> 4;
            if (col > 9)
                return false;
            bufferGrid[col, 0] = (d1 >> 3 & 0b1) == 1;
            bufferGrid[col, 1] = (d1 >> 2 & 0b1) == 1;
            bufferGrid[col, 2] = (d1 >> 1 & 0b1) == 1;
            bufferGrid[col, 3] = (d1 & 0b1) == 1;
            var d2 = buffer[1];
            bufferGrid[col, 4] = (d2 >> 7 & 0b1) == 1;
            bufferGrid[col, 5] = (d2 >> 6 & 0b1) == 1;
            bufferGrid[col, 6] = (d2 >> 5 & 0b1) == 1;
            bufferGrid[col, 7] = (d2 >> 4 & 0b1) == 1;
            bufferGrid[col, 8] = (d2 >> 3 & 0b1) == 1;
            bufferGrid[col, 9] = (d2 >> 2 & 0b1) == 1;
            bufferGrid[col, 10] = (d2 >> 1 & 0b1) == 1;
            bufferGrid[col, 11] = (d2 & 0b1) == 1;
            var d3 = buffer[2];
            bufferGrid[col, 12] = (d3 >> 7 & 0b1) == 1;
            bufferGrid[col, 13] = (d3 >> 6 & 0b1) == 1;
            bufferGrid[col, 14] = (d3 >> 5 & 0b1) == 1;
            bufferGrid[col, 15] = (d3 >> 4 & 0b1) == 1;
            bufferGrid[col, 16] = (d3 >> 3 & 0b1) == 1;
            bufferGrid[col, 17] = (d3 >> 2 & 0b1) == 1;
            bufferGrid[col, 18] = (d3 >> 1 & 0b1) == 1;
            bufferGrid[col, 19] = (d3 & 0b1) == 1;
            if (DateTime.Now - dtLastReceived < TimeSpan.FromSeconds(1))
                return true;
            if (col == 6)
            {
                tetrisData[0] = 0;
                tetrisData[1] = 0;
                var pos = 8;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 4; j++)
                        tetrisData[0] |= (byte)((bufferGrid[j + 3, i] ? 1 : 0) << --pos);
                pos = 8;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 4; j++)
                        tetrisData[1] |= (byte)((bufferGrid[j + 3, i + 2] ? 1 : 0) << --pos);
                var matchedTetris = TetrisMapper.Mapper.FirstOrDefault(p => p[0] == tetrisData[0] && p[1] == tetrisData[1]);
                if (matchedTetris != null)
                {
                    for (int i = 0; i < 10; i++)
                        for (int j = 4; j < 20; j++)
                            allGrids[i, j].show = bufferGrid[i, j];
                    nextChangeType = matchedTetris[2] % 10;
                    nextTetrisType = matchedTetris[2] / 10;
                    nextChangeType = nextChangeType % changeNum[nextTetrisType];
                    nextOffset = tetrisOffset[nextTetrisType][nextChangeType];
                    dtLastReceived = DateTime.Now;
                }
            }
            return true;
        }

        void InitGrids()
        {
            //初始化网格
            for (int i = 0; i < kSceneHeight; i++)
            {
                for (int j = 0; j < kSceneWidth; j++)
                {
                    Grid grid = new Grid();
                    grid.show = false;
                    grid.running = false;
                    grid.sceneX = j;
                    grid.sceneY = i;
                    grid.rect = new Rectangle(kScenePoint.X + kGridSize * j, kScenePoint.Y + kGridSize * i, kGridSize - 1, kGridSize - 1);
                    allGrids[j, i] = grid;
                }
            }

            //初始化预览
            for (int i = 0; i < kPreviewHeight; i++)
            {
                for (int j = 0; j < kPreviewWidth; j++)
                {
                    preGrids[j, i] = new Grid()
                    {
                        show = false,
                        sceneY = i,
                        sceneX = j,
                        rect = new Rectangle(kPreviewPoint.X + kGridSize * j, kPreviewPoint.Y + kGridSize * i, kGridSize - 1, kGridSize - 1),
                    };
                }
            }
        }
        void InitTetrisType()
        {
            //O
            tetrisOffset[3] = new List<SceneOffset>();
            tetrisOffset[3].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            changeNum[3] = 1;

            //I
            tetrisOffset[0] = new List<SceneOffset>();
            tetrisOffset[0].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 1, Y4 = 3 });
            tetrisOffset[0].Add(new SceneOffset() { X1 = 0, Y1 = 1, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 3, Y4 = 1 });
            changeNum[0] = 2;

            //S
            tetrisOffset[4] = new List<SceneOffset>();
            tetrisOffset[4].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[4].Add(new SceneOffset() { X1 = 2, Y1 = 1, X2 = 3, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            changeNum[4] = 2;
            //Z
            tetrisOffset[6] = new List<SceneOffset>();
            tetrisOffset[6].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[6].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            changeNum[6] = 2;
            //L
            tetrisOffset[2] = new List<SceneOffset>();
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[2].Add(new SceneOffset() { X1 = 3, Y1 = 1, X2 = 3, Y2 = 2, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            changeNum[2] = 4;
            //J
            tetrisOffset[1] = new List<SceneOffset>();
            tetrisOffset[1].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 2, Y2 = 1, X3 = 2, Y3 = 2, X4 = 1, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 2, Y2 = 0, X3 = 1, Y3 = 1, X4 = 1, Y4 = 2 });
            tetrisOffset[1].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 3, Y4 = 2 });
            changeNum[1] = 4;
            //T
            tetrisOffset[5] = new List<SceneOffset>();
            tetrisOffset[5].Add(new SceneOffset() { X1 = 1, Y1 = 1, X2 = 2, Y2 = 1, X3 = 3, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 2, Y1 = 0, X2 = 1, Y2 = 1, X3 = 2, Y3 = 1, X4 = 2, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 2, Y1 = 1, X2 = 1, Y2 = 2, X3 = 2, Y3 = 2, X4 = 3, Y4 = 2 });
            tetrisOffset[5].Add(new SceneOffset() { X1 = 1, Y1 = 0, X2 = 1, Y2 = 1, X3 = 1, Y3 = 2, X4 = 2, Y4 = 1 });
            changeNum[5] = 4;

            for (int i = 0; i < tetrisOffset.Length; i++)
            {
                localOffset[i] = new List<SceneOffset>();
                for (int j = 0; j < tetrisOffset[i].Count; j++)
                {
                    SceneOffset offset = tetrisOffset[i][j];
                    localOffset[i].Add(new SceneOffset() { X1 = 0, Y1 = 0, X2 = offset.X2 - offset.X1, Y2 = offset.Y2 - offset.Y1, X3 = offset.X3 - offset.X1, Y3 = offset.Y3 - offset.Y1, X4 = offset.X4 - offset.X1, Y4 = offset.Y4 - offset.Y1 });
                }
            }
        }

        Grid GetGridByPos(int x, int y)
        {
            if (x < 0 || y < 0 || x >= kSceneWidth || y >= kSceneHeight)
            {
                return null;
            }
            return allGrids[x, y];
        }

        //在某个坐标位置生成掉落方块
        Grid[] GetRunGridsAtPos(int x, int y, SceneOffset offset)
        {
            Grid[] grids = new Grid[4];
            grids[0] = GetGridByPos(x + offset.X1, y + offset.Y1);
            grids[1] = GetGridByPos(x + offset.X2, y + offset.Y2);
            grids[2] = GetGridByPos(x + offset.X3, y + offset.Y3);
            grids[3] = GetGridByPos(x + offset.X4, y + offset.Y4);
            return grids;
        }

        //预览区初始化
        void CalcPreGrids()
        {
            SceneOffset offset = nextOffset;

            foreach (Grid g in nextPreGrids)
            {
                if (g != null)
                {
                    g.show = false;
                }
            }

            nextPreGrids[0] = preGrids[kPreBornX + offset.X1, kPreBornY + offset.Y1];
            nextPreGrids[1] = preGrids[kPreBornX + offset.X2, kPreBornY + offset.Y2];
            nextPreGrids[2] = preGrids[kPreBornX + offset.X3, kPreBornY + offset.Y3];
            nextPreGrids[3] = preGrids[kPreBornX + offset.X4, kPreBornY + offset.Y4];
            foreach (Grid g in nextPreGrids)
            {
                g.show = true;
            }
        }

        //重新开始
        void Restart()
        {
            GameScore = 0;
            for (int i = 0; i < kSceneWidth; i++)
            {
                for (int j = 0; j < kSceneHeight; j++)
                {
                    allGrids[i, j].show = false;
                }
            }
            //初始化第一组
            OnNextRound();
            //定时器
            this.UITimer.Start();
        }

        //键盘操作
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            switch (e.KeyCode)
            {
                case Keys.Up:
                    RunGridMove(Direction.UP);
                    break;
                case Keys.Down:
                    gameState = GameState.FastDrop;
                    break;
                case Keys.Left:
                    RunGridMove(Direction.LEFT);
                    break;
                case Keys.Right:
                    RunGridMove(Direction.RIGHT);
                    break;
                case Keys.K:
                    IsAiControl = !IsAiControl;
                    //CalcAICtrl();
                    break;
                case Keys.Space:
                    UITimer.Enabled = !UITimer.Enabled;
                    break;
            }
        }

        int dropCounter = 0;
        void OnTimer(object sender, EventArgs e)
        {
            switch (gameState)
            {
                case GameState.NormalDrop:
                    if (++dropCounter == dropSpeed)//500ms掉落一格
                    {
                        dropCounter = 0;
                        OnDropping();
                    }
                    break;
                case GameState.FastDrop:
                    OnDropping();//100ms掉落一格
                    break;
                case GameState.Destroy:
                    OnDestroy();
                    break;
                case GameState.Fall:
                    Onfall();
                    break;
                case GameState.NextRound:
                    OnNextRound();
                    break;
                case GameState.GameOver:
                    this.UITimer.Stop();
                    Restart();
                    break;
            }

            //重绘整个窗口
            this.Invalidate(new Rectangle(0, 0, this.Size.Width, this.Size.Height));
        }

        //一轮下落的开始
        void OnNextRound()
        {
            currentRunGridX = kRunGridBirthX;
            currentRunGridY = kRunGridBirthY;

            if (nextTetrisType == -1)
                return;
            currentOffset = nextOffset;
            curChangeType = nextChangeType;
            curTetrisType = nextTetrisType;

            //把预览区的offset移到游戏区
            runGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY, currentOffset);
            if (!CheckNextGridValid(runGrids))
            {
                gameState = GameState.GameOver;
                return;
            }
            foreach (Grid g in runGrids)
            {
                g.running = true;
                g.show = true;
            }
            //生成预览区的offset
            //GenerateNextTetris();
            //计算预览区网格
            CalcPreGrids();
            gameState = GameState.NormalDrop;

            if (IsAiControl)
            {
                CalcAICtrl();
                nextTetrisType = -1;
            }
        }

        void CalcAICtrl()
        {
            // CalcAI1();
            CalcAI2();
        }

        class CheckResult
        {
            public int Change;//变形次数
            public int X;//检查位置
            public int MatchShape;//形状匹配
            public int EraseLine;//消除行数
            public int Height;//最终高度
            public int ChangeType;
        }

        //计算一个结果最好的X坐标落下去
        //1.选形状完全匹配的，如果有多个匹配进入2；
        //2.选消除行数最多的，如果有多个匹配进入3；
        //3.选高度最低的，如果有多个匹配选第一个；
        void CalcAI1()
        {
            //先把正在下落的格子显示状态抹除
            foreach (var g in runGrids)
            {
                g.show = false;
            }
            List<CheckResult> results = new List<CheckResult>();
            int changeType = curChangeType;
            for (int change = 0; change < changeNum[curTetrisType]; change++)
            {
                SceneOffset local_offset = localOffset[curTetrisType][changeType];

                for (int x = 0; x < kSceneWidth; x++)
                {
                    Grid[] lastValidGrids = null;
                    for (int y = 0; y < kSceneHeight; y++)
                    {
                        var showGrids = GetRunGridsAtPos(x, y, local_offset);
                        if (!CheckAIGridValid(showGrids))
                        {
                            break;
                        }
                        lastValidGrids = showGrids;
                    }
                    if (lastValidGrids != null)
                    {
                        CheckResult result = new CheckResult();
                        result.Change = change;
                        result.ChangeType = changeType;
                        result.X = x;
                        int minY = kSceneHeight - 1;
                        bool matchShape = true;
                        Dictionary<int, bool> lineY = new Dictionary<int, bool>();

                        foreach (var g in lastValidGrids)
                        {
                            //计算高度
                            if (g.sceneY < minY)
                            {
                                minY = g.sceneY;
                            }
                            //匹配形状
                            if (g.sceneY + 1 < kSceneHeight)
                            {
                                Grid nextGrid = allGrids[g.sceneX, g.sceneY + 1];
                                if (!GridsContainsGrid(lastValidGrids, nextGrid) && !nextGrid.show)
                                {
                                    matchShape = false;
                                }
                            }
                            //计算消除行数
                            if (!lineY.ContainsKey(g.sceneY))
                            {
                                lineY.Add(g.sceneY, true);
                                if (CheckLineYFinished(g.sceneY, lastValidGrids))
                                {
                                    result.EraseLine++;
                                }
                            }
                        }
                        result.Height = kSceneHeight - minY;
                        result.MatchShape = matchShape ? 1 : 2;

                        results.Add(result);
                    }
                }

                //遍历所有变形
                changeType = (changeType + 1) % changeNum[curTetrisType];
            }

            results.Sort((a, b) =>
            {
                int minH = Math.Min(a.Height, b.Height);
                if (minH > 10)
                {
                    if (a.EraseLine == b.EraseLine)
                    {
                        if (a.Height == b.Height)
                        {
                            return a.MatchShape - b.MatchShape;
                        }
                        return a.Height - b.Height;
                    }
                    return b.EraseLine - a.EraseLine;
                }
                if (a.MatchShape == b.MatchShape)
                {
                    if (a.EraseLine == b.EraseLine)
                    {
                        return a.Height - b.Height;
                    }
                    return b.EraseLine - a.EraseLine;
                }
                else
                {
                    return a.MatchShape - b.MatchShape;
                }

            });

            var finalResult = results[0];
            var offset = tetrisOffset[curTetrisType][finalResult.ChangeType];
            int moveX = finalResult.X - offset.X1 - currentRunGridX;
            foreach (var g in runGrids)//还原显示状态
            {
                g.show = true;
            }
            RunAISteps(moveX, finalResult.Change);
        }
        bool GridsContainsGrid(Grid[] grids, Grid grid)
        {
            foreach (var g in grids)
            {
                if (g.sceneX == grid.sceneX && g.sceneY == grid.sceneY)
                {
                    return true;
                }
            }
            return false;
        }
        bool CheckLineYFinished(int y, Grid[] grids)
        {
            foreach (var h in grids)
            {
                h.show = true;
            }
            bool finished = true;
            for (int lineX = 0; lineX < kSceneWidth; lineX++)
            {
                if (!allGrids[lineX, y].show)
                {
                    finished = false;
                    break;
                }
            }
            foreach (var h in grids)
            {
                h.show = false;
            }
            return finished;
        }

        //Pierre Dellacherie算法
        void CalcAI2()
        {
            //先把正在下落的格子显示状态抹除
            foreach (var g in runGrids)
            {
                g.show = false;
            }

            int R_X = 0; //最优坐标
            int R_Change = 0;//最优变换次数
            int R_ChangeType = 0;
            int R_Value = -9999;//最优评估值
            int changeType = curChangeType;
            for (int change = 0; change < changeNum[curTetrisType]; change++)
            {
                SceneOffset local_offset = localOffset[curTetrisType][changeType];
                for (int x = 0; x < kSceneWidth; x++)
                {
                    Grid[] lastValidGrids = null;
                    for (int y = 0; y < kSceneHeight; y++)
                    {
                        var showGrids = GetRunGridsAtPos(x, y, local_offset);
                        if (!CheckAIGridValid(showGrids))
                        {
                            break;
                        }
                        lastValidGrids = showGrids;
                    }
                    if (lastValidGrids != null)
                    {
                        int landingHeight = aiCalcLandingHeight(lastValidGrids);
                        int eraseLine = aiCalcEraseLine(lastValidGrids);
                        var finalGrids = aiCalcFinalGrids(lastValidGrids);
                        int boardRowTransitions = aiCalcRow(finalGrids);
                        int boardColTransitions = aiCalcColumn(finalGrids);
                        int boardBuriedHoles = aiCalcHoles(finalGrids);
                        int wells = aiCalcWell(finalGrids);
                        int value = -45 * landingHeight + 34 * eraseLine - 32 * boardRowTransitions - 93 * boardColTransitions - (79 * boardBuriedHoles) - 34 * wells;
                        if (value > R_Value)
                        {
                            R_Value = value;
                            R_X = x;
                            R_Change = change;
                            R_ChangeType = changeType;
                        }
                    }
                }

                //遍历所有变形
                changeType = (changeType + 1) % changeNum[curTetrisType];
            }


            var offset = tetrisOffset[curTetrisType][R_ChangeType];
            int moveX = R_X - offset.X1 - currentRunGridX;
            foreach (var g in runGrids)//还原显示状态
            {
                g.show = true;
            }

            RunDeviceSteps(moveX, R_Change);
            RunAISteps(moveX, R_Change);
        }

        //参数1.高度
        int aiCalcLandingHeight(Grid[] grids)
        {
            int minY = kSceneHeight;
            int maxY = 0;
            foreach (var g in grids)
            {
                //计算高度
                if (g.sceneY < minY)
                {
                    minY = g.sceneY;
                }
                if (g.sceneY > maxY)
                {
                    maxY = g.sceneY;
                }
            }
            int h = kSceneHeight - 1 - (minY + maxY) / 2;
            return h;
        }

        //参数2.消除行数*贡献方块
        int aiCalcEraseLine(Grid[] grids)
        {
            int line = 0;
            int cell = 0;
            Dictionary<int, bool> lineY = new Dictionary<int, bool>();

            foreach (var g in grids)
            {
                //计算消除行数
                if (!lineY.ContainsKey(g.sceneY))
                {
                    if (CheckLineYFinished(g.sceneY, grids))
                    {
                        lineY.Add(g.sceneY, true);
                        line++;
                        cell++;
                    }
                    else
                    {
                        lineY.Add(g.sceneY, false);
                    }
                }
                else if (lineY[g.sceneY])
                {
                    cell++;
                }
            }
            return line * cell;
        }

        //参数3.行变换
        int aiCalcRow(Grid[,] finalGrids)
        {
            int total = 0;
            for (int y = 0; y < kSceneHeight; y++)
            {
                int row = 0;
                bool show = true;
                for (int x = 0; x < kSceneWidth; x++)
                {
                    var g = finalGrids[x, y];
                    if (g.show != show)
                    {
                        row++;
                        show = g.show;
                    }
                }
                total += row;
            }
            return total;
        }

        //参数4.列变换
        int aiCalcColumn(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int row = 0;
                bool show = true;
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (g.show != show)
                    {
                        row++;
                        show = g.show;
                    }
                }
                total += row;
            }
            return total;
        }

        //参数5.空洞数量
        int aiCalcHoles(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int col = 0;
                int state = 0;//0初始 1遇到方块 2遇到空格
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (state == 0)
                    {
                        if (g.show) state = 1;
                    }
                    else if (state == 1)
                    {
                        if (!g.show) { state = 2; col++; }
                    }
                    else if (state == 2)
                    {
                        if (g.show) state = 1;
                    }
                }
                total += col;
            }
            return total;
        }

        //参数6.井
        int aiCalcWell(Grid[,] finalGrids)
        {
            int total = 0;
            for (int x = 0; x < kSceneWidth; x++)
            {
                int state = 1;//1遇到方块 2遇到空格
                int lastY = -1;

                List<int> wells = new List<int>();
                var f = new Action<int>((int y) =>
                {
                    int lr = 0;
                    if (x == 0) { lr++; }
                    else
                    {
                        var lg = finalGrids[x - 1, y];
                        if (lg.show) { lr++; }
                    }
                    if (x == kSceneWidth - 1) { lr++; }
                    else
                    {
                        var rg = finalGrids[x + 1, y];
                        if (rg.show) { lr++; }
                    }
                    if (lr == 2)
                    {
                        if (lastY == y - 1 && wells.Count > 0)
                        {
                            wells[wells.Count - 1]++;
                        }
                        else
                        {
                            wells.Add(1);
                        }
                        lastY = y;
                    }
                });
                for (int y = 0; y < kSceneHeight; y++)
                {
                    var g = finalGrids[x, y];
                    if (state == 1)
                    {
                        if (!g.show)
                        {
                            state = 2;
                            f(y);
                        }
                    }
                    else if (state == 2)
                    {
                        if (g.show) { state = 1; }
                        else
                        {
                            f(y);
                        }
                    }
                }
                foreach (int w in wells)
                {
                    int n = w;
                    while (n > 0)
                    {
                        total += n;
                        n--;
                    }
                }
            }
            return total;
        }

        Grid[,] aiCalcFinalGrids(Grid[] grids)
        {
            Grid[,] finalGrids = new Grid[kSceneWidth, kSceneHeight];
            foreach (var g in grids)
            {
                g.show = true;
            }
            for (int i = 0; i < kSceneHeight; i++)
            {
                for (int j = 0; j < kSceneWidth; j++)
                {
                    var g = allGrids[j, i];
                    Grid grid = new Grid();
                    grid.show = g.show;
                    grid.sceneX = j;
                    grid.sceneY = i;
                    finalGrids[j, i] = grid;
                }
            }
            foreach (var g in grids) { g.show = false; }
            //下落
            while (true)
            {
                int Y = -1;
                for (int y = kSceneHeight - 1; y >= 0; y--)
                {
                    bool finish = true;
                    for (int x = 0; x < kSceneWidth; x++)
                    {
                        if (!finalGrids[x, y].show)
                        {
                            finish = false;
                            break;
                        }
                    }
                    if (finish)
                    {
                        Y = y;
                        break;
                    }
                }
                if (Y == -1)
                {
                    break;
                }
                for (int y = Y; y > 0; y--)
                {
                    for (int x = 0; x < kSceneWidth; x++)
                    {
                        finalGrids[x, y].show = finalGrids[x, y - 1].show;
                    }
                }
            }

            return finalGrids;
        }

        void RunAISteps(int moveX, int change)
        {
            while (change > 0)
            {
                RunGridMove(Direction.UP);
                change--;
            }

            if (moveX > 0)
            {
                while (moveX > 0)
                {
                    RunGridMove(Direction.RIGHT);
                    moveX--;
                }
            }
            else if (moveX < 0)
            {
                while (moveX < 0)
                {
                    RunGridMove(Direction.LEFT);
                    moveX++;
                }
            }
            gameState = GameState.FastDrop;
        }

        void RunDeviceSteps(int moveX, int change)
        {
            var x = moveX;
            var type = nextTetrisType;
            if (type == 0 && (change == nextChangeType))
                x--;
            else if (type == 1 || type == 2 || type == 4 || type == 5 || type == 6)
                x++;

            serialPort.Write(new byte[] { (byte)((change << 4) | ((x > 0 ? 1 : 0) << 3) | ((x > 0 ? 1 : -1) * x)) }, 0, 1);
            serialPort.BaseStream.Flush();

            //Console.WriteLine($"MoveX={x}, Change={change}");
        }

        //正在下落
        void OnDropping()
        {
            if (!RunGridMove(Direction.DOWN))
            {
                //原来的rungrid状态修改
                foreach (Grid g in runGrids)
                {
                    g.running = false;
                }

                CalcGameScore();
                gameState = GameState.Destroy;
            }
        }

        enum Direction
        {
            LEFT, RIGHT, DOWN, UP
        }

        bool CheckAIGridValid(Grid[] grids)
        {
            foreach (var g in grids)
            {
                if (g == null || g.show) return false;
            }
            return true;
        }
        bool CheckNextGridValid(Grid[] nextGrids)
        {
            foreach (Grid g in nextGrids)
            {
                if (g == null) return false;
                if (!g.running && g.show) return false;
            }
            return true;
        }
        bool RunGridMove(Direction dir)
        {
            Grid[] nextGrids = null;
            switch (dir)
            {
                case Direction.DOWN:
                    nextGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY + 1, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridY++;
                    break;
                case Direction.LEFT:
                    nextGrids = GetRunGridsAtPos(currentRunGridX - 1, currentRunGridY, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridX--;
                    break;
                case Direction.RIGHT:
                    nextGrids = GetRunGridsAtPos(currentRunGridX + 1, currentRunGridY, currentOffset);
                    if (!CheckNextGridValid(nextGrids)) return false;
                    currentRunGridX++;
                    break;
                case Direction.UP:
                    {
                        int changType = (curChangeType + 1) % changeNum[curTetrisType];
                        SceneOffset offset = tetrisOffset[curTetrisType][changType];
                        nextGrids = GetRunGridsAtPos(currentRunGridX, currentRunGridY, offset);
                        if (!CheckNextGridValid(nextGrids))
                            return false;
                        currentOffset = offset;
                        curChangeType = changType;
                    }
                    break;
            }

            foreach (Grid g in runGrids)
            {
                g.show = false;
                g.running = false;
            }
            runGrids = nextGrids;
            foreach (Grid g in runGrids)
            {
                g.show = true;
                g.running = true;
            }
            return true;
        }

        void CalcGameScore()
        {
            int destroyLineNum = 0;
            for (int j = 0; j < kSceneHeight; j++)
            {
                int cnt = 0;
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (!allGrids[i, j].show)
                    {
                        break;
                    }
                    cnt++;
                }
                if (cnt == kSceneWidth)
                {
                    destroyLineNum++;
                }
            }
            //积分
            if (destroyLineNum > 0 && destroyLineNum < 4)
            {
                GameScore += destroyLineNum * scoreParam[destroyLineNum - 1];
            }

        }

        //记录要下落的方块
        List<Grid> fallGrids = new List<Grid>();
        //停止之后判断有没有能消除的行
        void OnDestroy()
        {
            int lastDestroyedLine = 0;
            for (int j = 0; j < kSceneHeight; j++)
            {
                int cnt = 0;
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (!allGrids[i, j].show)
                    {
                        break;
                    }
                    cnt++;
                }
                if (cnt == kSceneWidth)
                {
                    for (int i = 0; i < kSceneWidth; i++)
                    {
                        allGrids[i, j].show = false;
                    }
                    lastDestroyedLine = j;
                    break;
                }
            }

            fallGrids.Clear();
            //找出消除行之上的所有方块
            for (int j = 0; j < lastDestroyedLine; j++)
            {
                for (int i = 0; i < kSceneWidth; i++)
                {
                    if (allGrids[i, j].show)
                    {
                        fallGrids.Add(allGrids[i, j]);
                    }
                }
            }

            //回落
            if (fallGrids.Count > 0)
            {
                gameState = GameState.Fall;
            }
            else
            {
                gameState = GameState.NextRound;
            }
        }


        void Onfall()
        {
            //分两步，先隐藏原来的再显示下落后的
            foreach (Grid g in fallGrids)
            {
                g.show = false;
            }

            //y方向取得下一个方块 从后往前处理
            for (int i = fallGrids.Count - 1; i >= 0; i--)
            {
                Grid grid = GetGridByPos(fallGrids[i].sceneX, fallGrids[i].sceneY + 1);
                grid.show = true;
                fallGrids[i] = grid;
            }

            gameState = GameState.Destroy;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics dc = e.Graphics;
            //绘制边框
            DrawBorderLine(dc);
            //绘制预览
            DrawPreview(dc);
            //绘制方块
            DrawTetris(dc);
            //绘制积分
            DrawScore(dc);
        }

        void DrawBorderLine(Graphics g)
        {
            g.DrawRectangle(penBorder, new Rectangle(348, 438, 113, 223));
            g.DrawRectangle(penBorder, new Rectangle(kPreviewPoint, kPreviewSize));
            g.DrawRectangle(penBorder, new Rectangle(kScenePoint, kSceneSize));
            g.DrawRectangle(penBorder, new Rectangle(kPreviewPoint, kPreviewSize));

            g.FillRectangle(GameBkgrd, new Rectangle(349, 439, 111, 221));
            g.FillRectangle(GameBkgrd, new Rectangle(kScenePoint.X + 1, kScenePoint.Y + 1, kSceneSize.Width - 2, kSceneSize.Height - 2));
            g.FillRectangle(GameBkgrd, new Rectangle(kPreviewPoint.X + 1, kPreviewPoint.Y + 1, kPreviewSize.Width - 2, kPreviewSize.Height - 2));
        }

        Brush showBrush = new SolidBrush(Color.Black);
        Brush GameBkgrd = new SolidBrush(Color.FromArgb(147, 174, 97));
        Pen penBorder = new Pen(Color.Black, 2);

        void DrawTetris(Graphics g)
        {
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (bufferGrid[i, j])
                        g.FillRectangle(showBrush, 350 + i * 10 + i, 440 + j * 10 + j, 10, 10);
                }
            }

            List<Rectangle> allShown = new List<Rectangle>();
            foreach (Grid grid in allGrids)
            {
                if (grid.show)
                {
                    allShown.Add(grid.rect);
                }
            }
            if (allShown.Count == 0)
            {
                return;
            }
            g.FillRectangles(showBrush, allShown.ToArray());
        }

        void DrawPreview(Graphics g)
        {
            List<Rectangle> allShown = new List<Rectangle>();
            foreach (Grid grid in preGrids)
            {
                if (grid.show)
                {
                    allShown.Add(grid.rect);
                }
            }
            if (allShown.Count == 0)
                return;

            g.FillRectangles(showBrush, allShown.ToArray());
        }


        void DrawScore(Graphics g)
        {
            g.DrawString(string.Format("得分：{0}", GameScore), new Font("Arial", 10), new SolidBrush(Color.Black), kScorePoint.X, kScorePoint.Y);
        }
    }

    public class TetrisMapper
    {
        public static byte[][] Mapper = new byte[][] {
            //匹配类型
            // O
            new byte[] {0x66, 0x0, 30  },
            new byte[] {0x6, 0x60, 30  },
            new byte[] {0x0, 0x66, 30  },
                    
            // I 0	     
            new byte[] {0x22, 0x20, 0  },
            new byte[] {0x22, 0x22, 0  },
                    
            // I 90	     
            new byte[] {0xf0, 0x0, 1   },
            new byte[] {0xf, 0x0, 1    },
            new byte[] {0x0, 0xf0, 1   },
            new byte[] {0x0, 0xf, 1    },
                    
            // S 0	     
            new byte[] {0x6c, 0x0, 41  },
            new byte[] {0x6, 0xc0, 41  },
            new byte[] {0x0, 0x6c, 41  },
                    
            // S 90
            new byte[] {0x8c, 0x40, 40 },
            new byte[] {0x8, 0xc4, 40  },
                    
            // Z 0
            new byte[] {0xc6, 0x0, 60  },
            new byte[] {0xc, 0x60, 60  },
            new byte[] {0x0, 0xc6, 60  },
                    
            // Z 90
            new byte[] {0x4c, 0x80, 61 },
            new byte[] {0x4, 0xc8, 61  },
                    
            // J 0
            new byte[] {0x44, 0xc0, 10 },
            new byte[] {0x4, 0x4c, 10  },
                    
            // J 90
            new byte[] {0x8e, 0x0, 11  },
            new byte[] {0x8, 0xe0, 11  },
            new byte[] {0x0, 0x8e, 11  },
                    
            // J 180
            new byte[] {0xc8, 0x80, 12 },
            new byte[] {0xc, 0x88, 12  },
                    
            // J 270
            new byte[] {0xe2, 0x0, 13  },
            new byte[] {0xe, 0x20, 13  },
            new byte[] {0x0, 0xe2, 13  },
                    
            // L 0
            new byte[] {0x88, 0xc0, 20 },
            new byte[] {0x8, 0x8c, 20  },
                    
            // L 90
            new byte[] {0xe8, 0x0, 21  },
            new byte[] {0xe, 0x80, 21  },
            new byte[] {0x0, 0xe8, 21  },
                    
            // L 180
            new byte[] {0xc4, 0x40, 22 },
            new byte[] {0xc, 0x44, 22  },
                    
            // L 270   
            new byte[] {0x2e, 0x0, 23  },
            new byte[] {0x2, 0xe0, 23  },
            new byte[] {0x0, 0x2e, 23  },
                    
            // T 0	     
            new byte[] {0xe4, 0x0, 50  },
            new byte[] {0xe, 0x40, 50  },
            new byte[] {0x0, 0xe4, 50  },
                    
            // T 90	   
            new byte[] {0x4c, 0x40, 51 },
            new byte[] {0x4, 0xc4, 51  },
                    
            // T 180     
            new byte[] {0x4e, 0x0, 52  },
            new byte[] {0x4, 0xe0, 52  },
            new byte[] {0x0, 0x4e, 52  },
                    
            // T 270     
            new byte[] {0x8c, 0x80, 53 },
            new byte[] {0x8, 0xc8, 53  }
        };
    }
}
