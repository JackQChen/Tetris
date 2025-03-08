using System.Runtime.InteropServices;
using System.Xml.Linq;
using SixLabors.ImageSharp;

namespace TetrisApp
{
    public class Processor
    {

        Connector connector;
        AudioPlayer player;
        bool isWindows = false;
        DateTime lastUpdatedTime = DateTime.Now;


        /// <summary>
        /// 列数，最大x坐标+1
        /// </summary>
        public static readonly int columns = 10;
        /// <summary>
        /// 行数，最大y坐标+1
        /// </summary>
        public static readonly int rows = 20;
        /// <summary>
        /// 背景图矩阵，存放0无方块，1有方块
        /// </summary>
        public static double[,] arr;
        /// <summary>
        /// 板块对象
        /// </summary>
        public Brick curbrick;
        /// <summary>
        /// 预定义的int型二维矩阵
        /// </summary>
        private double[,] arr2;
        /// <summary>
        /// 预定义的int型二维矩阵
        /// </summary>
        private double[,] arr3;


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
                        Environment.Exit(0);
                        //// 保存截图
                        //var imageData = Paint();
                        //var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records");
                        //if (!Directory.Exists(dir))
                        //    Directory.CreateDirectory(dir);
                        //var path = Path.Combine(dir, $"{DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")}.png");
                        //File.WriteAllBytes(path, imageData);
                        //// 同步文件
                        //Task.Run(() =>
                        //{
                        //    ftpHandler.SyncFiles();
                        //});
                        //// 重置
                        //connector.Send(0xff);
                        //Restart();
                    }
                }
            }, TaskCreationOptions.LongRunning);
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
        }

        private void Connector_OnTetrisData(object? sender, int tetrisData)
        {
            lastUpdatedTime = DateTime.Now;
            int[] array = new int[10];
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 20; j++)
                    array[i] |= (allGrids[9 - i, 19 - j].show ? 1 : 0) << j;

            LoggerAI.Log(string.Join(',', array));
            LoggerAI.Log($"Tetris = {tetrisData}");

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

            var changeType = tetrisData % 10;
            var tetrisType = tetrisData / 10;
            changeType = changeType % changeNum[tetrisType];

            AIControl(tetrisType, changeType);
        }

        /// <summary>
        /// 深复制背景图矩阵arr2和arr3
        /// </summary>
        private void Copyarr2()
        {
            arr2 = new double[columns, rows];
            arr3 = new double[columns, rows];
            for (int i = 0; i < columns; i++)
                for (int j = 0; j < rows; j++)
                {
                    arr2[i, j] = arr[i, j];
                    arr3[i, j] = arr[i, j];
                }
        }

        /// <summary>
        /// 用于把板块写入背景图arr中，只有板块不能下落时才可以调用
        /// </summary>
        /// <param name="arr">背景图矩阵，int型二维数组</param>
        /// <param name="posnodes">以稀疏矩阵的方式存储每个方块对应背景矩阵arr的位置，List（Node）格式</param>
        public void Fillarr(double[,] arr, List<Node> posnodes)
        {
            foreach (Node item in posnodes)
            {
                arr[item.x, item.y] = 1;
            }
        }

        /// <summary>
        /// 对一个二维数组（矩阵）计算可消行数，与Cleanrows配套使用
        /// </summary>
        /// <param name="arr">一个二维数组</param>
        /// <returns>List（int）类型，第一个参数是可消总行数，接下来的项则是可消行的y坐标，从大到小排列</returns>
        public List<int> CountRow(double[,] arr)
        {
            List<int> countrows = new List<int>
            {
                0
            };
            bool isfull;
            for (int i = rows - 1; i >= 0; i--)
            {
                isfull = true;
                for (int j = 0; j < columns; j++)
                {
                    if (arr[j, i] == 0)
                    {
                        isfull = false;
                        break;
                    }
                }
                if (isfull)
                {
                    countrows[0]++;
                    countrows.Add(i);
                }
            }
            return countrows;
        }

        /// <summary>
        /// 将int型二维数组增加一圈
        /// </summary>
        /// <param name="arr2">一个int型二维数组</param>
        /// <returns>一个扩大的int型二维数组</returns>
        private double[,] Expandarr(double[,] arr2)
        {
            double[,] t_arr = new double[columns + 2, rows + 2];
            for (int i = 1; i <= columns; i++)
                for (int j = 1; j <= rows; j++)
                    t_arr[i, j] = arr2[i - 1, j - 1];
            for (int i = 0; i < columns + 2; i++)
            {
                t_arr[i, 0] = 1; t_arr[i, rows + 1] = 1;
            }
            for (int j = 0; j < rows + 2; j++)
            {
                t_arr[0, j] = 1; t_arr[columns + 1, j] = 1;
            }
            return t_arr;
        }

        /// <summary>
        /// 深复制背景图矩阵arr2
        /// </summary>
        private void Copyarr()
        {
            arr2 = new double[columns, rows];
            for (int i = 0; i < columns; i++)
                for (int j = 0; j < rows; j++)
                {
                    arr2[i, j] = arr[i, j];
                }
        }

        private void AIControl()
        {
            Copyarr2();
            Brick testbrick = new Brick(curbrick.type);
            double index1 = -4.500158825082766;
            double index2 = 3.4181268101392694;
            double index3 = -3.2178882868487753;
            double index4 = -9.348695305445199;
            double index5 = -7.899265427351652;
            double index6 = -3.3855972247263626;
            int[] BuiHeight = new int[columns];//第一个砖块，下面往上数
            int Rowtransitions = 0;//行变换
            int Holes = 0;//空洞数
            int Columntransitions = 0;//列变换
            int Wellsum = 0;//井
            double LandingHeight = 0;//落地高度
            int clearrows = 0;//消行数
            int contribution = 0;//贡献数
            List<int> countrows;
            double flag = Double.MinValue;
            double result = 0;
            int ai_rotate = 0;
            int ai_posx = testbrick.pos.x;
            int ai_posy = 0;
            for (int k = 0; k < 4; k++)
            {
                testbrick.pos.x = columns / 2 - 1;
                testbrick.pos.y = rows - 1;
                testbrick.Rotate();
                for (int i0 = 0; i0 < columns; i0++)
                {
                    ////result = SeekBest(arr4, testbrick);					
                    int Bui = 0;
                    for (int j = 0; j < rows; j++)
                        if (arr2[i0, j] == 0)
                        {
                            Bui = j; break;
                        }
                    testbrick.pos.x = i0;
                    testbrick.pos.y = Bui;
                    //按列寻找碰撞点
                    while (!testbrick.Canmove(testbrick.pos) && testbrick.pos.y <= rows)
                    {
                        testbrick.pos.y++;
                    }
                    if (testbrick.pos.y > rows) continue;
                    LandingHeight = testbrick.pos.y + (testbrick.typenodes[0].y + testbrick.typenodes[1].y + testbrick.typenodes[2].y + testbrick.typenodes[3].y) / 4;
                    testbrick.Canmove(testbrick.pos);
                    Fillarr(arr2, testbrick.posnodes);
                    countrows = CountRow(arr2);
                    //消行数
                    clearrows = countrows[0];
                    countrows.RemoveAt(0);
                    //贡献数
                    foreach (int item in countrows)
                        for (int j = 0; j < columns; j++)
                        {
                            if (arr3[j, item] == 0) contribution++;
                        }
                    //新背景图矩阵
                    foreach (int item in countrows)
                        for (int i = 0; i < columns; i++)
                            for (int j = item; j < rows - 1; j++)
                            {
                                arr2[i, j] = arr2[i, j + 1];
                            }
                    //这里是xy坐标系楼高
                    for (int i = 0; i < columns; i++)
                        for (int j = 0; j < rows; j++)
                            if (arr2[i, j] == 0)
                            {
                                BuiHeight[i] = j - 1; break;
                            }
                    for (int i = 0; i < columns; i++)
                        for (int j = 0; j < BuiHeight[i] - clearrows; j++)
                        {
                            //洞数
                            if (arr2[i, j] == 0)
                            {
                                Holes++;
                            }
                        }
                    arr2 = Expandarr(arr2);
                    //行变换
                    for (int i = 0; i < rows + 2; i++)
                        for (int j = 0; j < columns + 2; j++)
                        {
                            if (j > 0 && arr2[j, i] != arr2[j - 1, i])
                            {
                                Rowtransitions++;
                            }
                        }
                    //列变换
                    for (int i = 0; i < columns + 2; i++)
                        for (int j = 0; j < rows + 2; j++)
                        {
                            if (j > 0 && arr2[i, j] != arr2[i, j - 1])
                            {
                                Columntransitions++;
                            }
                        }
                    //井数
                    int temp = 0;
                    for (int i = 1; i <= columns; i++)
                        for (int j = 1; j <= rows; j++)
                        {
                            if (arr2[i, j] == 0 && arr2[i - 1, j] == 1 && arr2[i + 1, j] == 1)
                            {
                                temp++;
                            }
                            else
                            {
                                Wellsum += temp * (temp + 1) / 2;
                                temp = 0;
                            }
                        }
                    //double index1 = -4.500158825082766;
                    //double index2 = 3.4181268101392694;
                    //double index3 = -3.2178882868487753;
                    //double index4 = -9.348695305445199;
                    //double index5 = -7.899265427351652;
                    //double index6 = -3.3855972247263626;
                    result = index1 * LandingHeight + index2 * clearrows * contribution + index3 * Rowtransitions + index4 * Columntransitions + index5 * Holes + index6 * Wellsum;
                    //Console.WriteLine(result);
                    if (result > flag)
                    {
                        flag = result;
                        ai_rotate = k + 1;
                        ai_posx = i0;
                        ai_posy = testbrick.pos.y;
                        //Console.WriteLine(now[0]);
                        //Console.WriteLine(now[1]);
                    }
                    LandingHeight = 0;
                    clearrows = 0;
                    contribution = 0;
                    Rowtransitions = 0;
                    Columntransitions = 0;
                    Holes = 0;
                    Wellsum = 0;
                    Copyarr();
                }
            }
            curbrick.Rotate(ai_rotate);
            curbrick.pos.x = ai_posx;
            curbrick.pos.y = ai_posy;
            textBox1.Text = "旋转" + ai_rotate + "横坐标" + ai_posx + "纵坐标" + ai_posy;
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

    public class Brick
    {
        /// <summary>
        /// 说明板块类型，0田字形,1|字形,2T字形,3Z字形,4S字形,5J字形,6L字形
        /// </summary>
        public int type = 0;
        /// <summary>
        /// 以稀疏矩阵的方式存储每个方块相对板块的位置
        /// </summary>
        public List<Node> typenodes;
        /// <summary>
        /// 以稀疏矩阵的方式存储每个方块对应背景矩阵arr的位置
        /// </summary>
        public List<Node> posnodes;
        /// <summary>
        /// 板块中心位置
        /// </summary>
        public Node pos = new Node
        {
            x = Processor.columns / 2 - 1,
            y = Processor.rows + 1
        };
        /// <summary>
        /// 新建板块
        /// </summary>
        public Brick()
        {
            Random random = new Random();
            int index = random.Next(0, 49) / 7;
            //int index = 1;
            typenodes = new List<Node>();
            Node node1, node2, node3, node4;
            switch (index)
            {
                case 0:
                    //田字形
                    type = 0;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 1, y = -1 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node4);
                    break;
                case 1:
                    //|字形
                    type = 1;
                    node1 = new Node() { x = 2, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node4);
                    break;
                case 2:
                    //T字形
                    type = 2;
                    node1 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node4);
                    break;
                case 3:
                    //z字形
                    type = 3;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 1, y = 1 };
                    typenodes.Add(node4);
                    break;
                case 4:
                    //s字形
                    type = 4;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 1 };
                    typenodes.Add(node4);
                    break;
                case 5:
                    //J字形
                    type = 5;
                    node1 = new Node() { x = 0, y = 2 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = 1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node4);
                    break;
                case 6:
                    //L字形
                    type = 6;
                    node1 = new Node() { x = 0, y = 2 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = 1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node4);
                    break;
            }
        }
        /// <summary>
        /// 指定类型新建板块
        /// </summary>
        /// <param name="index">index整数，0~7分别代指一种板块类型，具体看type注释</param>
        public Brick(int index)
        {
            typenodes = new List<Node>();
            Node node1, node2, node3, node4;
            switch (index)
            {
                case 0:
                    //田字形
                    type = 0;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 1, y = -1 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node4);
                    break;
                case 1:
                    //|字形
                    type = 1;
                    node1 = new Node() { x = 2, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node4);
                    break;
                case 2:
                    //T字形
                    type = 2;
                    node1 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node4);
                    break;
                case 3:
                    //z字形
                    type = 3;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 1, y = 1 };
                    typenodes.Add(node4);
                    break;
                case 4:
                    //s字形
                    type = 4;
                    node1 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = -1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 1 };
                    typenodes.Add(node4);
                    break;
                case 5:
                    //J字形
                    type = 5;
                    node1 = new Node() { x = 0, y = 2 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = 1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = -1, y = 0 };
                    typenodes.Add(node4);
                    break;
                case 6:
                    //L字形
                    type = 6;
                    node1 = new Node() { x = 0, y = 2 };
                    typenodes.Add(node1);
                    node2 = new Node() { x = 0, y = 1 };
                    typenodes.Add(node2);
                    node3 = new Node() { x = 0, y = 0 };
                    typenodes.Add(node3);
                    node4 = new Node() { x = 1, y = 0 };
                    typenodes.Add(node4);
                    break;
            }
        }
        /// <summary>
        /// 仅改变typenode来逆时针旋转，不考虑越界
        /// </summary>
        public void Rotate()
        {
            List<Node> new_typenodes = new List<Node>();
            foreach (Node item in typenodes)
                new_typenodes.Add(item.Trans());
            typenodes = new_typenodes;
        }
        /// <summary>
        /// 仅改变typenode来逆时针旋转，不考虑越界
        /// </summary>
        /// <param name="time">旋转次数</param>
        public void Rotate(int time)
        {
            for (int i = 0; i < time; i++)
                Rotate();
        }
        /// <summary>
        /// 使板块逆时针旋转90度，只忽略背景图矩阵上界
        /// </summary>
        /// <returns>返回false表示旋转失败</returns>
        public bool Transform()
        {
            List<Node> new_posnodes = new List<Node>();
            List<Node> new_typenodes = new List<Node>();
            foreach (Node item in typenodes)
            {
                new_item = item.Trans() + pos;
                if (new_item.y <= Processor.rows - 1)
                {
                    if (new_item.x > Processor.columns - 1 || new_item.x < 0 || new_item.y < 0 || Processor.arr[new_item.x, new_item.y] == 1) return false;
                    new_posnodes.Add(new_item);
                }
                new_typenodes.Add(item.Trans());
            }
            posnodes = new_posnodes;
            typenodes = new_typenodes;
            eswn = (eswn + 1) % 4;
            return true;
        }
        /// <summary>
        /// 尝试左移，如能就左移
        /// </summary>
        public void Leftmove()
        {
            if (Canmove(lpos + pos)) pos += lpos;
        }
        /// <summary>
        /// 尝试右移，如能就右移
        /// </summary>
        public void Rightmove()
        {
            if (Canmove(rpos + pos)) pos += rpos;
        }
        /// <summary>
        /// 尝试下移，如能就下移
        /// </summary>
        /// <returns></returns>
        public bool Dropmove()
        {
            if (Canmove(dpos + pos)) { pos += dpos; return true; } else return false;
        }
        /// <summary>
        /// 判断能否移动到new_pos
        /// </summary>
        /// <param name="new_pos">Node类坐标</param>
        /// <returns>返回能否</returns>
        public bool Canmove(Node new_pos)
        {
            List<Node> new_posnodes = new List<Node>();
            foreach (Node item in typenodes)
            {
                Node new_item = new_pos + item;
                //三边满足
                if (new_item.x >= 0 && new_item.x < Processor.columns && new_item.y >= 0)
                {
                    //上越界
                    if (new_item.y > Processor.rows - 1) continue;
                    //四边满足有重合
                    else if (Processor.arr[new_item.x, new_item.y] == 1) return false;
                    //四边满足无重合
                    else new_posnodes.Add(new_item);
                }
                else return false;
            }
            posnodes = new_posnodes;
            return true;
        }
        /// <summary>
        /// 中间变量，请勿打扰
        /// </summary>
        private Node new_item;
        /// <summary>
        /// 预定义的左方偏移
        /// </summary>
        private static Node lpos = new Node
        {
            x = -1,
            y = 0
        };
        /// <summary>
        /// 预定义右方偏移
        /// </summary>
        private static Node rpos = new Node
        {
            x = 1,
            y = 0
        };
        /// <summary>
        /// 预定义向下偏移
        /// </summary>
        private static Node dpos = new Node
        {
            x = 0,
            y = -1
        };
        /// <summary>
        /// 模式匹配数组
        /// </summary>
        public static int[,] MatchPattern = new int[,] {
        { 2,0,0,0,0},{ 2,0,0,0,0},{ 2,0,0,0,0},{ 2,0,0,0,0},//田字
        { 4,0,0,0,0},{ 1,0,0,0,0},{ 4,0,0,0,0},{ 1,0,0,0,0},//一字
        { 3,0,-1,0,0},{ 2,0, 1,0,0 },{ 3,0,0,0,0 },{ 2,0,-1,0,0  },//T字
        { 2,0,1,0,0 },{ 3,0,-1,-1,0},{ 2,0,1,0,0 },{ 3,0,-1,-1,0},//Z字，2改成3增强不死性（概率不均时）
        { 2,0,-1,0,0},{ 3,0,0,1,0  },{ 2,0,-1,0,0},{ 3,0,0,1,0  },//S字，2改成3增强不死性（概率不均时）
        { 2,0,0,0,0 },{ 3,0,0,-1,0 },{ 2,0,2,0,0 },{ 3,0,0,0,0  },//J字
        { 2,0,0,0,0 },{ 3,0,0,0,0  },{ 2,0,-2,0,0},{ 2,0,1,1,0  },//L字
        };
        /// <summary>
        /// 标记砖块的旋转状态
        /// </summary>
        public int eswn = 0;
    }

    public class Node
    {
        /// <summary>
        /// x坐标
        /// </summary>
        public int x;
        /// <summary>
        /// y坐标
        /// </summary>
        public int y;
        /// <summary>
        /// 重载运算符+使得Node类可以直接相加
        /// </summary>
        /// <param name="a">Node类对象</param>
        /// <param name="b">Node类对象</param>
        /// <returns>Node类对象</returns>
        public static Node operator +(Node a, Node b)
        {
            Node node = new Node
            {
                x = a.x + b.x,
                y = a.y + b.y
            };
            return node;
        }
        /// <summary>
        /// 使Node类对象逆时针旋转90度
        /// </summary>
        /// <returns>逆时针旋转90度后Node类对象</returns>
        public Node Trans()
        {
            Node new_node = new Node();
            new_node.x = -y;
            new_node.y = x;
            return new_node;
        }
    }
}
