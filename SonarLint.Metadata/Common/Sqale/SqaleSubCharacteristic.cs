//-----------------------------------------------------------------------
// <copyright file="RuleGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

namespace SonarLint.Common.Sqale
{
    public enum SqaleSubCharacteristic
    {
        Modularity,
        Transportability,
        CompilerRelatedPortability,
        HardwareRelatedPortability,
        LanguageRelatedPortability,
        OsRelatedPortability,
        SoftwareRelatedPortability,
        TimeZoneRelatedPortability,
        Readability,
        Understandability,
        ApiAbuse,
        Errors,
        InputValidationAndRepresentation,
        SecurityFeatures,
        CpuEfficiency,
        MemoryEfficiency,
        NetworkUse,
        ArchitectureChangeability,
        DataChangeability,
        LogicChangeability,
        ArchitectureReliability,
        DataReliability,
        ExceptionHandling,
        FaultTolerance,
        InstructionReliability,
        LogicReliability,
        ResourceReliability,
        SynchronizationReliability,
        UnitTests,
        IntegrationTestability,
        UnitTestability,
        UsabilityAccessibility,
        UsabilityCompliance,
        UsabilityEaseOfUse,
        ReusabilityCompliance,
        PortabilityCompliance,
        MaintainabilityCompliance,
        SecurityCompliance,
        EfficiencyCompliance,
        ChangeabilityCompliance,
        ReliabilityCompliance,
        TestabilityCompliance
    }
}