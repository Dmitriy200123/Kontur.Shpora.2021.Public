using System;
using System.Threading;

namespace ReaderWriterLock
{
    public class RwLock : IRwLock
    {
        private int _readersCount;
        private readonly object _lockObj = new();
        public void ReadLocked(Action action)
        {
            lock (_lockObj)
            {
                _readersCount++;
            }
            action.Invoke();
            lock (_lockObj)
            {
                _readersCount--;
                if (_readersCount == 0)
                    Monitor.PulseAll(_lockObj);
            }
        }

        public void WriteLocked(Action action)
        {
            lock (_lockObj)
            {
                while (_readersCount > 0)
                    Monitor.Wait(_lockObj);
                action.Invoke();
            }
        }
    }
}