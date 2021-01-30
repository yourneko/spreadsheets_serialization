using System;
using SheetsIO;
using UnityEngine;

namespace Example
{
    [IOMeta("SimpleObject"), Serializable]
    public class SimpleData
    {
        [IOField, SerializeField] string firstStr;
        [IOField, SerializeField] string secondStr;
        [IOField(IsOptional = true), SerializeField] string thirdStr;
    }
}
