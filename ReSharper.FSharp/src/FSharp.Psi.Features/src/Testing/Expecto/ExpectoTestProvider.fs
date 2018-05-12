namespace rec JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Testing.Expecto

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Linq
open JetBrains.Metadata.Reader.API
open JetBrains.Metadata.Reader.Impl
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel
open JetBrains.ReSharper.Feature.Services.ClrLanguages
open JetBrains.ReSharper.Psi
open JetBrains.ReSharper.Psi.ExtensionsAPI
open JetBrains.ReSharper.Psi.Tree
open JetBrains.ReSharper.Psi.Util
open JetBrains.ReSharper.Resources.Shell
open JetBrains.ReSharper.UnitTestFramework
open JetBrains.ReSharper.UnitTestFramework.AttributeChecker
open JetBrains.ReSharper.UnitTestFramework.Elements
open JetBrains.ReSharper.UnitTestFramework.Exploration
open JetBrains.Threading
open JetBrains.Util
open JetBrains.Util.Dotnet.TargetFrameworkIds
open JetBrains.Util.Reflection

[<SolutionComponent>]
type ExpectoElementsSource
        (testProvider: ExpectoTestProvider, testElementManager: IUnitTestElementManager,
         idFactory: IUnitTestElementIdFactory, testAttributeCache: UnitTestAttributeCache,
         testCachingService: UnitTestingCachingService, clrLanguages: ClrLanguagesKnown) =

    interface IUnitTestExplorerFromFile with
        member x.Provider = testProvider :> _

        member x.ProcessFile(psiFile, observer, interruptCheck) =
            match clrLanguages.AllLanguages.Any(fun language -> language.Equals(psiFile.Language)) with
            | false -> ()
            | _ ->

            match psiFile.GetSourceFile().ToProjectFile() with
            | null -> ()
            | projectFile ->

            match projectFile.GetProject() with
            | null -> ()
            | project ->

            let factory = ExpectoTestElementFactory(testProvider, idFactory, testElementManager)

            let fileExplorer =
                ExpectoTestFileExplorer
                    (psiFile, project, factory, testAttributeCache, testCachingService, observer, interruptCheck)

            psiFile.ProcessDescendants(fileExplorer)
            observer.OnCompleted()


type ExpectoTestFileExplorer
        (psiFile: IFile, project, factory: ExpectoTestElementFactory, unitTestAttributeCache: UnitTestAttributeCache,
         testCachingService: UnitTestingCachingService, observer: IUnitTestElementsObserver,
         interruptCheck: Func<bool>) =

    let targetFrameworkId = project.GetCurrentTargetFrameworkId()
//    let psiModule = psiFile.GetPsiModule()
//    let testType = TypeFactory.CreateTypeByCLRName(ExpectoTestProvider.TestTypeName, psiModule)

    interface IRecursiveElementProcessor with
        member x.InteriorShouldBeProcessed(element) =
            not (element :? ITypeMemberDeclaration) || element :? ITypeDeclaration

        member x.ProcessingIsFinished =
            match interruptCheck.Invoke() with
            | true -> OperationCanceledException() |> raise
            | _ -> false

        member x.ProcessBeforeInterior(element) =
            match element with
            | :? IDeclaration as declaration ->
                match declaration.DeclaredElement with
                | :? ITypeMember as typeMember when
                        typeMember.ShortName <> SharedImplUtil.MISSING_DECLARATION_NAME &&
                        typeMember.IsStatic && typeMember.GetAccessRights() = AccessRights.PUBLIC &&
                        unitTestAttributeCache
                            .HasAttributeOrDerivedAttribute(project, typeMember, ExpectoTestProvider.TestAttributes) ->

                    let returnType =
                        match typeMember with
                        | :? IMethod as method -> method.ReturnType
                        | :? IProperty as property -> property.ReturnType
                        | :? IField as field -> field.Type
                        | _ -> null

                    if isNull returnType then () else

                    match returnType.GetTypeElement() with
                    | null -> ()
                    | returnTypeElement ->

                    match returnTypeElement.GetClrName().Equals(ExpectoTestProvider.TestTypeName) with
                    | false -> ()
                    | _ ->

                    match typeMember.GetContainingType() with
                    | null -> ()
                    | containingType ->
                        let typeName = containingType.GetClrName()
                        let id = sprintf "%O.%s" typeName typeMember.ShortName
                        let testElement =
                            factory.GetOrCreateTest(id, project, targetFrameworkId, typeName, typeMember.ShortName,
                                                    testCachingService)

                        let navigationRange = declaration.GetNameDocumentRange().TextRange
                        let containingRange = declaration.GetDocumentRange().TextRange

                        if navigationRange.IsValid && containingRange.IsValid then
                            let projectFile = psiFile.GetSourceFile().ToProjectFile()
                            let subElements = EmptyList<IUnitTestElement>.Instance

                            observer.OnUnitTestElementDisposition(
                                UnitTestElementDisposition(testElement, projectFile, navigationRange,
                                                           containingRange, subElements))
                | _ -> ()
            | _ -> ()

        member x.ProcessAfterInterior(element) = ()


[<UnitTestProvider>]
type ExpectoTestProvider() =
    let expectoId = "Expecto"
    let expectoAssemblyName = AssemblyNameInfoFactory.Create2("expecto", null)

    static let testTypeName = ClrTypeName("Expecto.Test") :> IClrTypeName

    static let testsAttribute = ClrTypeName("Expecto.TestsAttribute") :> IClrTypeName
    static let ftestsAttribute = ClrTypeName("Expecto.FTestsAttribute") :> IClrTypeName
    static let ptestsAttribute = ClrTypeName("Expecto.PTestsAttribute") :> IClrTypeName

    static let testAttributes = [| testsAttribute; ftestsAttribute; ptestsAttribute |]

    let isSupported project targetFrameworkId =
        use cookie = ReadLockCookie.Create()
        ReferencedAssembliesService
            .IsProjectReferencingAssemblyByName
                (project, targetFrameworkId, expectoAssemblyName, &Unchecked.defaultof<AssemblyNameInfo>)

    let isElementOfKind elementKind (declaredElement: IDeclaredElement) =
        match elementKind with
        | UnitTestElementKind.Test ->
            declaredElement :? IMethod || declaredElement :? IProperty || declaredElement :? IField

        | UnitTestElementKind.TestContainer ->
            declaredElement :? ITypeElement

        | _ -> false

    static member TestAttributes: IClrTypeName[] = testAttributes
    static member TestTypeName: IClrTypeName = testTypeName

    interface IUnitTestProvider with
        member x.ID = expectoId
        member x.Name = expectoId

        member x.IsElementOfKind(declaredElement: IDeclaredElement, elementKind: UnitTestElementKind) =
            isElementOfKind elementKind declaredElement

        member x.IsElementOfKind(element: IUnitTestElement, elementKind: UnitTestElementKind) =
            match element.GetDeclaredElement() with
            | null -> false
            | declaredElement -> isElementOfKind elementKind declaredElement

        member x.IsSupported(project, targetFrameworkId) = isSupported project targetFrameworkId
        member x.IsSupported(hostProvider, project, targetFrameworkId) = isSupported project targetFrameworkId
    
        member __.CompareUnitTestElements(x, y) = String.CompareOrdinal(x.ShortName, y.ShortName) // todo


type ExpectoTestElementFactory
        (testProvider, idFactory: IUnitTestElementIdFactory, elementManager: IUnitTestElementManager) =
    let locker = obj()
    let elements = new Dictionary<UnitTestElementId, IUnitTestElement>()

    member x.GetOrCreateTest(id, project, targetFrameworkId, typeName, shortName, testCachingService) =
        lock locker (fun _ ->
        let uid = idFactory.Create(testProvider, project, targetFrameworkId, id)
        elements.GetOrCreateValue(uid, fun () ->
            ExpectoTestElement(shortName, uid, typeName, testCachingService) :> IUnitTestElement))


type ExpectoTestElement(shortName, id, typeName: IClrTypeName, testCachingService: UnitTestingCachingService) =
    let getDeclaredType () =
        testCachingService.GetTypeElement(id.Project, id.TargetFrameworkId, typeName, true, true);

    interface IUnitTestElement with
        member x.Id = id
        member x.ShortName = shortName

        member x.Kind = "Expecto test"

        member x.GetPresentation(parent, full) = typeName.FullName

        member x.GetNamespace() = null

        member x.GetDisposition() = null

        member x.GetDeclaredElement() =
            match getDeclaredType () with
            | null -> null
            | declaredType ->
                // todo: take element sensibly
                declaredType
                    .EnumerateMembers(shortName, declaredType.CaseSensitiveName)
                    .FirstOrDefault() :> _

        member x.GetProjectFiles() = null

        member x.OwnCategories = EmptySet.InstanceSet
        member x.Children = EmptyList.Instance :> _

        member x.ExplicitReason = null
        member x.Explicit = false

        member val State = Unchecked.defaultof<UnitTestElementState> with get, set
        member val Parent = Unchecked.defaultof<IUnitTestElement> with get, set

        member x.GetRunStrategy(hostProvider) = null
        member x.GetTaskSequence(explicitElements, run) = null
