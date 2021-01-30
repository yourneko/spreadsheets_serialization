namespace Mimimi.SpreadsheetsSerialization.Core
{
    public abstract class CustomBatchRequest : CustomRequest
    {
        internal SerializationService Service;

        protected void EnqueueRequest(CustomRequest _request)
        {
            Service.Enqueue (_request);
        }
        
        public virtual void Enqueue()
        {
            if (Locked)
                return;

            Lock ();
            EnqueueRequest (this);
        }
    }
}

