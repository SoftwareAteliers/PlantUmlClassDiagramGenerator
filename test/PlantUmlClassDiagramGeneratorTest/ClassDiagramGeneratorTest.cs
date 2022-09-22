using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.IO;
using PlantUmlClassDiagramGenerator.Library;

namespace PlantUmlClassDiagramGeneratorTest
{
    [TestClass]
    public class ClassDiagramGeneratorTest
    {
        [TestMethod]
        public void GenerateTestAll()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}InputClasses.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ");
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}All.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateTestPublic()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}InputClasses.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ",
                    Accessibilities.Private | Accessibilities.Internal
                                            | Accessibilities.Protected | Accessibilities.ProtectedInternal);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}Public.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateTestWithoutPrivate()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}InputClasses.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ", Accessibilities.Private);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}WithoutPrivate.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateTestGenericsTypes()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}GenericsType.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ", Accessibilities.Private | Accessibilities.Internal
                                                                                            | Accessibilities.Protected | Accessibilities.ProtectedInternal);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}GenericsType.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void NullableTestNullableTypes()
        {
            var code = File.ReadAllText(Path.Combine("testData", "NullableType.cs"));
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ", Accessibilities.Private | Accessibilities.Internal
                                                                                            | Accessibilities.Protected | Accessibilities.ProtectedInternal);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText(Path.Combine("uml", "NullableType.puml")), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateTestAtPrefixType()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}AtPrefixType.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ", Accessibilities.Private | Accessibilities.Internal
                                                                                            | Accessibilities.Protected | Accessibilities.ProtectedInternal, true);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}AtPrefixType.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenerateTestCurlyBrackets()
        {
            var code = File.ReadAllText($"testData{Path.DirectorySeparatorChar}CurlyBrackets.cs");
            var tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetRoot();

            var output = new StringBuilder();
            using (var writer = new StringWriter(output))
            {
                var gen = new ClassDiagramGenerator(writer, "    ", Accessibilities.Private | Accessibilities.Internal
                                                                                            | Accessibilities.Protected | Accessibilities.ProtectedInternal, true);
                gen.Generate(root);
            }

            var expected = ConvertNewLineCode(File.ReadAllText($"uml{Path.DirectorySeparatorChar}CurlyBrackets.puml"), Environment.NewLine);
            var actual = output.ToString();
            Console.Write(actual);
            Assert.AreEqual(expected, actual);
        }

        private static string ConvertNewLineCode(string text, string newline)
        {
            var reg = new System.Text.RegularExpressions.Regex("\r\n|\r|\n");
            return reg.Replace(text, newline);
        }
    }
}