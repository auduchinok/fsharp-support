﻿using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken, IFSharpIdentifier
  {
    private FSharpSymbolReference mySymbolReference;

    public FSharpIdentifierToken(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public FSharpIdentifierToken(string text) : base(FSharpTokenType.IDENTIFIER, text)
    {
    }

    protected override void PreInit()
    {
      base.PreInit();
      mySymbolReference = new FSharpSymbolReference(this);
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(mySymbolReference);

    public string Name => GetText().RemoveBackticks();
  }
}
