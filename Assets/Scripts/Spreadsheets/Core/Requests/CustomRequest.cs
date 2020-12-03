namespace Mimimi.SpreadsheetsSerialization.Core
{
    public abstract class CustomRequest
    {
        public string SpreadsheetID { get; protected set; }

        public virtual void Terminate() { }

        public virtual string Description => $"Request of type {GetType ().Name}";
    }
}