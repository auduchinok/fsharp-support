﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpCacheDeclarationProcessor : TreeNodeVisitor
  {
    protected readonly ICacheBuilder Builder;
    private readonly FSharpCheckerService myCheckerService;

    public FSharpCacheDeclarationProcessor(ICacheBuilder builder, FSharpCheckerService checkerService)
    {
      Builder = builder;
      myCheckerService = checkerService;
    }

    private static FSharpFileKind GetFSharpFileKind(IFSharpFile file)
    {
      if (file is IFSharpImplFile) return FSharpFileKind.ImplFile;
      if (file is IFSharpSigFile) return FSharpFileKind.SigFile;
      throw new ArgumentOutOfRangeException();
    }

    public override void VisitFSharpFile(IFSharpFile fsFile)
    {
      var sourceFile = fsFile.GetSourceFile();
      var fileKind = GetFSharpFileKind(fsFile);
      var hasPairFile = myCheckerService.HasPairFile(sourceFile);

      Builder.CreateProjectFilePart(new FSharpProjectFilePart(sourceFile, fileKind, hasPairFile));

      foreach (var declaration in fsFile.DeclarationsEnumerable)
      {
        var qualifiers = declaration.LongIdentifier.Qualifiers;
        foreach (var qualifier in qualifiers)
        {
          var qualifierName = qualifier.GetText().RemoveBackticks();
          Builder.StartPart(new QualifiedNamespacePart(qualifier.GetTreeStartOffset(), Builder.Intern(qualifierName)));
        }

        declaration.Accept(this);

        foreach (var _ in qualifiers)
          Builder.EndPart();
      }
    }

    public override void VisitDeclaredNamespaceDeclaration(IDeclaredNamespaceDeclaration decl)
    {
      Builder.StartPart(new DeclaredNamespacePart(decl));
      FinishModuleLikeDeclaration(decl);
    }

    public override void VisitGlobalNamespaceDeclaration(IGlobalNamespaceDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
    }

    public override void VisitAnonModuleDeclaration(IAnonModuleDeclaration decl)
    {
      Builder.StartPart(new AnonModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl);
    }

    public override void VisitNamedModuleDeclaration(INamedModuleDeclaration decl)
    {
      Builder.StartPart(new NamedModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl);
    }

    public override void VisitNestedModuleDeclaration(INestedModuleDeclaration decl)
    {
      Builder.StartPart(new NestedModulePart(decl, Builder));
      FinishModuleLikeDeclaration(decl);
    }

    private void FinishModuleLikeDeclaration(IModuleLikeDeclaration decl)
    {
      foreach (var memberDecl in decl.MembersEnumerable)
        memberDecl.Accept(this);
      Builder.EndPart();
    }

    public override void VisitMemberDeclaration(IMemberDeclaration decl)
    {
      Builder.AddDeclaredMemberName(decl.DeclaredName);
    }

    public override void VisitLet(ILet letParam)
    {
      Builder.AddDeclaredMemberName(letParam.DeclaredName);
    }

    public override void VisitExceptionDeclaration(IExceptionDeclaration decl)
    {
      Builder.StartPart(new ExceptionPart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitEnumDeclaration(IEnumDeclaration decl)
    {
      Builder.StartPart(new EnumPart(decl, Builder));
      foreach (var memberDecl in decl.EnumMembersEnumerable)
        Builder.AddDeclaredMemberName(memberDecl.DeclaredName);
      Builder.EndPart();
    }

    public override void VisitRecordDeclaration(IRecordDeclaration decl)
    {
      var recordPart =
        decl.HasAttribute(FSharpImplUtil.Struct)
          ? (Part) new StructRecordPart(decl, Builder)
          : new RecordPart(decl, Builder);

      Builder.StartPart(recordPart);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitUnionDeclaration(IUnionDeclaration decl)
    {
      var unionCases = decl.UnionCases;

      var casesWithFieldsCount = 0;
      foreach (var unionCase in unionCases)
        if (unionCase is INestedTypeUnionCaseDeclaration)
          casesWithFieldsCount++;
      var hasPublicNestedTypes = casesWithFieldsCount > 0 && unionCases.Count > 1;

      var unionPart =
        decl.HasAttribute(FSharpImplUtil.Struct)
          ? (Part) new StructUnionPart(decl, Builder, false)
          : new UnionPart(decl, Builder, hasPublicNestedTypes);

      Builder.StartPart(unionPart);
      foreach (var unionCase in unionCases)
        unionCase.Accept(this);
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitNestedTypeUnionCaseDeclaration(INestedTypeUnionCaseDeclaration decl)
    {
      Builder.StartPart(new UnionCasePart(decl, Builder));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitTypeAbbreviationDeclaration(ITypeAbbreviationDeclaration decl)
    {
      Builder.StartPart(new HiddenTypePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitAbstractTypeDeclaration(IAbstractTypeDeclaration decl)
    {
      Builder.StartPart(new HiddenTypePart(decl, Builder));
      Builder.EndPart();
    }

    public override void VisitObjectModelTypeDeclaration(IObjectModelTypeDeclaration decl)
    {
      Builder.StartPart(CreateObjectTypePart(decl));
      ProcessTypeMembers(decl.MemberDeclarations);
      Builder.EndPart();
    }

    public override void VisitTypeExtension(ITypeExtension typeExtension) =>
      ProcessTypeMembers(typeExtension.TypeMembers);

    private Part CreateObjectTypePart(IObjectModelTypeDeclaration decl)
    {
      switch (decl.TypePartKind)
      {
        case FSharpPartKind.Class:
          return new ClassPart(decl, Builder);
        case FSharpPartKind.Interface:
          return new InterfacePart(decl, Builder);
        case FSharpPartKind.Struct:
          return new StructPart(decl, Builder);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void ProcessTypeMembers(IReadOnlyList<ITypeMemberDeclaration> memberDeclarations)
    {
      foreach (var typeMemberDeclaration in memberDeclarations)
      {
        var declaredName = typeMemberDeclaration.DeclaredName;
        if (declaredName != SharedImplUtil.MISSING_DECLARATION_NAME)
          Builder.AddDeclaredMemberName(declaredName);
      }
    }
  }
}
