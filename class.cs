using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ActivityLog
{
    public string Username { get; set; }
    public DateTime Timestamp { get; set; }
    public string Action { get; set; }
    public bool IsSuccess { get; set; }
    public string Details { get; set; }

    public ActivityLog(string username, string action, bool isSuccess, string details = "")
    {
        Username = username;
        Timestamp = DateTime.Now;
        Action = action;
        IsSuccess = isSuccess;
        Details = details;
    }

    public override string ToString()
    {
        return $"[{Timestamp}] User: {Username}, Action: {Action}, Success: {IsSuccess}, Details: {Details}";
    }
}

public class User
{
    public string Username { get; set; }
    public string Password { get; set; }
    public bool IsAdmin { get; set; }

    public User(string username, string password, bool isAdmin = false)
    {
        Username = username;
        Password = password;
        IsAdmin = isAdmin;
    }

    public static List<User> LoadUsers()
    {
        List<User> users = new List<User>();
        string filePath = "users.txt";
        
        if (File.Exists(filePath))
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    users.Add(new User(parts[0], parts[1], bool.Parse(parts[2])));
                }
            }
        }
        else
        {
            // İlk çalıştırmada admin kullanıcısı oluştur
            users.Add(new User("admin", "admin123", true));
            SaveUsers(users);
        }
        
        return users;
    }

    public static void SaveUsers(List<User> users)
    {
        string filePath = "users.txt";
        List<string> lines = users.Select(u => $"{u.Username},{u.Password},{u.IsAdmin}").ToList();
        File.WriteAllLines(filePath, lines);
    }
} 