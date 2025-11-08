using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    internal class ClassConnectionDataBase
    {
        string connection = "Server=localhost; Port=5432; Database=rgr; User Id = postgres; Password=43898362Dd+-;";

        public void initializationOfDataBase(string qwery)
        {
            try
            {
                NpgsqlConnection sqlConnection = new NpgsqlConnection(connection);
                sqlConnection.Open();


                using (var cmd = new NpgsqlCommand(qwery, sqlConnection))
                {
                    cmd.ExecuteNonQuery();
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

    }
}
