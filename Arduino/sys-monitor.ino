#include <LiquidCrystal.h>
// initialize the library with the numbers of the interface pins
LiquidCrystal lcd(12, 11, 5, 4, 3, 2);

byte cpu[] = {
  B00000,
  B11111,
  B10001,
  B11001,
  B11101,
  B11111,
  B00000,
  B00000
};


byte gpu[] = {
  B11110,
  B11110,
  B11110,
  B10010,
  B10011,
  B10011,
  B10011,
  B11100
};

void setup() {
  // set up the LCD's number of columns and rows:
  lcd.begin(16, 2);
  lcd.createChar(0, cpu);
  lcd.createChar(1, gpu);
  lcd.setCursor(0,0);
  lcd.write(byte(0));
  lcd.setCursor(0,1);
  lcd.write(byte(1));
  lcd.setCursor(4,0);
  lcd.write("RAM");
  lcd.setCursor(4,1);
  lcd.print("VRM");
  lcd.setCursor(10,1);
  lcd.print("PWR");
  Serial.begin(9600);
}

void loop() {
  // set the cursor to column 0, line 1
  // (note: line 1 is the second row, since counting begins with 0):
  lcd.setCursor(0, 1);
  if (Serial.available() > 0) {
  String sensor_info = Serial.readStringUntil('\n');
  lcd.setCursor(1, 0);
  lcd.print(getValue(sensor_info, ':', 0));
  lcd.setCursor(7, 0);
  lcd.print(getValue(sensor_info, ':', 1));
  lcd.setCursor(7, 1);
  lcd.setCursor(1, 1);
  lcd.print(getValue(sensor_info, ':', 2));
  lcd.setCursor(7, 1);
  lcd.print(getValue(sensor_info, ':', 4));
  lcd.setCursor(13, 1);
  lcd.print(getValue(sensor_info, ':', 8));
  }
}

String getValue(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return index<9 ? data.substring(strIndex[0], strIndex[1]) : data.substring(strIndex[0], strIndex[1]-1);
}
