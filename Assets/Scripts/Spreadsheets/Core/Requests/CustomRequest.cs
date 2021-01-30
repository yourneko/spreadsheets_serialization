using System;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public abstract class CustomRequest
    {
        public bool Locked { get; private set; }

        public virtual string Description => $"Request of type {GetType ().Name}";        
        public string SpreadsheetID { get; protected set; }

        protected void Lock() => Locked = true;

        public virtual void Terminate() { throw new NotImplementedException ();}

    }
}