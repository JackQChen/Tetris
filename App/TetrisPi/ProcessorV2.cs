using System.Runtime.InteropServices;

namespace TetrisApp
{
    public class ProcessorV2
    {
        class Grid
        {
            public bool show;
        }

        Connector connector;
        AudioPlayer player;
        bool isWindows = false;
        DateTime lastUpdatedTime = DateTime.Now;

        //画布网格数 10x20
        const int kSceneWidth = 10;
        const int kSceneHeight = 20;
        //变体数量
        int[] changeNum = new int[7];
        //全部网格
        Grid[,] allGrids = new Grid[kSceneWidth, kSceneHeight];

        int GameScore = 0;

        public ProcessorV2()
        {
            //O
            changeNum[3] = 1;
            //I
            changeNum[0] = 2;
            //S
            changeNum[4] = 2;
            //Z
            changeNum[6] = 2;
            //L
            changeNum[2] = 4;
            //J
            changeNum[1] = 4;
            //T
            changeNum[5] = 4;

            //初始化网格
            for (int i = 0; i < kSceneHeight; i++)
            {
                for (int j = 0; j < kSceneWidth; j++)
                {
                    Grid grid = new Grid();
                    grid.show = false;
                    allGrids[j, i] = grid;
                }
            }
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

        public void Init()
        {
            isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var datetime = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss");

            var logFilePath = Path.Combine(dir, $"LOG_{datetime}.txt");
            Logger.Open(logFilePath);

            logFilePath = Path.Combine(dir, $"LOG_AI_{datetime}.txt");
            LoggerAI.Open(logFilePath);

            // 初始化设备
            connector = new Connector();
            connector.Init(isWindows ? "COM2" : "/dev/ttyUSB0", 115200); // 修改为实际的串口号
            connector.OnTetrisData += Connector_OnTetrisData;

            player = new AudioPlayer();
            //player.Init(3);

            InitTask();
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

            // 转换网格
            for (int i = 0; i < kSceneWidth; i++)
                for (int j = 0; j < kSceneHeight; j++)
                    map[i + 1, j + 3] = allGrids[i, j].show ? 1 : 0;

            var type = 0;
            switch (tetrisType)
            {
                case 0: type = 2; break;
                case 1: type = 6; break;
                case 2: type = 7; break;
                case 3: type = 1; break;
                case 4: type = 5; break;
                case 5: type = 3; break;
                case 6: type = 4; break;
            }
            curType = type;
            curChange = changeType;

            CalcAI(tetrisType, changeType);
        }

        void CalcAI(int tetrisType, int changeType)
        {
            var result = AIControl();
            var change = changeType;
            int type = result.Item1, move = result.Item2, rotate = result.Item3, rows = result.Item4;
            if (rows > 0)
            {
                switch (rows)
                {
                    case 1: GameScore += 100; break;
                    case 2: GameScore += 300; break;
                    case 3: GameScore += 700; break;
                    case 4: GameScore += 1500; break;
                }
            }
            switch (type)
            {
                //O
                case 1:
                    {
                        RunAISteps(tetrisType, changeType, move - 5, 0);
                    }
                    break;
                //I
                case 2:
                    {
                        if (rotate == 1)
                            RunAISteps(tetrisType, changeType, move - 5, change);
                        else
                            RunAISteps(tetrisType, changeType, move - 6, change + 1);
                    }
                    break;
                //T
                case 3:
                    {
                        if (rotate == 3)
                            RunAISteps(tetrisType, changeType, move - 5, 4 - change + 6 - rotate);
                        else
                            RunAISteps(tetrisType, changeType, move - 6, 4 - change + 6 - rotate);
                    }
                    break;
                //Z
                case 4:
                    {
                        RunAISteps(tetrisType, changeType, move - 6, 2 - change + (rotate == 1 ? 1 : 0));
                    }
                    break;
                //S
                case 5:
                    {
                        RunAISteps(tetrisType, changeType, move - 6, 2 - change + (rotate == 1 ? 1 : 0));
                    }
                    break;
                //J
                case 6:
                    {
                        if (rotate == 1 || rotate == 3)
                            RunAISteps(tetrisType, changeType, move - 6, 4 - change + 4 - rotate);
                        else if (rotate == 2)
                            RunAISteps(tetrisType, changeType, move - 5, 4 - change + 4 - rotate);
                        else
                            RunAISteps(tetrisType, changeType, move - 6, 4 - change);
                    }
                    break;
                //L
                case 7:
                    {
                        if (rotate < 4)
                            RunAISteps(tetrisType, changeType, move - 6, 4 - change + 4 - rotate);
                        else
                            RunAISteps(tetrisType, changeType, move - 5, 4 - change);
                    }
                    break;
            }
        }

        void RunAISteps(int tetrisType, int changeType, int moveX, int change)
        {
            RunDeviceSteps(tetrisType, changeType, moveX, change % changeNum[tetrisType]);
        }

        void RunDeviceSteps(int tetrisType, int changeType, int moveX, int change)
        {
            var x = moveX;
            var type = tetrisType;
            if (type == 0 && (change == changeType))
                x--;
            else if (type == 1 || type == 2 || type == 4 || type == 5 || type == 6)
                x++;

            connector.Send((byte)((change << 4) | ((x > 0 ? 1 : 0) << 3) | ((x > 0 ? 1 : -1) * x)));
            Logger.Log($"X = {x}, C = {change}");
        }

        public int curType = 0;
        public int curChange = 0;
        int[,] map_try;
        const int map_width = 11;
        const int map_height = 23;
        public int[,] map = new int[map_width, map_height];

        public Tuple<int, int, int, int> AIControl()
        {
            double bestins = -0x7ffffff;
            int bestst = 0;
            int rotime = 0;
            int lines = 0;

            int Ranbk = curType;
            brick one_bk = new brick();
            one_bk.type = curType;
            one_bk.cores = GetBrick(Ranbk);

            brick two_bk = new brick();
            two_bk.type = Ranbk;
            two_bk.cores = GetBrick(Ranbk);
            map_try = new int[map_width, map_height];
            for (int i = 1; i <= 4; i++)
            {

                RotateBrick(two_bk);
                for (int ix = 1; ix <= map_width - 1; ix++)
                {
                    int overrange = 0;

                    foreach (node pretest in two_bk.cores)
                    {
                        if (pretest.x + ix <= 0 || pretest.x + ix >= map_width)
                        {
                            overrange++;
                            break;
                        }
                    }
                    if (overrange > 0)
                    {
                        continue;
                    }

                    two_bk.mov_x = ix;
                    two_bk.mov_y = 0;
                    Array.Clear(map_try, 0, map_height * map_width);

                    while (canRun(two_bk, map_try) == true)
                    {
                        two_bk.mov_y++;
                    }
                    double tbest = SeekBest(map_try, two_bk);
                    //init(map_try);
                    if (tbest > bestins)
                    {
                        bestins = tbest;
                        bestst = ix;
                        rotime = i;
                        lines = completeline;
                    }
                }
            }
            //for (int i = 0; i < rotime; i++)
            //{
            //    RotateBrick(one_bk);
            //}
            return Tuple.Create(curType, bestst, rotime, lines);
        }

        private bool canRun(brick mbk, int[,] m_try)
        {
            int flag = 0;
            foreach (node item in mbk.cores)
            {
                int fx = item.x + mbk.mov_x;
                int fy = item.y + mbk.mov_y;

                if (fy <= 0)
                {
                    return true;
                }
                if (fy + 1 >= map_height || map[fx, fy + 1] == 1)
                {
                    flag++;

                }
            }
            if (flag > 0)
            {
                //m_try = new int[map_width, map_height];
                for (int i = 0; i < map_width; i++)
                {
                    for (int j = 0; j < map_height; j++)
                    {
                        m_try[i, j] = map[i, j];
                    }
                }
                foreach (node item2 in mbk.cores)
                {
                    m_try[item2.x + mbk.mov_x, item2.y + mbk.mov_y] = 1;
                }
                return false;

            }
            return true;
        }

        int[] ind_comline = new int[23];
        int holes = 0;
        int completeline = 0;
        int bumpiness = 0;
        int aheight = 0;

        private double SeekBest(int[,] map_try, brick tbk)
        {
            //init(map_try);

            int LandingHeight = map_height - (tbk.cores[0].y + tbk.mov_y);
            int Roweliminated = 0;
            int Rowtransitions = 0;
            int Columntransitions = 0;
            int Wellsum = 0;

            int[] columnHeight = new int[map_width];
            holes = 0;
            completeline = 0;
            bumpiness = 0;
            aheight = 0;

            double a = -0.610066;
            double b = 0.760666;
            double c = -0.55663;
            double d = -0.184483;

            for (int i = 1; i < map_height; i++)
            {
                int flag0 = 1;
                for (int j = 1; j < map_width; j++)
                {
                    if (flag0 != map_try[j, i])
                    {
                        flag0 = map_try[j, i];
                        Rowtransitions++;
                    }
                }
                if (flag0 != 1)
                {
                    Rowtransitions++;
                }
            }
            for (int i = 1; i <= map_width - 1; i++)
            {
                int flag0 = 1;
                for (int j = 1; j <= map_height - 1; j++)
                {

                    if (map_try[i, j] == 1)
                    {
                        columnHeight[i] = map_height - j;
                        int flag1 = 0;
                        for (int k = j; k <= map_height - 1; k++)
                        {
                            if (flag0 != map_try[i, k])
                            {
                                flag0 = map_try[i, k];
                                Columntransitions++;
                            }
                            if (map_try[i, k] == 0)
                            {
                                if (flag1 == 0)
                                {
                                    holes++;
                                }
                                flag1 = 1;
                            }
                            else
                            {
                                flag1 = 0;
                            }
                        }
                        if (flag0 != 1)
                        {
                            Columntransitions++;
                        }
                        break;
                    }
                }
            }

            int contribute = 0;
            for (int j = 1; j <= map_height - 1; j++)
            {
                ind_comline[completeline] = j;
                completeline++;
                for (int i = 1; i <= map_width - 1; i++)
                {
                    if (map_try[i, j] == 0)
                    {
                        completeline--;
                        break;
                    }
                    foreach (node item in tbk.cores)
                    {
                        if (item.y + tbk.mov_y == j)
                        {
                            contribute++;
                        }
                    }
                }
            }
            Roweliminated = contribute * completeline;

            List<int> wells = new List<int>();
            for (int i = 1; i < map_width; i++)
            {
                int count = 0;
                for (int j = 1; j < map_height; j++)
                {
                    if (map_try[i, j] == 1)
                    {
                        if (count != 0)
                        {
                            wells.Add(count);
                            count = 0;
                        }
                    }
                    int left = 1;
                    int right = 1;
                    if (i - 1 >= 1)
                    {
                        left = map_try[i - 1, j];
                    }
                    if (i + 1 < map_width)
                    {
                        right = map_try[i + 1, j];
                    }
                    if (map_try[i, j] == 0 && left == 1 && right == 1)
                    {
                        count++;
                    }
                }
            }
            foreach (int itemnum in wells)
            {
                Wellsum += (1 + itemnum) * itemnum / 2;
            }

            //for (int i = 1; i < map_width - 1; i++) 
            //{
            //    bumpiness += Math.Abs(columnHeight[i] - columnHeight[i + 1]);
            //    aheight += columnHeight[i];
            //}
            //aheight += columnHeight[map_width - 1];

            double index1 = -4.500158825082766;
            double index2 = 3.4181268101392694;
            double index3 = -3.2178882868487753;
            double index4 = -9.348695305445199;
            double index5 = -7.899265427351652;
            double index6 = -3.3855972247263626;

            return index1 * LandingHeight + index2 * Roweliminated + index3 * Rowtransitions + index4 * Columntransitions + index5 * holes + index6 * Wellsum;
            //return a * aheight + b * completeline + c * holes + d * bumpiness;
        }

        private class node
        {
            public int x;
            public int y;
        }

        private class brick
        {
            public List<node> cores;
            public int type;

            public int mov_x;
            public int mov_y;
            //shift:mid of width

        }

        private List<node> GetBrick(int Bricktype)
        {
            List<node> ret = new List<node>();
            if (Bricktype == 1)
            {
                //O
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = 1, y = 0 };
                ret.Add(node2);
                node node3 = new node() { x = 1, y = -1 };
                ret.Add(node3);
                node node4 = new node() { x = 0, y = -1 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 2)
            {
                //I
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = -1, y = 0 };
                ret.Add(node2);
                node node3 = new node() { x = 1, y = 0 };
                ret.Add(node3);
                node node4 = new node() { x = 2, y = 0 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 3)
            {
                //T
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = -1, y = 0 };
                ret.Add(node2);
                node node3 = new node() { x = 0, y = -1 };
                ret.Add(node3);
                node node4 = new node() { x = 1, y = 0 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 4)
            {
                //Z
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = -1, y = -1 };
                ret.Add(node2);
                node node3 = new node() { x = 0, y = -1 };
                ret.Add(node3);
                node node4 = new node() { x = 1, y = 0 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 5)
            {
                //S
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = 1, y = -1 };
                ret.Add(node2);
                node node3 = new node() { x = 0, y = -1 };
                ret.Add(node3);
                node node4 = new node() { x = -1, y = 0 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 6)
            {
                //J
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = 0, y = -1 };
                ret.Add(node2);
                node node3 = new node() { x = 0, y = 1 };
                ret.Add(node3);
                node node4 = new node() { x = -1, y = 1 };
                ret.Add(node4);
                return ret;
            }
            if (Bricktype == 7)
            {
                //L
                node node1 = new node() { x = 0, y = 0 };
                ret.Add(node1);
                node node2 = new node() { x = 0, y = -1 };
                ret.Add(node2);
                node node3 = new node() { x = 0, y = 1 };
                ret.Add(node3);
                node node4 = new node() { x = 1, y = 1 };
                ret.Add(node4);
                return ret;
            }
            else
            {
                return null;
            }
        }
        private void RotateBrick(brick bk)
        {
            if (bk.type == 1)
            {
                return;
            }
            foreach (node item in bk.cores)
            {
                int tx = item.x;
                int ty = item.y;
                item.x = ty;
                item.y = -tx;
            }
            //rout: x'= x*cosn+y*sinn
            //rout: y'= -x*sinn+y*cosn
            //sin90=1 cos90=0
        }
    }
}
