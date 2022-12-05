using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System.Linq;
using System.Reflection;

namespace GodotAnalysers.Test
{
    [TestFixture]
    public class DependencyInjectorGeneratorTest : System.Object
    {
        [Test]
        public void SimpleClassTest()
        {
            DoTest(@"
[InjectableAttribute(false)]
public class C { }
",
@"using System;
namespace DependencyInjection
{
    public static class DependencyInjector
    {
        public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();
        public static DependencyInjectorContext GetNewContext()
        {
            return new DependencyInjectorContext(GlobalContext);
        }
    }
    public class DependencyInjectorContext
    {
        public readonly C c;
        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)
        {
            c = copyContext?.c ?? new C();
        }
    }
}");
        }

        [Test]
        public void MultipleConstructors()
        {
            DoTest(@"
[InjectableAttribute(false)]
public class B { }

[InjectableAttribute(false)]
public class C {
    [InjectableConstructorAttribute()]
    public C(B b){}
    public C(){}
}
",
@"using System;
namespace DependencyInjection
{
    public static class DependencyInjector
    {
        public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();
        public static DependencyInjectorContext GetNewContext()
        {
            return new DependencyInjectorContext(GlobalContext);
        }
    }
    public class DependencyInjectorContext
    {
        public readonly B b;
        public readonly C c;
        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)
        {
            b = copyContext?.b ?? new B();
            c = copyContext?.c ?? new C(b);
        }
    }
}");
        }

        [Test]
        public void ClassWithNamespaceTest()
        {
            DoTest(@"
namespace Test{
    [InjectableAttribute(false)]
    public class C { }
}
",
@"using System;
namespace DependencyInjection
{
    public static class DependencyInjector
    {
        public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();
        public static DependencyInjectorContext GetNewContext()
        {
            return new DependencyInjectorContext(GlobalContext);
        }
    }
    public class DependencyInjectorContext
    {
        public readonly Test.C c;
        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)
        {
            c = copyContext?.c ?? new Test.C();
        }
    }
}");
        }

        [Test]
        public void OnDemandDependency()
        {
            DoTest(@"
[InjectableAttribute(true)]
public class A {
    public A(B b, C c){}
}

[InjectableAttribute(true)]
public class B { }

[InjectableAttribute(false)]
public class C {
    public C(B b){}
}
",
@"using System;
namespace DependencyInjection
{
    public static class DependencyInjector
    {
        public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();
        public static DependencyInjectorContext GetNewContext()
        {
            return new DependencyInjectorContext(GlobalContext);
        }
    }
    public class DependencyInjectorContext
    {
        public readonly B b;
        public readonly C c;
        public readonly A a;
        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)
        {
            b = new B();
            c = copyContext?.c ?? new C(b);
            a = new A(b, c);
        }
    }
}");
        }

        [Test]
        public void ComplexDependenciesTest()
        {
            DoTest(@"
[InjectableAttribute(false)]
public class A {
    public A(B b, C c){}
}

[InjectableAttribute(false)]
public class B { 
    public B(D d){}

}

[InjectableAttribute(false)]
public class C {
    public C(B b, D d){}

}

[InjectableAttribute(false)]
public class D {
}

",
@"using System;
namespace DependencyInjection
{
    public static class DependencyInjector
    {
        public static DependencyInjectorContext GlobalContext = new DependencyInjectorContext();
        public static DependencyInjectorContext GetNewContext()
        {
            return new DependencyInjectorContext(GlobalContext);
        }
    }
    public class DependencyInjectorContext
    {
        public readonly D d;
        public readonly B b;
        public readonly C c;
        public readonly A a;

        public DependencyInjectorContext(DependencyInjectorContext copyContext = null)
        {
            d = copyContext?.d ?? new D();
            b = copyContext?.b ?? new B(d);
            c = copyContext?.c ?? new C(b, d);
            a = copyContext?.a ?? new A(b, c);
        }
    }
}");
        }

        public void DoTest(string sourceText, string resultText)
        {
            var expectedResult = CreateCompilation(resultText).SyntaxTrees.First().GetRoot().NormalizeWhitespace().ToFullString();

            Compilation compilation = CreateCompilation(sourceText);
            var syntaxTree = compilation.SyntaxTrees.First();
            compilation = compilation
                .RemoveSyntaxTrees(syntaxTree)
                .AddSyntaxTrees(compilation.SyntaxTrees.First().WithFilePath(Assembly.GetExecutingAssembly().Location));

            var generator = new DependencyInjectorGenerator();
            GeneratorDriver driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            System.Console.WriteLine(diagnostics.FirstOrDefault()?.ToString());

            // We can now assert things about the resulting compilation:
            Assert.IsTrue(diagnostics.IsEmpty); // there were no diagnostics created by the generators
            Assert.AreEqual(2, outputCompilation.SyntaxTrees.Count()); // we have two syntax trees, the original 'user' provided one, and the one added by the generator
            // System.Console.WriteLine(string.Join("\n", outputCompilation.GetDiagnostics()));
            // Assert.IsTrue(outputCompilation.GetDiagnostics().IsEmpty); // verify the compilation with the added source has no diagnostics

            // Or we can look at the results directly:
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            // The runResult contains the combined results of all generators passed to the driver
            Assert.AreEqual(1, runResult.GeneratedTrees.Length);
            Assert.IsTrue(runResult.Diagnostics.IsEmpty);

            // Or you can access the individual results on a by-generator basis
            GeneratorRunResult generatorResult = runResult.Results[0];
            Assert.IsTrue(generatorResult.Generator == generator);
            Assert.IsTrue(generatorResult.Diagnostics.IsEmpty);
            Assert.AreEqual(1, generatorResult.GeneratedSources.Length);
            Assert.IsTrue(generatorResult.Exception is null);

            Assert.AreEqual(expectedResult, generatorResult.GeneratedSources[0].SyntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
        }

        private static Compilation CreateCompilation(string source)
          => CSharpCompilation.Create("compilation",
              new[] { CSharpSyntaxTree.ParseText(source) },
              new[] { MetadataReference.CreateFromFile(typeof(SceneReferenceAttribute).GetTypeInfo().Assembly.Location) },
              new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
