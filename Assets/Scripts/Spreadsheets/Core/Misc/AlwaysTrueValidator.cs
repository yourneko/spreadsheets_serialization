using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public static class AlwaysTrueValidator
    {
        public static bool ReturnTrue(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }
    }
}