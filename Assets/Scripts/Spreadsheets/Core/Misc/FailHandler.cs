using System;
using UnityEngine;
using System.Threading.Tasks;
using Google.Apis.Http;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class FailHandler : IHttpUnsuccessfulResponseHandler
    {
        public event Action OnFailHandled;

        public Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            if (!args.Response.IsSuccessStatusCode)
            {
                Debug.LogError ("Request failed. Resending is not implemented, request cancelled.");
                OnFailHandled?.Invoke ();
            }
            return new Task<bool>(() => false); // no resending. yet
        }
    }
}