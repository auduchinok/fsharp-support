namespace JetBrains.ReSharper.Plugins.FSharp.ProjectModel.ProjectProperties

open System.Runtime.InteropServices
open JetBrains.Metadata.Utils
open JetBrains.ProjectModel.Impl.Build
open JetBrains.ProjectModel
open JetBrains.ProjectModel.Properties
open JetBrains.ProjectModel.Properties.Common
open JetBrains.ProjectModel.Properties.Managed
open JetBrains.ReSharper.Plugins.FSharp.ProjectModel

type FSharpProjectProperties =
    inherit ProjectPropertiesBase<ManagedProjectConfiguration>

    val mutable targetPlatformData: TargetPlatformData
    val buildSettings: ManagedProjectBuildSettings

    new(projectTypeGuids, platformId, factoryGuid, targetFrameworkIds, targetPlatformData, dotNetCoreSDK) =
        { inherit ProjectPropertiesBase<_>(projectTypeGuids, platformId, factoryGuid, targetFrameworkIds, dotNetCoreSDK)
          buildSettings = ManagedProjectBuildSettings()
          targetPlatformData = targetPlatformData }

    new(factoryGuid, [<Optional; DefaultParameterValue(null: TargetPlatformData)>] targetPlatformData) =
        { inherit ProjectPropertiesBase<_>(factoryGuid)
          buildSettings = ManagedProjectBuildSettings()
          targetPlatformData = targetPlatformData }

    override x.BuildSettings = x.buildSettings :> _
    override x.DefaultLanguage = FSharpProjectLanguage.Instance :> _

    override x.ReadProjectProperties(reader) =
        base.ReadProjectProperties(reader)
        x.buildSettings.ReadBuildSettings(reader)
        let tpd = TargetPlatformData()
        tpd.Read(reader)
        if not tpd.IsEmpty then x.targetPlatformData <- tpd

    override x.WriteProjectProperties(writer) =
        base.WriteProjectProperties(writer)
        x.buildSettings.WriteBuildSettings(writer)
        match x.targetPlatformData with
        | null -> TargetPlatformData.WriteEmpty(writer)
        | _ -> x.targetPlatformData.Write(writer)

    override x.Dump(writer, indent) =
        writer.Write(new string(' ', indent * 2))
        writer.WriteLine("F# properties:")
        x.DumpActiveConfigurations(writer, indent)
        writer.Write(new string(' ', 2 + indent * 2))
        x.buildSettings.Dump(writer, indent + 2)
        base.Dump(writer, indent + 1)
