namespace G9LogManagement.Enums
{
    /// <summary>
    ///     Specify reason close type
    /// </summary>
    internal enum ReasonCloseType
    {
        /// <summary>
        ///     File is open or force close app and can't update reason type
        /// </summary>
        OpenOrForceClose,

        /// <summary>
        ///     Reason close file is Unknown
        /// </summary>
        Unknown,

        /// <summary>
        ///     Reason close file is FileSize
        /// </summary>
        FileSize,

        /// <summary>
        ///     Reason close file is Restart
        /// </summary>
        Restart,

        /// <summary>
        ///     Reason close file is ChangeDay (start new day)
        /// </summary>
        ChangeDay,

        /// <summary>
        ///     Reason close file is ExitApp (When application close)
        /// </summary>
        ExitApp
    }
}