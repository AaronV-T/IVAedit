using IVAE.RedditBot.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IVAE.RedditBot
{
  public class DatabaseAccessor
  {
    private readonly string connectionString;

    public DatabaseAccessor(string connectionString)
    {
      this.connectionString = connectionString;
    }

    public void AddFallbackReplyLink(string linkFullname, DateTime linkDatetime)
    {
      using (IDbConnection dbConnection = this.GetOpenDbConnection())
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        dbCommand.CommandText = "INSERT INTO FallbackRepliesLinks (link_fullname, link_datetime) VALUES (@link_fullname, @link_datetime)";

        dbCommand.AddParameter("@link_fullname", linkFullname);
        dbCommand.AddParameter("@link_datetime", linkDatetime);

        dbCommand.ExecuteNonQuery();
      }
    }

    public void EnsureDatabaseIsUpToDate()
    {
      string[] resourceNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();

      using (IDbConnection dbConnection = this.GetOpenDbConnection())
      {
        int? currentSchemaVersion = GetSchemaVersion(dbConnection);
        if (currentSchemaVersion == null)
          currentSchemaVersion = 0;

        Func<string> GetNextUpdateFileName = () => $"IVAE.RedditBot.Resources.SQL.DBUpdate_{currentSchemaVersion.Value + 1}.sql";

        using (IDbTransaction dbTransaction = dbConnection.BeginTransaction())
        {
          while (resourceNames.Contains(GetNextUpdateFileName()))
          {
            // There is an SQL script with higher version than our current schema version: run that script and update our schema version.
            List<string> sqlStrings = GetSqlStringsFromResourceFile(GetNextUpdateFileName());
            foreach (string sql in sqlStrings)
            {
              using (IDbCommand dbCommand = dbConnection.CreateCommand())
              {
                dbCommand.Transaction = dbTransaction;
                dbCommand.CommandText = sql;
                dbCommand.ExecuteNonQuery();
              }
            }

            currentSchemaVersion++;
            SetSchemaVersion(dbConnection, dbTransaction, currentSchemaVersion.Value);
          }

          dbTransaction.Commit();
        }
      }
    }

    public Tuple<string, DateTime> GetMostRecentFallbackRepliesLink()
    {
      using (IDbConnection dbConnection = this.GetOpenDbConnection())
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        dbCommand.CommandText = "SELECT link_fullname, link_datetime FROM FallbackRepliesLinks ORDER BY link_datetime";

        using (IDataReader dataReader = dbCommand.ExecuteReader())
        {
          if (dataReader.Read())
          {
            int col = 0;
            string fullName = dataReader.GetString(col++);
            DateTime datetime = dataReader.GetDateTime(col++);

            return new Tuple<string, DateTime>(fullName, datetime);
          }

          return null;
        }
      }
    }

    public List<UploadLog> GetAllUploadLogs()
    {
      using (IDbConnection dbConnection = this.GetOpenDbConnection())
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        dbCommand.CommandText = "SELECT deleted, delete_datetime, delete_reason, delete_key, id, post_fullname, reply_fullname, requestor_username, upload_datetime, upload_destination FROM UploadLogs";

        using (IDataReader dataReader = dbCommand.ExecuteReader())
        {
          List<UploadLog> uploadLogs = new List<UploadLog>();

          while (dataReader.Read())
          {
            int col = 0;
            UploadLog uploadLog = new UploadLog();
            uploadLog.Deleted = dataReader.GetBoolean(col++);
            if (!dataReader.IsDBNull(col++)) uploadLog.DeleteDatetime = dataReader.GetDateTime(col - 1);
            if (!dataReader.IsDBNull(col++)) uploadLog.DeleteReason = dataReader.GetString(col - 1);
            uploadLog.DeleteKey = dataReader.GetString(col++);
            uploadLog.Id = dataReader.GetInt32(col++);
            uploadLog.PostFullname= dataReader.GetString(col++);
            uploadLog.ReplyFullname = dataReader.GetString(col++);
            uploadLog.RequestorUsername = dataReader.GetString(col++);
            uploadLog.UploadDatetime = dataReader.GetDateTime(col++);
            uploadLog.UploadDestination = dataReader.GetString(col++);

            uploadLogs.Add(uploadLog);
          }

          return uploadLogs;
        }
      }
    }

    private static int? GetSchemaVersion(IDbConnection dbConnection)
    {
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        dbCommand.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'DBSettings'";
        if ((int)dbCommand.ExecuteScalar() == 0)
          return null;

        dbCommand.CommandText = "SELECT SettingValue From DBSettings WHERE SettingKey = 'SchemaVersion'";
        return int.Parse((string)dbCommand.ExecuteScalar());
      }
    }

    public void SaveUploadLog(UploadLog uploadLog)
    {
      using (IDbConnection dbConnection = this.GetOpenDbConnection())
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        bool isInsertCommand = uploadLog.Id == 0;

        if (isInsertCommand)
          dbCommand.CommandText = "INSERT INTO UploadLogs (deleted, delete_datetime, delete_reason, delete_key, post_fullname, reply_fullname, requestor_username, upload_datetime, upload_destination) OUTPUT inserted.id VALUES (@deleted, @delete_datetime, @delete_reason, @delete_key, @post_fullname, @reply_fullname, @requestor_username, @upload_datetime, @upload_destination)";
        else
          dbCommand.CommandText = "UPDATE UploadLogs SET deleted = @deleted, delete_datetime = @delete_datetime, delete_reason = @delete_reason, delete_key = @delete_key, post_fullname = @post_fullname, reply_fullname = @reply_fullname, requestor_username = @requestor_username, upload_datetime = @upload_datetime, upload_destination = @upload_destination WHERE id = @id";

        dbCommand.AddParameter("@deleted", uploadLog.Deleted);
        dbCommand.AddParameter("@delete_datetime", (object)uploadLog.DeleteDatetime ?? DBNull.Value);
        dbCommand.AddParameter("@delete_reason", (object)uploadLog.DeleteReason ?? DBNull.Value);
        dbCommand.AddParameter("@delete_key", uploadLog.DeleteKey);
        if (!isInsertCommand) dbCommand.AddParameter("@id", uploadLog.Id);
        dbCommand.AddParameter("@post_fullname", uploadLog.PostFullname);
        dbCommand.AddParameter("@reply_fullname", uploadLog.ReplyFullname);
        dbCommand.AddParameter("@requestor_username", uploadLog.RequestorUsername);
        dbCommand.AddParameter("@upload_datetime", uploadLog.UploadDatetime);
        dbCommand.AddParameter("@upload_destination", uploadLog.UploadDestination);

        if (isInsertCommand)
          uploadLog.Id = (int)dbCommand.ExecuteScalar();
        else
          dbCommand.ExecuteNonQuery();
      }
    }

    private static void SetSchemaVersion(IDbConnection dbConnection, IDbTransaction dbTransaction, int version)
    {
      using (IDbCommand dbCommand = dbConnection.CreateCommand())
      {
        dbCommand.Transaction = dbTransaction;

        dbCommand.CommandText = "UPDATE DBSettings SET SettingValue = @version WHERE SettingKey = 'SchemaVersion'";
        dbCommand.AddParameter("@version", version);

        dbCommand.ExecuteNonQuery();
      }
    }

    private static List<string> GetSqlStringsFromResourceFile(string resourceName)
    {
      using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
      {
        using (StreamReader streamReader = new StreamReader(stream))
        {
          string sqlFile = streamReader.ReadToEnd();
          return sqlFile.Split(new string[] { "GO" }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
      }
    }

    private IDbConnection GetOpenDbConnection()
    {
      IDbConnection dbConnection = new SqlConnection(connectionString);
      dbConnection.Open();

      return dbConnection;
    }
  }
}
