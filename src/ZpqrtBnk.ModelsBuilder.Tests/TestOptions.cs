namespace ZpqrtBnk.ModelsBuilder.Tests
{
    static class TestOptions
    {
        // mssql
        //public const string ConnectionString = @"server=localhost\sqlexpress;database=dev_umbraco6;user id=sa;password=sa";
        //public const string DatabaseProvider = "System.Data.SqlClient";

        // mysql
        public const string ConnectionString = @"server=localhost;database=eurovia_w310;user id=root;password=root;default command timeout=120;";
        public const string DatabaseProvider = "MySql.Data.MySqlClient";
    }
}
