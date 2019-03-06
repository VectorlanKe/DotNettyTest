using DotNetty.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettyHttp
{
    public class AttributeMapConstant
    {
        public static readonly AttributeKey<string> HttpAttriKey = AttributeKey<string>.ValueOf("httpAttriKey");
    }
}
