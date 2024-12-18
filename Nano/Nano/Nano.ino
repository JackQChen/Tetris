const int ROWS = 4; // 行数
const int COLS = 4; // 列数
bool pixelData[ROWS][COLS]; // 用于存储像素点亮灭状态

// 配置
const int analogPin = A0;        // 模拟信号输入引脚
const int thresholdHigh = 3 * 1024 / 5, thresholdLow = 1 * 1024 / 5; //阈值
const int debounceTime = 3;     // 防抖时间（单位：ms）

// 全局变量
unsigned long lastTriggerTime = 0; // 上次触发的时间
unsigned long lastReportTime = 0;  // 上次报告的时间

void setup() {
	ADCSRA = (ADCSRA & 0xF8) | 0x02; // 设置分频器
	Serial.begin(9600); // 初始化串口通信
}

void loop() {
	// 获取当前时间
	unsigned long currentTime = millis();

	// 判断是否超过触发阈值
	if (analogRead(analogPin) > thresholdHigh) {
		// 判断是否超过防抖时间
		if (currentTime - lastTriggerTime > debounceTime) {
			lastTriggerTime = currentTime; // 更新触发时间
			updatePixelData();
		}
	}

	// 发送数据
	if (currentTime - lastReportTime >= 50) {
		lastReportTime = currentTime; // 更新报告时间
		// 发送整个矩阵
		uint8_t packedData[3] = { 0 };
		for (int row = 0; row < ROWS; row++) {
			for (int col = 0; col < COLS; col++) {
				if (pixelData[row][col]) {
					int bitIndex = row * COLS + col;
					packedData[bitIndex / 8] |= (1 << (bitIndex % 8));
				}
			}
		}
		// 计算校验和
		packedData[2] = packedData[0] ^ packedData[1];

		for (int i = 0;i < 3;i++)
			Serial.write(packedData[i]);
		Serial.write(0xff); // 标记传输结束
	}

	// 稍作延时，避免过于频繁采样
	delay(1);
}

// 更新像素矩阵中指定列的数据
void updatePixelData()
{
	for (int i = 0; i < 4; i++) {
		pixelData[0][i] = analogRead(A1) < thresholdLow;
		pixelData[1][i] = analogRead(A2) < thresholdLow;
		pixelData[2][i] = analogRead(A3) < thresholdLow;
		pixelData[3][i] = analogRead(A4) < thresholdLow;
		delayMicroseconds(1900);
	}
}