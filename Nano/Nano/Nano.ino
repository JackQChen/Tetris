
// 配置
const int thresholdHigh = 3 * 1024 / 5, thresholdLow = 1 * 1024 / 5; //阈值

// 全局变量
int columnIndex = 0; // 当前列位置

void setup() {
	ADCSRA = (ADCSRA & 0xF8) | 0x02; // 设置分频器
	Serial.begin(115200); // 初始化串口通信

	for (int i = 2;i < 10;i++)
		pinMode(i, OUTPUT);

	pinMode(A0, INPUT);
	pinMode(A1, INPUT);
}

int channelRead(byte channel, int pin)
{
	digitalWrite(6, channel >> 3 & 0b1);
	digitalWrite(7, channel >> 2 & 0b1);
	digitalWrite(8, channel >> 1 & 0b1);
	digitalWrite(9, channel & 0b1);
	return analogRead(pin);
}

void loop() {
	//检测列数据
	int channel = 0;
	switch (columnIndex)
	{
	case 0: channel = 0b0000; break;
	case 1: channel = 0b1000; break;
	case 2: channel = 0b0100; break;
	case 3: channel = 0b1100; break;
	case 4: channel = 0b0010; break;
	case 5: channel = 0b1010; break;
	case 6: channel = 0b0110; break;
	case 7: channel = 0b1110; break;
	case 8: channel = 0b0001; break;
	case 9: channel = 0b1001; break;
	}
	if (channelRead(channel, A0) > thresholdHigh)
	{
		byte data[3] = { 0, 0, 0 };
		data[0] = (9 - columnIndex) << 4;
		analogRead(A1); //重置状态
		data[0] |= (channelRead(0b1010, A1) < thresholdLow ? 1 : 0) << 3;
		data[0] |= (channelRead(0b0010, A1) < thresholdLow ? 1 : 0) << 2;
		data[0] |= (channelRead(0b1100, A1) < thresholdLow ? 1 : 0) << 1;
		data[0] |= (channelRead(0b0100, A1) < thresholdLow ? 1 : 0);
		data[1] = (channelRead(0b1000, A1) < thresholdLow ? 1 : 0) << 7;
		data[1] |= (channelRead(0b0000, A1) < thresholdLow ? 1 : 0) << 6;
		analogRead(A0); //重置状态
		data[1] |= (channelRead(0b1111, A0) < thresholdLow ? 1 : 0) << 5;
		data[1] |= (channelRead(0b0111, A0) < thresholdLow ? 1 : 0) << 4;
		data[1] |= (channelRead(0b1011, A0) < thresholdLow ? 1 : 0) << 3;
		data[1] |= (channelRead(0b0011, A0) < thresholdLow ? 1 : 0) << 2;
		data[1] |= (channelRead(0b1101, A0) < thresholdLow ? 1 : 0) << 1;
		data[1] |= (channelRead(0b0101, A0) < thresholdLow ? 1 : 0);
		analogRead(A1); //重置状态
		data[2] = (channelRead(0b0001, A1) < thresholdLow ? 1 : 0) << 7;
		data[2] |= (channelRead(0b1001, A1) < thresholdLow ? 1 : 0) << 6;
		data[2] |= (channelRead(0b0101, A1) < thresholdLow ? 1 : 0) << 5;
		data[2] |= (channelRead(0b1101, A1) < thresholdLow ? 1 : 0) << 4;
		data[2] |= (channelRead(0b0011, A1) < thresholdLow ? 1 : 0) << 3;
		data[2] |= (channelRead(0b1011, A1) < thresholdLow ? 1 : 0) << 2;
		data[2] |= (channelRead(0b0111, A1) < thresholdLow ? 1 : 0) << 1;
		data[2] |= (channelRead(0b1111, A1) < thresholdLow ? 1 : 0);
		Serial.write(data, 3);
		columnIndex++;
		if (columnIndex > 9)
			columnIndex = 0;
	}

	// 检查串口是否接收到数据
	if (Serial.available() > 0) {
		byte data = Serial.read();
		if (data == 0xff)
		{
			digitalWrite(2, HIGH);
			delay(50);
			digitalWrite(2, LOW);
			delay(50);
			return;
		}
		// 读取数据
		int change = (data >> 4) & 0b1111;
		int dir = (data >> 3) & 0b1;
		int move = data & 0b111;

		for (int i = 0;i < change;i++)
		{
			digitalWrite(3, HIGH);
			delay(50);
			digitalWrite(3, LOW);
			delay(50);
		}
		int pin = dir == 0 ? 4 : 5;
		for (int i = 0;i < move;i++)
		{
			digitalWrite(pin, HIGH);
			delay(50);
			digitalWrite(pin, LOW);
			delay(50);
		}
	}
}
