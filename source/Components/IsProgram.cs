using Unmanaged;

namespace Programs.Components
{
    public readonly struct IsProgram
    {
        public readonly StartProgramFunction start;
        public readonly UpdateProgramFunction update;
        public readonly FinishProgramFunction finish;
        public readonly RuntimeType type;

        public IsProgram(StartProgramFunction start, UpdateProgramFunction update, FinishProgramFunction finish, RuntimeType type)
        {
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.type = type;
        }
    }
}