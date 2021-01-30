using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    static class AlwaysTrueValidator
    {
        public static bool ReturnTrue(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }
    }
}