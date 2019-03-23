int count = 0;

void setup()
{
}


void loop()
{
	if (Bean.getConnectionState() == true)
	{
		Bean.setLed(0, 255, 0);
		count++;

		Bean.setScratchNumber(1, count);
		Bean.setScratchNumber(2, count * 10);
		Bean.setScratchNumber(3, count * 100);
		Bean.setScratchNumber(4, count * 1000);
		Bean.setScratchNumber(5, count * 10000);
	}
	else
	{
		count = 0;
		Bean.setLed(255, 0, 0);
	}
    Bean.sleep(500);
}