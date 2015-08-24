﻿using Microsoft.Languages.Core.Test.Utility;
using Microsoft.R.Core.Test.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.R.Core.Test.Parser
{
    [TestClass]
    public class ParseKnownConstantsTest : UnitTestBase
    {
        [TestMethod]
        public void ParseKnownContstantsTest1()
        {
            string expected =
@"GlobalScope  [Global]
    ExpressionStatement  [NULL + NA]
        Expression  [NULL + NA]
            TokenOperator  [+ [5...6]]
                NullValue  [NULL [0...4]]
                TokenNode  [+ [5...6]]
                MissingValue  [NA [7...9]]
";

            ParserTest.VerifyParse(expected, "NULL + NA");
        }
    }
}