namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public enum FSharpPartKind : byte
  {
    QualifiedNamespace = 0,
    DeclaredNamespace = 1,
    NamedModule = 2,
    AnonModule = 3,
    NestedModule = 4,
    Exception = 5,
    Enum = 6,
    Record = 7,
    Union = 8,
    UnionCase = 9,
    HiddenType = 10,
    Interface = 11,
    Class = 12,
    Struct = 13,
    StructRecord = 14,
    StructUnion = 15,
  }
}