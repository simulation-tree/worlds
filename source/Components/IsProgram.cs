namespace Programs.Components
{
    /// <summary>
    /// Stores functions to start, update, and finish a program.
    /// </summary>
    public readonly struct IsProgram
    {
        /// <summary>
        /// Starts the program.
        /// </summary>
        public readonly StartProgramFunction start;

        /// <summary>
        /// Updates the program.
        /// </summary>
        public readonly UpdateProgramFunction update;

        /// <summary>
        /// Finishes the program.
        /// </summary>
        public readonly FinishProgramFunction finish;

        /// <summary>
        /// Size of the type that the program operates on.
        /// </summary>
        public readonly ushort typeSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="IsProgram"/> struct.
        /// </summary>
        public IsProgram(StartProgramFunction start, UpdateProgramFunction update, FinishProgramFunction finish, ushort typeSize)
        {
            this.start = start;
            this.update = update;
            this.finish = finish;
            this.typeSize = typeSize;
        }
    }
}