using System;
using System.Threading;

namespace ReaderWriterLock
{
    public class RwLock : IRwLock
    {
        private int _writersCount;
        private int _readersCount;
        private readonly object _lockObj = new();
        public void ReadLocked(Action action)
        {
            lock (_lockObj)
            {
                while (_writersCount > 0)
                    Monitor.Wait(_lockObj);
                _readersCount++;
            }
            action.Invoke();
            lock (_lockObj)
            {
                _readersCount--;
                Monitor.Pulse(_lockObj);
            }
        }

        public void WriteLocked(Action action)
        {
            lock (_lockObj)
            {
                while (_writersCount > 0 || _readersCount > 0)
                    Monitor.Wait(_lockObj);
                _writersCount++;
                action.Invoke();
                _writersCount--;
                Monitor.PulseAll(_lockObj);
            }
        }
    }
}