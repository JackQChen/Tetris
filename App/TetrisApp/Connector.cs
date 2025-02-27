using System.IO.Ports;

namespace TetrisApp
{
    public class Connector : IDisposable
    {
        SerialPort serialPort;

        int receivedIndex = 0;
        byte[] receivedData = new byte[3];
        byte[] receivedBuffer = new byte[2048];

        byte[] mapperData = new byte[2];

        public bool[,] GridData = new bool[10, 20];

        public DateTime LastUpdatedTime = DateTime.MinValue;

        public event EventHandler OnFrameData;
        public event EventHandler<int> OnColumnData;
        public event EventHandler<int> OnTetrisData;

        public bool Init(string portName, int baudRate)
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate);
                serialPort.DataReceived += OnDataReceived;

                serialPort.WriteTimeout = 1;
                serialPort.WriteBufferSize = 2;
                serialPort.ReadTimeout = 1;
                serialPort.ReadBufferSize = 32;

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

        private bool UpdateGridData(byte[] buffer)
        {
            var d1 = buffer[0];
            var col = d1 >> 4;
            if (col > 9)
                return false;
            GridData[col, 0] = (d1 >> 3 & 0b1) == 1;
            GridData[col, 1] = (d1 >> 2 & 0b1) == 1;
            GridData[col, 2] = (d1 >> 1 & 0b1) == 1;
            GridData[col, 3] = (d1 & 0b1) == 1;
            var d2 = buffer[1];
            GridData[col, 4] = (d2 >> 7 & 0b1) == 1;
            GridData[col, 5] = (d2 >> 6 & 0b1) == 1;
            GridData[col, 6] = (d2 >> 5 & 0b1) == 1;
            GridData[col, 7] = (d2 >> 4 & 0b1) == 1;
            GridData[col, 8] = (d2 >> 3 & 0b1) == 1;
            GridData[col, 9] = (d2 >> 2 & 0b1) == 1;
            GridData[col, 10] = (d2 >> 1 & 0b1) == 1;
            GridData[col, 11] = (d2 & 0b1) == 1;
            var d3 = buffer[2];
            GridData[col, 12] = (d3 >> 7 & 0b1) == 1;
            GridData[col, 13] = (d3 >> 6 & 0b1) == 1;
            GridData[col, 14] = (d3 >> 5 & 0b1) == 1;
            GridData[col, 15] = (d3 >> 4 & 0b1) == 1;
            GridData[col, 16] = (d3 >> 3 & 0b1) == 1;
            GridData[col, 17] = (d3 >> 2 & 0b1) == 1;
            GridData[col, 18] = (d3 >> 1 & 0b1) == 1;
            GridData[col, 19] = (d3 & 0b1) == 1;

            OnColumnData?.Invoke(this, col);

            if (col == 0)
                OnFrameData?.Invoke(this, EventArgs.Empty);

            if (DateTime.Now - LastUpdatedTime < TimeSpan.FromSeconds(1))
                return true;

            if (col == 3)
            {
                mapperData[0] = 0;
                mapperData[1] = 0;
                var pos = 8;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 4; j++)
                        mapperData[0] |= (byte)((GridData[j + 3, i] ? 1 : 0) << --pos);
                pos = 8;
                for (int i = 0; i < 2; i++)
                    for (int j = 0; j < 4; j++)
                        mapperData[1] |= (byte)((GridData[j + 3, i + 2] ? 1 : 0) << --pos);
                var matchedTetris = Mapper.FirstOrDefault(p => p[0] == mapperData[0] && p[1] == mapperData[1]);
                if (matchedTetris != null)
                {
                    LastUpdatedTime = DateTime.Now;
                    OnTetrisData?.Invoke(this, matchedTetris[2]);
                }
            }
            return true;
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
