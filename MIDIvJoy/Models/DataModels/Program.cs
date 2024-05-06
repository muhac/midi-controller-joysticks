namespace MIDIvJoy.Models.DataModels;

public class Program
{
    private Program()
    {
        Instance = this;
    }

    public static Program Instance { get; private set; } = new();

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