using System.Diagnostics;
using System.IO.Ports;

namespace TetrisApp
{
    public class Connector : IDisposable
    {
        SerialPort serialPort;

        int receivedIndex = 0;
        byte[] receivedData = new byte[3];
        byte[] receivedBuffer = new byte[4096];

        int[] gridData = new int[10];

        int maxRow = 0;
        Rectangle rectGrid;

        bool readyToTrigger = false;

        public event EventHandler OnFrameData;
        public event EventHandler<int> OnColumnData;
        public event EventHandler<int> OnTetrisData;

        public bool Init(string portName, int baudRate)
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate);
                serialPort.DataReceived += OnDataReceived;
                serialPort.Open();

                return true;
            }
            catch
            {
                return false;
            }
        }

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
        }

        private bool UpdateGridData(byte[] data)
        {
            var d1 = data[0];
            var col = d1 >> 4;
            if (col > 9)
                return false;

            var d2 = data[1];
            var d3 = data[2];

            gridData[col] = (d1 & 0b1111) << 16;
            gridData[col] |= d2 << 8;
            gridData[col] |= d3;

            OnColumnData?.Invoke(this, col);

            if (col == 9)
            {
                OnFrameData?.Invoke(this, EventArgs.Empty);

                var combined = 0;
                for (int i = 0; i < gridData.Length; i++)
                    combined |= gridData[i];

                var range = GetRange(combined);
                maxRow = range.Item1 == 0 ? range.Item2 : -1;

                combined &= 0x00ffffff << (maxRow + 1);
                range = GetRange(combined);
                var startRow = range.Item1;
                var endRow = range.Item2;

                combined = 0;
                for (int i = 0; i < gridData.Length; i++)
                {
                    if (((0x00ffffff << (maxRow + 1)) & gridData[i]) > 0)
                        combined |= 1 << i;
                }
                range = GetRange(combined);
                var startColumn = range.Item1;
                var endColumn = range.Item2;

                rectGrid = Rectangle.FromLTRB(startColumn, startRow, endColumn, endRow);

                if (rectGrid.Left == -1 && rectGrid.Right == -1 && rectGrid.Top == -1 && rectGrid.Bottom == -1)
                {
                    readyToTrigger = true;
                    return true;
                }

                var tetris = MatchTetris();

                if (readyToTrigger && tetris != -1)
                {

                    Debug.WriteLine($"tetris = {tetris}");
                    for (int i = 0; i < gridData.Length; i++)
                        Debug.Write($"{(i == 0 ? "" : ",")}{gridData[i]}");
                    Debug.WriteLine(Environment.NewLine + "===========================");

                    OnTetrisData?.Invoke(this, tetris);
                    readyToTrigger = false;
                }
            }

            return true;
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

        int MatchTetris()
        {
            int w = rectGrid.Width + 1, h = rectGrid.Height + 1;
            if (w == 2 && h == 2)
            {
                if (CheckCells(0b1111)) return 30;
            }
            else if (w == 1 && h == 4)
                return 0;
            else if (w == 4 && h == 1)
                return 1;
            else if (w == 2 && h == 3)
            {
                if (CheckCells(0b101101)) return 40; // S90
                if (CheckCells(0b011110)) return 61; // Z90
                if (CheckCells(0b101011)) return 20; // L0
                if (CheckCells(0b110101)) return 22; // L180
                if (CheckCells(0b010111)) return 10; // J0
                if (CheckCells(0b111010)) return 12; // J180
                if (CheckCells(0b011101)) return 51; // T90
                if (CheckCells(0b101110)) return 53; // T270
            }
            else if (w == 3 && h == 2)
            {
                if (CheckCells(0b011110)) return 41; // S0
                if (CheckCells(0b110011)) return 60; // Z0
                if (CheckCells(0b111100)) return 21; // L90
                if (CheckCells(0b001111)) return 23; // L270
                if (CheckCells(0b100111)) return 11; // J90
                if (CheckCells(0b111001)) return 13; // J270
                if (CheckCells(0b111010)) return 50; // T0
                if (CheckCells(0b010111)) return 52; // T180
            }
            return -1;
        }

        bool CheckCells(int expected)
        {
            int width = rectGrid.Width + 1, height = rectGrid.Height + 1;
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    var expectedValue = expected >> (row * width + column) & 0b1;
                    if ((gridData[rectGrid.X + column] >> (rectGrid.Y + row) & 0b1) != expectedValue)
                        return false;
                }
            }
            return true;
        }

        public int GetMaxRow()
        {
            return 19 - maxRow;
        }

        public Rectangle GetRectangle()
        {
            return Rectangle.FromLTRB(9 - rectGrid.Left, 19 - rectGrid.Top, 9 - rectGrid.Right, 19 - rectGrid.Bottom);
        }

        public Tuple<int, int> GetColumnRange()
        {
            return Tuple.Create(9 - 0, 9 - 0);
        }

        public bool GetCellStatus(int column, int row)
        {
            return (gridData[9 - column] >> (19 - row) & 0b1) == 1;
        }

        public void Send(byte data)
        {
            serialPort.BaseStream.WriteByte(data);
            serialPort.BaseStream.Flush();
        }

        public void Dispose()
        {
            serialPort.Close();
            serialPort.Dispose();
        }

        static byte[][] Mapper = new byte[][] {

            // O
            new byte[] {0x66, 0x00, 30 },
            new byte[] {0x06, 0x60, 30 },
            new byte[] {0x00, 0x66, 30 },
                    
            // I 0	     
            new byte[] {0x22, 0x20, 0  },
            new byte[] {0x22, 0x22, 0  },
                    
            // I 90	     
            new byte[] {0xf0, 0x00, 1  },
            new byte[] {0x0f, 0x00, 1  },
            new byte[] {0x00, 0xf0, 1  },
            new byte[] {0x00, 0x0f, 1  },
                    
            // S 0	     
            new byte[] {0x6c, 0x00, 41 },
            new byte[] {0x06, 0xc0, 41 },
            new byte[] {0x00, 0x6c, 41 },
                    
            // S 90
            new byte[] {0x8c, 0x40, 40 },
            new byte[] {0x08, 0xc4, 40 },
                    
            // Z 0
            new byte[] {0xc6, 0x00, 60 },
            new byte[] {0x0c, 0x60, 60 },
            new byte[] {0x00, 0xc6, 60 },
                    
            // Z 90
            new byte[] {0x4c, 0x80, 61 },
            new byte[] {0x04, 0xc8, 61 },
                    
            // J 0
            new byte[] {0x44, 0xc0, 10 },
            new byte[] {0x04, 0x4c, 10 },
                    
            // J 90
            new byte[] {0x8e, 0x00, 11 },
            new byte[] {0x08, 0xe0, 11 },
            new byte[] {0x00, 0x8e, 11 },
                    
            // J 180
            new byte[] {0xc8, 0x80, 12 },
            new byte[] {0x0c, 0x88, 12 },
                    
            // J 270
            new byte[] {0xe2, 0x00, 13 },
            new byte[] {0x0e, 0x20, 13 },
            new byte[] {0x00, 0xe2, 13 },
                    
            // L 0
            new byte[] {0x88, 0xc0, 20 },
            new byte[] {0x08, 0x8c, 20 },
                    
            // L 90
            new byte[] {0xe8, 0x00, 21 },
            new byte[] {0x0e, 0x80, 21 },
            new byte[] {0x00, 0xe8, 21 },
                    
            // L 180
            new byte[] {0xc4, 0x40, 22 },
            new byte[] {0x0c, 0x44, 22 },
                    
            // L 270   
            new byte[] {0x2e, 0x00, 23 },
            new byte[] {0x02, 0xe0, 23 },
            new byte[] {0x00, 0x2e, 23 },
                    
            // T 0	     
            new byte[] {0xe4, 0x00, 50 },
            new byte[] {0x0e, 0x40, 50 },
            new byte[] {0x00, 0xe4, 50 },
                    
            // T 90	   
            new byte[] {0x4c, 0x40, 51 },
            new byte[] {0x04, 0xc4, 51 },
                    
            // T 180     
            new byte[] {0x4e, 0x00, 52 },
            new byte[] {0x04, 0xe0, 52 },
            new byte[] {0x00, 0x4e, 52 },
                    
            // T 270     
            new byte[] {0x8c, 0x80, 53 },
            new byte[] {0x08, 0xc8, 53 }
        };
    }

}
