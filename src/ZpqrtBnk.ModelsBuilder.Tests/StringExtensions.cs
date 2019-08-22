namespace ZpqrtBnk.ModelsBuilder.Tests
{
    public static class StringExtensions
    {
        public static string ClearLf(this string s)
        {
            return s.Replace("\r", "");
        }
    }
}
