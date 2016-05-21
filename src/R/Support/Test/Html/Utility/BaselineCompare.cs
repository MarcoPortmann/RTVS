﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Html.Core.Test.Utility {
    [ExcludeFromCodeCoverage]
    public static class BaselineCompare {
        public static string CompareStrings(string expected, string actual) {
            var result = new StringBuilder(); ;

            int length = Math.Min(expected.Length, actual.Length);
            for (int i = 0; i < length; i++) {
                if (expected[i] != actual[i]) {
                    result.AppendFormat("Position: {0}: expected: '{1}', actual '{2}'\r\n", i, expected[i], actual[i]);
                    if (i > 6 && i < length - 6) {
                        result.AppendFormat("Context: {0} -> {1}", expected.Substring(i - 6, 12), actual.Substring(i - 6, 12));
                    }
                    break;
                }

            }

            if (expected.Length != actual.Length)
                result.AppendFormat("\r\nLength different. Expected: '{0}' , actual '{1}'", expected.Length, actual.Length);

            return result.ToString();
        }

        static public int CompareLines(string expected, string actual, out string baseLine, out string newLine) {
            var newReader = new StringReader(actual);
            var baseReader = new StringReader(expected);

            int lineNum = 1;
            for (lineNum = 1; ; lineNum++) {
                baseLine = baseReader.ReadLine();
                newLine = newReader.ReadLine();

                if (baseLine == null || newLine == null)
                    break;

                if (String.CompareOrdinal(baseLine, newLine) != 0)
                    return lineNum;
            }

            if (baseLine == null && newLine == null) {
                baseLine = String.Empty;
                newLine = String.Empty;

                return 0;
            }

            return lineNum;
        }

        public static void CompareFiles(string baselineFile, string actual, bool regenerateBaseline) {
            StreamWriter sw = null;
            StreamReader sr = null;

            try {
                if (regenerateBaseline) {
                    if (File.Exists(baselineFile))
                        File.SetAttributes(baselineFile, FileAttributes.Normal);

                    sw = new StreamWriter(baselineFile);
                    sw.Write(actual);
                } else {
                    sr = new StreamReader(baselineFile);
                    string expected = sr.ReadToEnd();

                    string baseLine, newLine;
                    int line = CompareLines(expected, actual, out baseLine, out newLine);

                    Assert.Equal(0, line);
                }
            } finally {
                if (sr != null) {
                    sr.Close();
                    sr.Dispose();
                }

                if (sw != null) {
                    sw.Close();
                    sw.Dispose();
                }
            }
        }
    }
}
