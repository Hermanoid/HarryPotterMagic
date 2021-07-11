using Stateless;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public enum States
    {
        Waiting,
        Raised,
        Raised_Failed,
        LevelOne,
        LevelTwo,
        LevelThree,
        LevelOne_Failed,
        LevelTwo_Failed,
        LevelThree_Failed
    };
    public enum Trigger
    {
        Wingardium_Leviosa = Spell.Wingardium_Leviosa,
        Smallo_Munchio = Spell.Smallo_Munchio,
        Funsizarth = Spell.Funsizarth,
        Bigcandius = Spell.Bigcandius,
        Obtainit = Spell.Obtainit,
        Dud = Spell.Dud,
        Reparo = Spell.Reparo,
        Fail_Timeout = -1
    }
    public partial class GameController
    {
        public StateMachine<States, Trigger> InitMachine()
        {
            var machine = new StateMachine<States, Trigger>(States.Waiting);


            machine.Configure(States.Waiting)
                .Permit(Trigger.Wingardium_Leviosa, States.Raised);

            machine.Configure(States.Raised)
                .Permit(Trigger.Smallo_Munchio, States.LevelOne)
                .Permit(Trigger.Dud, States.Raised_Failed);

            machine.Configure(States.Raised_Failed)
                .Permit(Trigger.Reparo, States.Raised)
                .Permit(Trigger.Fail_Timeout, States.Waiting);

            machine.Configure(States.LevelOne)
                .Permit(Trigger.Funsizarth, States.LevelTwo)
                .Permit(Trigger.Dud, States.LevelOne_Failed)
                .Permit(Trigger.Obtainit, States.Waiting);

            machine.Configure(States.LevelOne_Failed)
                .Permit(Trigger.Reparo, States.LevelOne)
                .Permit(Trigger.Fail_Timeout, States.Waiting);

            machine.Configure(States.LevelTwo)
                .Permit(Trigger.Bigcandius, States.LevelThree)
                .Permit(Trigger.Dud, States.LevelTwo_Failed)
                .Permit(Trigger.Obtainit, States.Waiting);

            machine.Configure(States.LevelTwo_Failed)
                .Permit(Trigger.Reparo, States.LevelTwo)
                .Permit(Trigger.Fail_Timeout, States.Waiting);

            machine.Configure(States.LevelThree)
                .Permit(Trigger.Dud, States.LevelThree_Failed)
                .Permit(Trigger.Obtainit, States.Waiting);

            machine.Configure(States.LevelThree_Failed)
                .Permit(Trigger.Reparo, States.LevelThree)
                .Permit(Trigger.Fail_Timeout, States.Waiting);

            return machine;
        }
    }
}
