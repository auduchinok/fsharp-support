using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class TopLevelModulePartBase<T> : ModulePartBase<T>
    where T : class, IFSharpModuleDeclaration
  {
    protected TopLevelModulePartBase([NotNull] T declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder.Intern(declaration.ShortName),
        ModifiersUtil.GetDecoration(declaration.AccessModifiers, declaration.AttributesEnumerable), cacheBuilder)
    {
    }

    protected TopLevelModulePartBase(IReader reader) : base(reader)
    {
    }

    public override TypeElement CreateTypeElement()
    {
      return new FSharpModule(this);
    }
  }
}