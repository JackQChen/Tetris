using System.Runtime.InteropServices;
using SixLabors.ImageSharp;

namespace TetrisApp
{
    public class Processor
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

        //网格大小
        const int kGridSize = 32;
        //画布起点
        Point kScenePoint = new Point(10, 20);
        //画布网格数 10x20
        const int kSceneWidth = 10;
        const int kSceneHeight = 20;
        //全部网格
        Grid[,] allGrids = new Grid[kSceneWidth, kSceneHeight];
        //7种组合，在第一块固定的时候，其他块的偏移
        List<SceneOffset>[] tetrisOffset = new List<SceneOffset>[7];
        //局部坐标系，以左上角为原点
        List<SceneOffset>[] localOffset = new List<SceneOffset>[7];
        //变体数量
        int[] changeNum = new int[7];

        Connector connector;
        AudioPlayer player;
        bool isWindows = false;
        DateTime lastUpdatedTime = DateTime.Now;

        int completeline = 0;
        int GameScore = 0;

        public Processor()
        {
        }

        void InitTask()
        {
            // 自动重置
            Task.Factory.StartNew(() =>
            {
                //var ftpHandler = new FTPHandler();
                while (true)
                {
                    Thread.Sleep(10000);
                    if ((DateTime.Now - lastUpdatedTime).TotalSeconds > 10)
                    {
                        //Environment.Exit(0);
                        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records");
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        var datetime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

                        var logFilePath = Path.Combine(dir, $"LOG_{datetime}.txt");
                        Logger.Open(logFilePath);

                        logFilePath = Path.Combine(dir, $"LOG_AI_{datetime}.txt");
                        LoggerAI.Open(logFilePath);

                        var path = Path.Combine(dir, "GameScore.txt");
                        var strScore = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}{Environment.NewLine}GameScore = {GameScore}{Environment.NewLine}";
                        File.AppendAllText(path, strScore);

                        //// 同步文件
                        //Task.Run(() =>
                        //{
                        //    ftpHandler.SyncFiles();
                        //});
                        // 重置
                        connector.Send(0xff);
                        GameScore = 0;
                    }
                }
            }, TaskCreationOptions.LongRunning);
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

        public void Init()
        {
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            // 初始化设备
            connector = new Connector();
            connector.Init(isWindows ? "COM2" : "/dev/ttyUSB0", 115200); // 修改为实际的串口号
            connector.OnTetrisData += Connector_OnTetrisData;

            player = new AudioPlayer();
            //player.Init(3);

            InitTask();
            InitGrids();
            InitTetrisType();
        }

        private void Connector_OnTetrisData(object? sender, int tetrisData)
        {
            var maxRow = connector.GetMaxRow() - 1;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (j < maxRow)
                        allGrids[i, j].show = false;
                    else
                        allGrids[i, j].show = connector.GetCellStatus(i, j);
                }
            }

            lastUpdatedTime = DateTime.Now;
            uint[] array = new uint[10];
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 20; j++)
                    array[i] |= (allGrids[9 - i, 19 - j].show ? 1U : 0) << j;

            LoggerAI.Log(string.Join(',', array));
            LoggerAI.Log($"Tetris = {tetrisData}");

            var changeType = tetrisData % 10;
            var tetrisType = tetrisData / 10;
            changeType = changeType % changeNum[tetrisType];

            completeline = 0;

            CalcAI(tetrisType, changeType);

            if (completeline > 0)
            {
                switch (completeline)
                {
                    case 1: GameScore += 100; break;
                    case 2: GameScore += 300; break;
                    case 3: GameScore += 700; break;
                    case 4: GameScore += 1500; break;
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

        bool CheckAIGridValid(Grid[] grids)
        {
            foreach (var g in grids)
            {
                if (g == null || g.show) return false;
            }
            return true;
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
        void CalcAI(int tetrisType, int changeType)
        {
            int R_X = 0; //最优坐标
            int R_Change = 0;//最优变换次数
            int R_ChangeType = 0;
            int R_Value = -9999;//最优评估值
            for (int change = 0; change < changeNum[tetrisType]; change++)
            {
                SceneOffset local_offset = localOffset[tetrisType][changeType];
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
                            completeline = eraseLine;
                            R_Value = value;
                            R_X = x;
                            R_Change = change;
                            R_ChangeType = changeType;
                        }
                    }
                }

                changeType = (changeType + 1) % changeNum[tetrisType];
            }

            var offset = tetrisOffset[tetrisType][R_ChangeType];
            int moveX = R_X - offset.X1 - (kSceneWidth / 2 - 2);

            RunDeviceSteps(moveX, R_Change, tetrisType, changeType);
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

        void RunDeviceSteps(int moveX, int change, int tetrisType, int changeType)
        {
            var x = moveX;
            var type = tetrisType;
            if (type == 0 && (change == changeType))
                x--;
            else if (type == 1 || type == 2 || type == 4 || type == 5 || type == 6)
                x++;

            connector.Send((byte)(change << 4 | (x > 0 ? 1 : 0) << 3 | (x > 0 ? 1 : -1) * x));
            Logger.Log($"X = {x}, C = {change}");
        }

    }
}
