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
        Level_Three_Failed,
        Obtainit
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
        Obtainit_Complete = -3
    }
    public partial class GameController
    {
        public StateMachine<State, Trigger> InitMachine()
        {
            var machine = new StateMachine<State, Trigger>(State.Startup);

            machine.Configure(State.Startup)
                .Permit(Trigger.Startup_Complete, State.Waiting);

            machine.Configure(State.Waiting)
                .OnEntryAsync(onEnterWaiting)
                .PermitIf(Trigger.Wingardium_Leviosa, State.Raised, () => !manual_override)
                .Ignore(Trigger.Times_Up);

            machine.Configure(State.Raised)
                .OnEntryAsync(onEnterRaised)
                .Permit(Trigger.Smallo_Munchio, State.Level_One)
                .Permit(Trigger.Dud, State.Raised_Failed)
                .Permit(Trigger.Times_Up, State.Raised_Failed);

            machine.Configure(State.Raised_Failed)
                .OnEntryAsync(onEnterReparo)
                .Permit(Trigger.Reparo, State.Raised)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_One)
                .OnEntryAsync(onEnterLevelOne)
                .Permit(Trigger.Funsizarth, State.Level_Two)
                .Permit(Trigger.Dud, State.Level_One_Failed)
                .Permit(Trigger.Times_Up, State.Level_One_Failed)
                .Permit(Trigger.Obtainit, State.Obtainit);

            machine.Configure(State.Level_One_Failed)
                .OnEntryAsync(onEnterReparo)
                .Permit(Trigger.Reparo, State.Level_One)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_Two)
                .OnEntryAsync(onEnterLevelTwo)
                .Permit(Trigger.Bigcandius, State.Level_Three)
                .Permit(Trigger.Dud, State.Level_Two_Failed)
                .Permit(Trigger.Times_Up, State.Level_Two_Failed)
                .Permit(Trigger.Obtainit, State.Obtainit);

            machine.Configure(State.Level_Two_Failed)
                .OnEntryAsync(onEnterReparo)
                .Permit(Trigger.Reparo, State.Level_Two)
                .Permit(Trigger.Times_Up, State.Waiting);

            machine.Configure(State.Level_Three)
                .OnEntryAsync(onEnterLevelThree)
                .Permit(Trigger.Dud, State.Waiting)
                .Permit(Trigger.Times_Up, State.Waiting)
                .Permit(Trigger.Obtainit, State.Obtainit);

            machine.Configure(State.Obtainit)
                .OnEntryAsync(onEnterObtainit)
                .Permit(Trigger.Obtainit_Complete, State.Waiting);

            return machine;
        }
    }
}
