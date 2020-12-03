using System;

namespace Mimimi.SpreadsheetsSerialization
{
    public class DisposableObject : IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                DisposeInternalEntities();

            disposed = true;
        }

        // Free any unmanaged objects here.
        protected virtual void DisposeInternalEntities() {}

        // Overriding a destructor
        ~DisposableObject()
        {
            Dispose(false);
        }
    }
}