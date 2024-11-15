namespace Programs.Components
{
    public readonly struct IsProgram
    {
        public readonly StartProgramFunction start;
        public readonly UpdateProgramFunction update;
        public readonly FinishProgramFunction finish;
        public readonly ushort typeSize;

        public IsProgram(StartProgramFunction start, UpdateProgramFunction update, FinishProgramFunction finish, ushort typeSize)
        {
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.typeSize = typeSize;
        }
    }
}