using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public static class FSharpTreeNodeExtensions
  {
    [CanBeNull, Pure]
    public static INamespaceDeclaration GetContainingNamespaceDeclaration([NotNull] this IFSharpTreeNode treeNode)
    {
      return treeNode.GetContainingNode<INamespaceDeclaration>();
    }

    [CanBeNull, Pure]
    public static IFSharpTypeElementDeclaration GetContainingTypeDeclaration([NotNull] this ITreeNode treeNode)
    {
      while (treeNode != null && !(treeNode is ITypeDeclaration))
        treeNode = treeNode.Parent;

      return (IFSharpTypeElementDeclaration) treeNode;
    }
  }
}