using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AnonModuleDeclaration
  {
    public override TreeTextRange GetNameRange() => /*TreeTextRange.InvalidRange;*/ new TreeTextRange(TreeOffset.Zero);
    protected override string DeclaredElementName => GetSourceFile().GetLocation().NameWithoutExtension.Capitalize();
    public bool IsModule => true;
  }
}