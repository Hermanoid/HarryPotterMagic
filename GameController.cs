using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lego.Ev3.Desktop;
using Lego.Ev3.Core;
using Stateless;
using System.Windows.Threading;

namespace Microsoft.Samples.Kinect.InfraredBasics
{

    public partial class GameController
    {
        public const string COM_PORT = "COM3";
        public const int UPDATE_RATE = 10;
        public const InputPort SHOULDER_TOUCH_PORT = InputPort.One;
        public const InputPort ELBOW_TOUCH_PORT = InputPort.Two;
        public const OutputPort SHOULDER_MOTOR_PORT = OutputPort.A;
        public const OutputPort ELBOW_MOTOR_PORT = OutputPort.B;


        public const int RAISED_TIME = 75;
        public const int LEVELONE_TIME = 45;
        public const int LEVELTWO_TIME = 20;
        public const int LEVELTHREE_TIME = 10;
        public const int REPARO_TIME = 20;

        public readonly BluetoothController bluetoothController;
        public EventHandler<object> OnTimerTick;
        public Brick brick;
        public StateMachine<State, Trigger> machine;
        public int current_time = 0;
        private DispatcherTimer internalTimer;
        private bool? shoulderInitialState = null;
        public GameController()
        {
            bluetoothController = new BluetoothController();
            brick = new Brick(new BluetoothCommunication(COM_PORT), true);
            machine = InitMachine();
            internalTimer = new DispatcherTimer();
            internalTimer.Interval = new TimeSpan(0, 0, 0, 0, 1 / UPDATE_RATE);
            internalTimer.Tick += InternalTimer_Tick;
        }

        public async Task Initialize()
        {
            bluetoothController.Initialize();
            internalTimer.Start();
            await brick.ConnectAsync();
            brick.Ports[SHOULDER_TOUCH_PORT].SetMode(TouchMode.Touch);
            brick.Ports[ELBOW_TOUCH_PORT].SetMode(TouchMode.Touch);
            brick.Ports[InputPort.A].SetMode(MotorMode.Degrees);
            brick.Ports[InputPort.B].SetMode(MotorMode.Degrees);
            brick.Ports[InputPort.C].SetMode(MotorMode.Degrees);
            brick.Ports[InputPort.D].SetMode(MotorMode.Degrees);
            //await brick.BatchCommand.
            //await t.ContinueWith(v => Task.Delay(1000));
            //await t.ContinueWith(v => MotorCalibrationUpdate());

        }
        public async Task TriggerSpell(Spell spell)
        {
            await bluetoothController.TriggerSpell(spell); // Trigger bluetooth effects if they're available
            if (machine.CanFire((Trigger)spell)) // The trigger enum is a subset of Spell, so we can cast
            {
                machine.Fire((Trigger)spell);
            }
        }
        public void dispatcherTimer_Tick(object sender, object e)
        {
            if (current_time <= 0)
            {
                current_time = 0;
            }
            else
            {
                current_time--;
                if (current_time <= 0)
                {
                    machine.Fire(Trigger.Times_Up);
                }
            }
        }
        //private async Task MotorCalibrationUpdate()
        //{
        //    bool isCalibrated = true;
        //    if (brick.Ports[SHOULDER_TOUCH_PORT].Type == 0)
        //    {
        //        isCalibrated = false;
        //    }
        //    else
        //    {
        //        bool shoulderState = brick.Ports[SHOULDER_TOUCH_PORT].SIValue == 1;
        //        bool elbowState = brick.Ports[ELBOW_TOUCH_PORT].SIValue == 1;
        //        if (!shoulderInitialState.HasValue)
        //        {
        //            shoulderInitialState = shoulderState;
        //        }
        //        if (shoulderState && shoulderInitialState.Value)
        //        {
        //            isCalibrated = false;
        //            brick.BatchCommand.TurnMotorAtPower(SHOULDER_MOTOR_PORT, 30);
        //        }
        //        else if (!shoulderState && !shoulderInitialState.Value)
        //        {
        //            isCalibrated = false;
        //            brick.BatchCommand.TurnMotorAtPower(SHOULDER_MOTOR_PORT, -30);
        //        }
        //        else
        //        {
        //            brick.BatchCommand.TurnMotorAtPower(SHOULDER_MOTOR_PORT, 0);
        //        }
        //        if (!elbowState)
        //        {
        //            isCalibrated = false;
        //            brick.BatchCommand.TurnMotorAtPower(ELBOW_MOTOR_PORT, -30);
        //        }
        //        else
        //        {
        //            brick.BatchCommand.TurnMotorAtPower(ELBOW_MOTOR_PORT, 0);
        //        }
        //        await brick.BatchCommand.SendCommandAsync();
        //    }
        //    if (isCalibrated)
        //    {
        //        machine.Fire(Trigger.Startup_Complete);
        //    }
        //}
        private void InternalTimer_Tick(object sender, object e)
        {
            //brick.DirectCommand.ReadySIAsync(InputPort.A, MotorMode.Degrees);
            switch (machine.State)
            {
                case State.Startup:

                default:
                    return;
            }
        }

        private void onEnterWaiting()
        {

        }
        private void onEnterRaised()
        {
            current_time = RAISED_TIME;
        }
        private void onEnterLevelOne()
        {
            current_time = LEVELONE_TIME;
        }
        private void onEnterLevelTwo()
        {
            current_time = LEVELTWO_TIME;
        }
        private void onEnterLevelThree()
        {
            current_time = LEVELTHREE_TIME;
        }
        private void onEnterReparo()
        {
            current_time = REPARO_TIME;
        }
        private void onObtainit()
        {

        }
    }
}
