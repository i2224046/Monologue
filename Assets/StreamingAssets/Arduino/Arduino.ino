#include <Keyboard.h>

const int sensorPin = 7; 

int lastSensorState = HIGH;
unsigned long detectionStartTime = 0;
unsigned long cooldownEndTime = 0; // クールダウン終了時刻
bool isDetecting = false;          // カウント中かどうか
bool actionDone = false;           // アクション実行済みか
int lastPrintedSec = 0;

void setup() {
  pinMode(sensorPin, INPUT);
  Keyboard.begin();
  Serial.begin(9600);
}

void loop() {
  int currentSensorState = digitalRead(sensorPin);
  unsigned long currentTime = millis();
  bool inCooldown = (currentTime < cooldownEndTime);

  // --- 1. クールダウン中の場合 ---
  if (inCooldown) {
    // クールダウン中に手をかざしている場合のログ（うるさければ削除可）
    if (currentSensorState == LOW) {
      // Serial.println("待機中... (クールダウン)"); 
    }
    // ここでは何もしない（isDetectingもtrueにしない）
  }
  
  // --- 2. クールダウンではない（または終了した）場合 ---
  else {
    
    // A. 検知スタート条件の変更
    // 「今センサーが反応している(LOW)」かつ「まだカウント開始していない(!isDetecting)」ならスタート
    // これにより、クールダウン明けに手が置きっぱなしでもここに入ります。
    if (currentSensorState == LOW && !isDetecting) {
      isDetecting = true;
      detectionStartTime = currentTime;
      actionDone = false;
      lastPrintedSec = 0;
      Serial.println("検知開始 (0秒)");
    }

    // B. 検知継続中の処理
    if (isDetecting && currentSensorState == LOW) {
      unsigned long elapsedTime = currentTime - detectionStartTime;
      int currentSec = elapsedTime / 1000;

      if (currentSec > lastPrintedSec) {
        Serial.print(currentSec);
        Serial.println("秒");
        lastPrintedSec = currentSec;
      }

      // 3秒経過 & 未実行なら発火
      if (elapsedTime >= 1500 && !actionDone) {
        Keyboard.press(' ');
        delay(50);
        Keyboard.release(' ');
        
        Serial.println(">> 1.5秒到達: SPACE送信 <<");
        actionDone = true; 
      }
    }

    // C. 手が離れた処理
    // 検知モード中に手が離れたら終了＆クールダウン開始
    if (isDetecting && currentSensorState == HIGH) {
      isDetecting = false;
      cooldownEndTime = currentTime + 1500; // 3秒のクールダウンセット
      
      if (actionDone) {
        Serial.println("離れました。クールダウン開始(1.5秒)...");
      } else {
        Serial.println("キャンセル。クールダウン開始(1.5秒)...");
      }
    }
  }

  lastSensorState = currentSensorState;
  delay(10); 
}