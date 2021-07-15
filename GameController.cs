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
        public int GO_TO_POSITION_POWER = 40;
        //public int GO_TO_POSITION_SHOULDER_POWER = 60;


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
        //private int set_time = 0;
        private DispatcherTimer internalTimer;
        private bool manual_override = false;
        private bool lucas_requested_a_restart = false;
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
            await Task.Delay(500);
            StartPosition = new Dictionary<OutputPort, int>();
            StartPosition[OutputPort.A] = (int)brick.Ports[InputPort.A].SIValue;
            StartPosition[OutputPort.B] = (int)brick.Ports[InputPort.B].SIValue;
            StartPosition[OutputPort.C] = (int)brick.Ports[InputPort.C].SIValue;
            StartPosition[OutputPort.D] = (int)brick.Ports[InputPort.D].SIValue;
            CurrentPosition = PositionDict[State.Startup];
            await machine.FireAsync(Trigger.Startup_Complete);
        }
        public async Task TriggerSpell(Spell spell)
        {
            await bluetoothController.TriggerSpell(spell); // Trigger bluetooth effects if they're available
            if (machine.CanFire((Trigger)spell)) // The trigger enum is a subset of Spell, so we can cast
            {
                await machine.FireAsync((Trigger)spell);
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
                    Task.Run(() => machine.FireAsync(Trigger.Times_Up));
                }
            }
        }

        private async void InternalTimer_Tick(object sender, object e)
        {
            if (lucas_requested_a_restart)
            {
                await GoToPosition(State.Startup);
            }
            //brick.DirectCommand.ReadySIAsync(InputPort.A, MotorMode.Degrees);
            await machine.ActivateAsync();
        }

        private Dictionary<OutputPort, InputPort> OutToIn = new Dictionary<OutputPort, InputPort>()
        {
            { OutputPort.A, InputPort.A },
            { OutputPort.B, InputPort.B },
            { OutputPort.C, InputPort.C },
            { OutputPort.D, InputPort.D }
        };

        private async Task GoToPosition(State state)
        {
            await GoToPosition(PositionDict[state]);
        }
        private async Task GoToPosition(Dictionary<OutputPort, int> position)
        {
            int max_move = 0;
            foreach (var kv in position)
            {
                int displacement = kv.Value - ((int)brick.Ports[OutToIn[kv.Key]].SIValue - StartPosition[kv.Key]);
                bool reverse = false;
                if (displacement < 0)
                {
                    reverse = true;
                    displacement *= -1;
                }
                if(displacement < 100)
                {
                    displacement = 0;
                }
                if(displacement > max_move)
                {
                    max_move = displacement;
                }
                int power = GO_TO_POSITION_POWER * (reverse ? -1 : 1);
                brick.BatchCommand.StepMotorAtSpeed(kv.Key, power, (uint)displacement, true);
            }
            await brick.BatchCommand.SendCommandAsync();
            var estimated_move_seconds = max_move / GO_TO_POSITION_POWER / 10;
            await Task.Delay((int)(estimated_move_seconds * 1.1 * 1000 + 1000));
            CurrentPosition = position;
        }

        private async Task onEnterWaiting()
        {
            current_time = 0;
            await GoToPosition(State.Waiting);
        }
        private async Task onEnterRaised()
        {
            current_time = 0;
            await GoToPosition(State.Raised);
            current_time = RAISED_TIME;
        }
        private async Task onEnterLevelOne()
        {
            current_time = 0;
            await GoToPosition(State.Level_One);
            current_time = LEVELONE_TIME;
        }
        private async Task onEnterLevelTwo()
        {
            current_time = 0;
            await GoToPosition(State.Level_Two);
            current_time = LEVELTWO_TIME;
        }
        private async Task onEnterLevelThree()
        {
            current_time = 0;
            await GoToPosition(State.Level_Three);
            current_time = LEVELTHREE_TIME;
        }
        private async Task onEnterReparo()
        {
            current_time = 0;
            int delay = 250;
            for (int i = 0; i < 3; i++) {
                await brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.D, 30, (uint)delay, false);
                await Task.Delay(delay);
                await brick.DirectCommand.TurnMotorAtPowerForTimeAsync(OutputPort.D, -30, (uint)delay, false);
                await Task.Delay(delay);
            }
            current_time = REPARO_TIME;
        }
        private async Task onEnterObtainit()
        {
            current_time = 0;
            await RunSequence(ObtainitPregrabSequence);
            await RunSequence(ObtainitPostgrabSequence);
            await machine.FireAsync(Trigger.Obtainit_Complete);
        }

        private async Task RunSequence(Dictionary<OutputPort, int>[] sequence)
        {
            foreach (var step in sequence)
            {
                await GoToPosition(step);
                await Task.Delay(step.ContainsKey(OutputPort.A) ? 2000 : 0); // Wait longer for shoulder move
            }
        }

        public async Task ManualStartGrab(State Level)
        {
            manual_override = true;
            await GoToPosition(Level);
            await RunSequence(ObtainitPregrabSequence);
        }
        public async Task ManualFinishGrab()
        {
            await RunSequence(ObtainitPostgrabSequence);
            await GoToPosition(State.Waiting);
            manual_override = true;
        }
    }
}
