
// PCI sensors (PORTB group: D8–D13)
// const byte saves memory and prevents accidental change
const byte PIR_PIN = 8;
const byte IR_PIN = 9;
const byte ARM_PIN = 10;

// Analog sensor
const byte LDR_PIN = A0;

// Actuators
const byte RED_LED = 3;
const byte GREEN_LED = 4;
const byte BUZZER = 5;



//  SHARED FLAGS 
// volatile is a global shared variable used to data transfer between ISR and  Main loop
// if we want global shared varible to change change valuses in real time, we declare them volatile.
//volatile prevent error at complie time. 
volatile bool pirFlag = false;
volatile bool irFlag = false;
volatile bool armState = false;

// signals main loop that PCI occurred
volatile bool pciTriggered = false;

// Timer data - tells main loop to run periodic task.
volatile bool timerTick = false;

// System state - where the system is armed or disarmed.
bool intrusion = false;
int lightLevel = 0;
bool isDark = false;

// used for detecting manual reset 
bool prevArmState = false;

void setup()
{
  // Starts serial communication
  Serial.begin(9600);

  // Sensor pins - sets digital sensors as inputs
  pinMode(PIR_PIN, INPUT);
  pinMode(IR_PIN, INPUT);
  pinMode(ARM_PIN, INPUT);

  // Actuators - sets actuators as outputs
  pinMode(RED_LED, OUTPUT);
  pinMode(GREEN_LED, OUTPUT);
  pinMode(BUZZER, OUTPUT);

  // Enable PCI for PORTB (D8–D13) , PCIE0 is a bit insde the PCICR group
  PCICR |= (1 << PCIE0);  
  PCMSK0 |= (1 << PCINT0); // D8
  PCMSK0 |= (1 << PCINT1); // D9
  PCMSK0 |= (1 << PCINT2); // D10

  // Calls timer function.
  setupTimer1();

  // prints on serial monitor that the system is ready
  Serial.println("System Ready");
}


// 
void setupTimer1()
{
  //Temporarily disables interrupts that are going on
  noInterrupts();

  // Clears timer control registers
  // 
  TCCR1A = 0;
  TCCR1B = 0;

  // Resets timer counter to zero.
  TCNT1 = 0;

  OCR1A = 7812; // ~0.5 sec at 16 MHz with 1024 prescaler ()

  TCCR1B |= (1 << WGM12);  // CTC mode
  TCCR1B |= (1 << CS12) | (1 << CS10); // 1024 prescaler

  // enable timer compare interrup
  TIMSK1 |= (1 << OCIE1A); 
  
 // Re-enables global interrupts.
  interrupts();
}



// Signals main loop that an event occurred.
ISR(PCINT0_vect)
{
  pciTriggered = true;

  //Captures current sensor states.
  //Stores in shared flags.
  pirFlag = digitalRead(PIR_PIN);
  irFlag = digitalRead(IR_PIN);
  armState = digitalRead(ARM_PIN);
}


// Runs every 0.5 s.
//Only sets a flag
ISR(TIMER1_COMPA_vect)
{
  timerTick = true;
}



// 
void loop()
{
  // ===== PERIODIC TASK =====
  if (timerTick)
  {
    timerTick = false;

    // Reads ambient light.
    lightLevel = analogRead(LDR_PIN);
    isDark = (lightLevel < 500);

    Serial.print("Light: ");
    Serial.println(lightLevel);
  }

  // MANUAL RESET DETECTION
  // Means user just turned system OFF.
   if (prevArmState == true && armState == false)
   {
  //Clears alarm - buzzer do not make any noise.
     intrusion = false;
     Serial.println("System manually reset");
    }

  // Updates previous state memory
     prevArmState = armState;
  
  // EVENT PROCESSING 
  if (pciTriggered)
  {
    pciTriggered = false;

    //Shows real-time sensor status
    Serial.print("ARM: ");
    Serial.print(armState);
    Serial.print(" PIR: ");
    Serial.print(pirFlag);
    Serial.print(" IR: ");
    Serial.println(irFlag);

    // Intrusion logic
    // Intrusion logic (night-only)
if (armState && isDark && (pirFlag || irFlag))
{
  intrusion = true;   // stays true until disarmed
}
  }
  // ACT
  if (!armState)
  {
    // DISARMED
    intrusion = false;
    digitalWrite(GREEN_LED, HIGH);
    digitalWrite(RED_LED, LOW);
    digitalWrite(BUZZER, LOW);
  }
  else
  {
    digitalWrite(GREEN_LED, LOW);

    if (intrusion)
    {
      digitalWrite(RED_LED, HIGH);
      digitalWrite(BUZZER, HIGH);
    }
    else
    {
      digitalWrite(RED_LED, LOW);
      digitalWrite(BUZZER, LOW);
    }
  }}



