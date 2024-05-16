using LiteDB;
using Newtonsoft.Json;

namespace MIDIvJoy.Models.DataModels;

public class Program
{
    public static Program Instance { get; private set; }

    private Program()
    {
        Instance = this;
    }

    static Program()
    {
        Instance = new Program();
    }

    private static readonly ReaderWriterLockSlim Lock = new();
    private static bool _isWindowActivated;

    public bool IsWindowActivated
    {
        get
        {
            Lock.EnterReadLock();
            try
            {
                return _isWindowActivated;
            }
            finally
            {
                Lock.ExitReadLock();
            }
        }
        set
        {
            Lock.EnterWriteLock();
            try
            {
                _isWindowActivated = value;
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
    }
}

public static class Database
{
    private const string DbFile = "MIDIvJoy.db";
    private const string KvCollection = "Kv_v1";

    public static T? Get<T>(string key)
    {
        using var db = new LiteDatabase(DbFile);
        var kv = db.GetCollection<Kv>(KvCollection);
        var json = kv.FindById(key);
        return json != null ? JsonConvert.DeserializeObject<T>(json.Value) : default;
    }

    public static void Set<T>(string key, T value)
    {
        using var db = new LiteDatabase(DbFile);
        var kv = db.GetCollection<Kv>(KvCollection);
        var json = JsonConvert.SerializeObject(value);
        kv.Upsert(key, new Kv(key, json));
    }

    private class Kv(string key, string value)
    {
        public string Key { get; set; } = key;
        public string Value { get; set; } = value;
    }
}