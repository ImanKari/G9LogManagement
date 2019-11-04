namespace G9LogManagement.Enums
{
    public enum DateTimeType : byte
    {
        /// <summary>
        ///     <para>Gregorian (Julian) date time</para>
        ///     <para>Sample: 2019-11-04</para>
        /// </summary>
        Gregorian,

        /// <summary>
        ///     <para>Shamsi (Persian) date time</para>
        ///     <para>Sample: 1398-13-08</para>
        /// </summary>
        Shamsi,

        /// <summary>
        ///     <para>Started gregorian then shamsi date time</para>
        ///     <para>Sample: 2019-11-04_1398-13-08</para>
        /// </summary>
        GregorianShamsi,

        /// <summary>
        ///     <para>Started shamsi then gregorian date time</para>
        ///     <para>Sample: 1398-13-08_2019-11-04</para>
        /// </summary>
        ShamsiGregorian
    }
}