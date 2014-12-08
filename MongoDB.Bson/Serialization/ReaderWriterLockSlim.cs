using System.Threading;
namespace MongoDB.Bson
{
	public class ReaderWriterLockSlim
	{
		ReaderWriterLock locker;
		public ReaderWriterLockSlim (LockRecursionPolicy policy)
		{
			locker = new ReaderWriterLock();
		}
		public void EnterReadLock()
		{
			locker.AcquireReaderLock(10);
		}
		public void ExitReadLock()
		{
			locker.ReleaseReaderLock();
		}
		public void EnterWriteLock()
		{
			locker.AcquireWriterLock(10);
		}
		public void ExitWriteLock()
		{
			locker.ReleaseReaderLock();
		}
	}
}

