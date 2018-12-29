using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal class AnonModulePart : TopLevelModulePartBase<IAnonModuleDeclaration>
  {
    public AnonModulePart([NotNull] IAnonModuleDeclaration declaration, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, cacheBuilder)
    {
    }

    public AnonModulePart(IReader reader) : base(reader)
    {
    }
    
    protected override byte SerializationTag => (byte) FSharpPartKind.NamedModule;
  }
}