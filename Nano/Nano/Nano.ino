
// 配置
const int thresholdHigh = 3 * 1024 / 5, thresholdLow = 1 * 1024 / 5; //阈值

// 全局变量
int columnIndex = 0; // 当前列位置

int step = 0; // 按键状态
unsigned long lastPressTime; //上次按键时间

int keyIndex = -1, KeyCount = 0;
int keyArray[10];

void setup() {
	ADCSRA = (ADCSRA & 0xF8) | 0x02; // 设置分频器
	Serial.begin(115200); // 初始化串口通信

	for (int i = 2;i < 10;i++)
		pinMode(i, OUTPUT);

	pinMode(A0, INPUT);
	pinMode(A1, INPUT);
}

int channelRead(byte channel, uint8_t pin)
{
	digitalWrite(6, channel & 0b1);
	digitalWrite(7, channel >> 1 & 0b1);
	digitalWrite(8, channel >> 2 & 0b1);
	digitalWrite(9, channel >> 3 & 0b1);
	return analogRead(pin);
}

void loop() {
	//检测列数据
	if (channelRead(columnIndex, A0) > thresholdHigh)
	{
		byte data[3] = { 0, 0, 0 };
		data[0] = columnIndex << 4;
		analogRead(A1); //等待ADC稳定
		data[0] |= (channelRead(5, A1) < thresholdLow) << 3;
		data[0] |= (channelRead(4, A1) < thresholdLow) << 2;
		data[0] |= (channelRead(3, A1) < thresholdLow) << 1;
		data[0] |= (channelRead(2, A1) < thresholdLow);
		data[1] = (channelRead(1, A1) < thresholdLow) << 7;
		data[1] |= (channelRead(0, A1) < thresholdLow) << 6;
		data[1] |= (channelRead(15, A0) < thresholdLow) << 5;
		data[1] |= (channelRead(14, A0) < thresholdLow) << 4;
		data[1] |= (channelRead(13, A0) < thresholdLow) << 3;
		data[1] |= (channelRead(12, A0) < thresholdLow) << 2;
		data[1] |= (channelRead(11, A0) < thresholdLow) << 1;
		data[1] |= (channelRead(10, A0) < thresholdLow);
		analogRead(A1); //等待ADC稳定
		data[2] = (channelRead(8, A1) < thresholdLow) << 7;
		data[2] |= (channelRead(9, A1) < thresholdLow) << 6;
		data[2] |= (channelRead(10, A1) < thresholdLow) << 5;
		data[2] |= (channelRead(11, A1) < thresholdLow) << 4;
		data[2] |= (channelRead(12, A1) < thresholdLow) << 3;
		data[2] |= (channelRead(13, A1) < thresholdLow) << 2;
		data[2] |= (channelRead(14, A1) < thresholdLow) << 1;
		data[2] |= (channelRead(15, A1) < thresholdLow);
		Serial.write(data, 3);
		columnIndex++;
		if (columnIndex > 9)
			columnIndex = 0;
	}

	// 检查串口是否接收到数据
	if (step == 0 && Serial.available() > 0) {
		byte data = Serial.read();
		int change = (data >> 4) & 0b1111;
		int dir = (data >> 3) & 0b1;
		int move = data & 0b111;

		keyIndex = 0;
		KeyCount = change + move;

		for (int i = 0; i < change; i++) {
			keyArray[keyIndex++] = 3;
		}

		int pin = dir == 0 ? 4 : 5;
		for (int i = 0; i < move; i++) {
			keyArray[keyIndex++] = pin;
		}

		keyIndex = 0;
		step = 1;
	}
	handleKeyPress(millis());
}

void handleKeyPress(unsigned long currentTime) {
	switch (step) {
	case 1:
		digitalWrite(keyArray[keyIndex], HIGH);
		lastPressTime = currentTime;
		step = 2;
		break;
	case 2:
		if (currentTime - lastPressTime >= 50) {
			digitalWrite(keyArray[keyIndex], LOW);
			lastPressTime = currentTime;
			step = 3;
		}
		break;
	case 3:
		if (currentTime - lastPressTime >= 30) {
			keyIndex++;
			step = keyIndex >= KeyCount ? 0 : 1;
		}
		break;
	}
}
