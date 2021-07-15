using Stateless;

namespace Microsoft.Samples.Kinect.InfraredBasics
{
    public enum State
    {
        Startup,
        Waiting,
        Raised,
        Raised_Failed,
        Level_One,
        Level_Two,
        Level_Three,
        Level_One_Failed,
        Level_Two_Failed,
        Level_Three_Failed
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
        Times_Up = -1,
        Startup_Complete = -2,
    }
    public partial class GameController
    {
        public StateMachine<State, Trigger> InitMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Startup);

            machine.Configure(State.Startup)
                .Permit(Trigger.Startup_Complete, State.Waiting);

            machine.Configure(State.Waiting)
                .OnEntry(onEnterWaiting)
                .Permit(Trigger.Wingardium_Leviosa, State.Raised)
                .Ignore(Trigger.Times_Up);

            machine.Configure(State.Raised)
                .OnEntry(onEnterRaised)
                .Permit(Trigger.Smallo_Munchio, State.Level_One)
                .Permit(Trigger.Dud, State.Raised_Failed)
                .Permit(Trigger.Times_Up, State.Raised_Failed);

            machine.Configure(State.Raised_Failed)
                .OnEntry(onEnterReparo)
                .Permit(Trigger.Reparo, State.Raised)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_One)
                .OnEntry(onEnterLevelOne)
                .Permit(Trigger.Funsizarth, State.Level_Two)
                .Permit(Trigger.Dud, State.Level_One_Failed)
                .Permit(Trigger.Times_Up, State.Level_One_Failed)
                .Permit(Trigger.Obtainit, State.Waiting);

            machine.Configure(State.Level_One_Failed)
                .OnEntry(onEnterReparo)
                .Permit(Trigger.Reparo, State.Level_One)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_Two)
                .OnEntry(onEnterLevelTwo)
                .Permit(Trigger.Bigcandius, State.Level_Three)
                .Permit(Trigger.Dud, State.Level_Two_Failed)
                .Permit(Trigger.Times_Up, State.Level_Two_Failed)
                .Permit(Trigger.Obtainit, State.Waiting);

            machine.Configure(State.Level_Two_Failed)
                .OnEntry(onEnterReparo)
                .Permit(Trigger.Reparo, State.Level_Two)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_Three)
                .OnEntry(onEnterLevelThree)
                .Permit(Trigger.Dud, State.Level_Three_Failed)
                .Permit(Trigger.Times_Up, State.Level_Three_Failed)
                .Permit(Trigger.Obtainit, State.Waiting);

            machine.Configure(State.Level_Three_Failed)
                .OnEntry(onEnterReparo)
                .Permit(Trigger.Reparo, State.Level_Three)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.OnTransitioned((transition) =>
            {
                if(transition.Trigger == Trigger.Obtainit)
                {
                    onObtainit();
                }
            });

            return machine;
        }
    }
}
