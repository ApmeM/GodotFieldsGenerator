using NUnit.Framework;

namespace Godot.Test
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
namespace Godot
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
namespace Godot
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
namespace Godot
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
namespace Godot
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
namespace Godot
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

        public static void DoTest(string sourceText, string resultText)
        {
            var generator = new DependencyInjectorGenerator();
            TestHelper.DoTest(generator, sourceText, resultText);
        }
    }
}
