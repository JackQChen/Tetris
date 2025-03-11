using System.Runtime.InteropServices;

namespace TetrisApp
{
    public class Processor
    {
        Connector connector;
        AudioPlayer player;
        bool isWindows = false;
        DateTime lastUpdatedTime = DateTime.Now;

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

            //var maxRow = connector.GetMaxRow() - 1;
            //for (int i = 0; i < 10; i++)
            //{
            //    for (int j = 0; j < 20; j++)
            //    {
            //        if (j < maxRow)
            //            allGrids[i, j].show = false;
            //        else
            //            allGrids[i, j].show = connector.GetCellStatus(i, j);
            //    }
            //}

            //lastUpdatedTime = DateTime.Now;
            //uint[] array = new uint[10];
            //for (int i = 0; i < 10; i++)
            //    for (int j = 0; j < 20; j++)
            //        array[i] |= (allGrids[9 - i, 19 - j].show ? 1U : 0) << j;

            //LoggerAI.Log(string.Join(',', array));
            //LoggerAI.Log($"Tetris = {tetrisData}");

            //var changeType = tetrisData % 10;
            //var tetrisType = tetrisData / 10;
            //changeType = changeType % changeNum[tetrisType];

            //AIControl(tetrisType, changeType);
        }


        private void AIControl()
        {
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
