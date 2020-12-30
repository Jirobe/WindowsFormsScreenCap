using System.Data.SQLite;
using WindowsFormsScreenCap.Services.Entities;

namespace WindowsFormsScreenCap.Services
{
    class SqliteService
    {
        public static SqliteService Current { get; private set; } = new SqliteService();

        private SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder { DataSource = "WindowsFormsScreenCap.db" };

        private SQLiteConnection connection;

        private SqliteService()
        {
            connection = new SQLiteConnection(builder.ToString());

            connection.Open();
            using (var cmd = new SQLiteCommand(connection))
            {
                //テーブル作成
                cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS PokeColorsData(" +
                "FileName  TEXT," +
                "Black     INTEGER," +
                "White     INTEGER," +
                "Gray      INTEGER," +
                "Red       INTEGER," +
                "Blue      INTEGER," +
                "Green     INTEGER," +
                "Yellow    INTEGER," +
                "LightBlue INTEGER," +
                "Purple    INTEGER," +
                "CNT       INTEGER" +
                ")";
                cmd.ExecuteNonQuery();
            }
            connection.Close();

        }

        public int Insert(ColorsEntity entity)
        {
            int result = -1;

            connection.Open();
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "INSERT INTO PokeColorsData(FileName, Black, White, Gray, Red, Blue, Green, Yellow, LightBlue, Purple) VALUES(" +
                $"\"{entity.FileName}\",{entity.Black},{entity.White},{entity.Gray},{entity.Red},{entity.Blue},{entity.Green},{entity.Yellow},{entity.LightBlue},{entity.Purple})";
                result = cmd.ExecuteNonQuery();
            }
            connection.Close();

            return result;
        }

        public (string name, int count) Select(ColorsEntity entity, decimal thresholdValue)
        {
            string name = "???";
            int count = 0;

            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = "SELECT FileName FROM PokeColorsData " +
                $"where Black     BETWEEN {entity.Black - thresholdValue} AND {entity.Black + thresholdValue} " +
                $"and   White     BETWEEN {entity.White - thresholdValue} AND {entity.White + thresholdValue} " +
                $"and   Gray      BETWEEN {entity.Gray - thresholdValue} AND {entity.Gray + thresholdValue} " +
                $"and   Red       BETWEEN {entity.Red - thresholdValue} AND {entity.Red + thresholdValue} " +
                $"and   Blue      BETWEEN {entity.Blue - thresholdValue} AND {entity.Blue + thresholdValue} " +
                $"and   Green     BETWEEN {entity.Green - thresholdValue} AND {entity.Green + thresholdValue} " +
                $"and   Yellow    BETWEEN {entity.Yellow - thresholdValue} AND {entity.Yellow + thresholdValue} " +
                $"and   LightBlue BETWEEN {entity.LightBlue - thresholdValue} AND {entity.LightBlue + thresholdValue} " +
                $"and   Purple    BETWEEN {entity.Purple - thresholdValue} AND {entity.Purple + thresholdValue} ";

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        name = reader["FileName"].ToString().Split('-')[0];
                    }
                }

                cmd.CommandText = $"SELECT SUM(CNT) AS CNT FROM PokeColorsData where FileName LIKE \"{name}-%\" ";

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        int.TryParse(reader["CNT"].ToString(), out count);
                    }
                }
            }

            return (name, count);
        }

        public void CountUp(string name)
        {
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = $"SELECT FileName, CNT FROM PokeColorsData where FileName LIKE \"{name}-%\" ";

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string fileName = reader["FileName"].ToString();
                        int count = 0;
                        int.TryParse(reader["CNT"].ToString(), out count);

                        using (var updateCmd = new SQLiteCommand(connection))
                        {
                            //カウントアップ
                            updateCmd.CommandText = $"UPDATE PokeColorsData SET CNT = {++count} WHERE FileName = \"{fileName}\"";
                            updateCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// CNTをすべて0にする。レコードは残す
        /// </summary>
        public void Reset()
        {
            connection.Open();
            using (var cmd = new SQLiteCommand(connection))
            {
                cmd.CommandText = $"UPDATE PokeColorsData SET CNT = 0 ";
                cmd.ExecuteNonQuery();
                
            }
            connection.Close();
        }


        public void Open() => connection.Open();

        public void Close() => connection.Close();
    }
}