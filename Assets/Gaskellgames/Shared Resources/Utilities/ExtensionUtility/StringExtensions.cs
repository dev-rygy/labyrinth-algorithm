namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class StringExtensions
    {
        /// <summary>
        /// Return a three digit int as a 'nicified' string value for the number.
        /// </summary>
        /// <param name="value"></param>
        /// <returns>0-9 will be returned as 000-009, 10-99 will be returned as 010-099</returns>
        public static string NicifyNumberAsString(int value)
        {
            // negatives
            if (value < 0) { return value.ToString(); }

            // 000-009
            if (value < 10) { return $"00{value}"; }

            // 010-099
            if (value < 100) { return $"0{value}"; }

            // 100+
            return value.ToString();
        }

    } // class end
}
