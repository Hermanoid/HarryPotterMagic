using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lego.Ev3.Desktop;
using Lego.Ev3.Core;
using Stateless;
using Windows.UI.Xaml;

namespace Microsoft.Samples.Kinect.InfraredBasics
{

    public partial class GameController
    {
        public const string COM_PORT = "COM4";
        public readonly BluetoothController bluetoothController;
        public EventHandler<object> OnTimerTick;
        public Brick brick;
        public StateMachine<States, Trigger> machine;
        public int current_time = 0;
        public GameController()
        {
            bluetoothController = new BluetoothController();
            brick = new Brick(new BluetoothCommunication(COM_PORT), true);
            machine = InitMachine();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += OnTimerTick;
            OnTimerTick += dispatcherTimer_Tick;
            timer.Start();
        }

        public void Initialize()
        {
            bluetoothController.Initialize();
        }

        public async Task TriggerSpell(Spell spell)
        {
            await bluetoothController.TriggerSpell(spell); // Trigger bluetooth effects if they're available
            if (machine.CanFire((Trigger)spell)) // The trigger enum is a subset of Spell, so we can cast
            {
                machine.Fire((Trigger)spell);
            }
        }
        private void dispatcherTimer_Tick(object sender, object e)
        {
            if (current_time <= 0)
            {
                current_time = 0;
            }
            else
            {
                current_time--;
            }
        }
    }
}
