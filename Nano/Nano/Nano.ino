#include "dic.h"
const int ROWS = 4; // 行数
const int COLS = 4; // 列数
bool pixelData[ROWS][COLS]; // 用于存储像素点亮灭状态

// 配置
const int analogPin = A0;        // 模拟信号输入引脚
const int thresholdHigh = 3 * 1024 / 5, thresholdLow = 1 * 1024 / 5; //阈值
const int debounceTime = 3;     // 防抖时间（单位：ms）

// 全局变量
unsigned long lastTriggerTime = 0; // 上次触发的时间
unsigned long lastDetectTime = 0;  // 上次检测的时间
unsigned long lastReportTime = 0;  // 上次报告的时间

int lastReportValue = 0; // 上次报告的值

void setup() {
	ADCSRA = (ADCSRA & 0xF8) | 0x02; // 设置分频器
	Serial.begin(9600); // 初始化串口通信
	pinMode(2, OUTPUT);
	pinMode(3, OUTPUT);
	pinMode(4, OUTPUT);
	pinMode(5, OUTPUT);
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

	// 检测数据
	if (currentTime - lastDetectTime >= 50) {
		lastDetectTime = currentTime; // 更新检测时间
		// 计算整个矩阵
		uint8_t packedData[2] = { 0 };
		for (int row = 0; row < ROWS; row++) {
			for (int col = 0; col < COLS; col++) {
				if (pixelData[row][col]) {
					int bitIndex = row * COLS + col;
					packedData[bitIndex / 8] |= (1 << (bitIndex % 8));
				}
			}
		}
		// 匹配数据
		int value = findValue(packedData);
		if (value != -1 && lastReportValue != value)
		{
			if (currentTime - lastReportTime >= 500)
			{
				lastReportTime = currentTime;
				Serial.write(value);
			}
		}
		lastReportValue = value;
	}

	// 检查串口是否接收到数据
	if (Serial.available() > 0) {
		byte data = Serial.read();
		if (data == 0xff)
		{
			digitalWrite(5, HIGH);
			delay(80);
			digitalWrite(5, LOW);
			delay(80);
			return;
		}
		// 读取数据
		int change = (data >> 4) & 0b1111;
		int dir = (data >> 3) & 0b1;
		int move = data & 0b111;

		for (int i = 0;i < change;i++)
		{
			digitalWrite(4, HIGH);
			delay(80);
			digitalWrite(4, LOW);
			delay(80);
		}
		int pin = dir == 0 ? 2 : 3;
		for (int i = 0;i < move;i++)
		{
			digitalWrite(pin, HIGH);
			delay(80);
			digitalWrite(pin, LOW);
			delay(80);
		}
	}

	// 稍作延时，避免过于频繁采样
	delay(1);
}

int findValue(const byte packedData[2]) {
	// 遍历字典
	for (int i = 0; i < sizeof(dic) / sizeof(dic[0]); i++) {
		// 比较前两个字节是否匹配
		if (dic[i][0] == packedData[0] && dic[i][1] == packedData[1]) {
			return dic[i][2]; // 返回第三个字节作为 value
		}
	}
	return -1; // 未找到时返回 -1 表示无效值
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