using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedModuleDeclaration
  {
    protected override string DeclaredElementName => LongIdentifier.GetModuleCompiledName(Attributes);
    public override string SourceName => LongIdentifier.GetSourceName();

    public override TreeTextRange GetNameRange() => LongIdentifier.GetNameRange();

    public bool IsModule => true;
  }
}
