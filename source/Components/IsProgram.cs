using Programs.Functions;
using Unmanaged;

namespace Programs.Components
{
    public readonly struct IsProgram
    {
        public readonly StartFunction start;
        public readonly UpdateFunction update;
        public readonly FinishFunction finish;
        public readonly RuntimeType type;

        public IsProgram(StartFunction start, UpdateFunction update, FinishFunction finish, RuntimeType type)
        {
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.type = type;
        }
    }
}