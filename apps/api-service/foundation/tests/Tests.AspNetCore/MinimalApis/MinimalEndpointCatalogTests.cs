using Aurore.Foundation.AspNetCore.MinimalApis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Tests.AspNetCore.MinimalApis;

[Trait("Feature", "Minimal endpoints")]
public sealed class MinimalEndpointCatalogTests
{
    [Fact(DisplayName = "Versions are distinct and ascending across groups")]
    public void VersionsAreDistinctAndAscendingAcrossGroups()
    {
        // Arrange
        Type[] types = [typeof(OrdersGroup), typeof(OrdersV1), typeof(OrdersV2), typeof(BillingGroup), typeof(BillingV2), typeof(BillingV3)];

        // Act
        var catalog = Create(types);

        // Assert
        Assert.Equal([1, 2, 3], catalog.Versions.Select(x => x.Number));
    }

    [Fact(DisplayName = "A deprecated version keeps its sunset date")]
    public void DeprecatedVersionKeepsItsSunsetDate()
    {
        // Arrange
        Type[] types = [typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2)];

        // Act
        var catalog = Create(types);

        // Assert
        Assert.Equal(new MinimalEndpointVersion(1), catalog.Versions[0]);
        Assert.Equal(new MinimalEndpointVersion(2, true, new DateOnly(2030, 1, 1)), catalog.Versions[1]);
    }

    [Fact(DisplayName = "Throws when an endpoint targets a version its group does not declare")]
    public void ThrowsWhenEndpointTargetsUndeclaredVersion()
    {
        // Arrange
        Type[] types = [typeof(UndeclaredGroup), typeof(UndeclaredV1), typeof(UndeclaredV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("targeting version 2", error.Message);
    }

    [Fact(DisplayName = "Throws when a declared version has no endpoint")]
    public void ThrowsWhenDeclaredVersionHasNoEndpoint()
    {
        // Arrange
        Type[] types = [typeof(NoEndpointGroup), typeof(NoEndpointV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("[2] without any endpoint", error.Message);
    }

    [Fact(DisplayName = "Throws when a group declares the same version twice")]
    public void ThrowsWhenVersionsAreDuplicated()
    {
        // Arrange
        Type[] types = [typeof(DuplicatedGroup), typeof(DuplicatedV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("duplicate versions", error.Message);
    }

    [Fact(DisplayName = "Throws when a sunset date is set on a version that is not deprecated")]
    public void ThrowsWhenSunsetIsSetOnAnActiveVersion()
    {
        // Arrange
        Type[] types = [typeof(SunsetOnActiveGroup), typeof(SunsetOnActiveV1)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("not deprecated", error.Message);
    }

    [Fact(DisplayName = "Throws when two groups share the same name")]
    public void ThrowsWhenGroupNamesAreNotUnique()
    {
        // Arrange
        Type[] types = [typeof(FirstSameNameGroup), typeof(SecondSameNameGroup)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("share the name 'same'", error.Message);
    }

    [Fact(DisplayName = "Throws when groups disagree on whether a version is deprecated")]
    public void ThrowsWhenGroupsDisagreeOnDeprecation()
    {
        // Arrange
        Type[] types = [typeof(OrdersGroup), typeof(OrdersV1), typeof(OrdersV2), typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("Version 2", error.Message);
        Assert.Contains("deprecation", error.Message);
    }

    [Fact(DisplayName = "Throws when groups disagree on the sunset date of a version")]
    public void ThrowsWhenGroupsDisagreeOnSunsetDate()
    {
        // Arrange
        Type[] types = [typeof(DeprecatingGroup), typeof(DeprecatingV1), typeof(DeprecatingV2), typeof(LaterSunsetGroup), typeof(LaterSunsetV2)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("Version 2", error.Message);
        Assert.Contains("sunset dates", error.Message);
    }

    [Fact(DisplayName = "Throws when two endpoints share the same route name")]
    public void ThrowsWhenRouteNamesAreNotUnique()
    {
        // Arrange
        Type[] types = [typeof(SameRouteNameGroup), typeof(SameRouteNameFirst), typeof(SameRouteNameSecond)];

        // Act
        var error = Assert.Throws<InvalidOperationException>(() => Create(types));

        // Assert
        Assert.Contains("share the route name 'same-route'", error.Message);
    }

    private static MinimalEndpointCatalog Create(params Type[] types)
    {
        return MinimalEndpointCatalog.Create(types);
    }

    private abstract class TestEndpoint<TGroup> : MinimalEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override RouteHandlerBuilder Map(IEndpointRouteBuilder builder)
        {
            throw new NotSupportedException();
        }
    }

    private abstract class TestEndpointV2<TGroup> : TestEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override int Version => 2;
    }

    private abstract class TestEndpointV3<TGroup> : TestEndpoint<TGroup>
        where TGroup : ResourceGroup
    {
        public override int Version => 3;
    }

    private sealed class OrdersGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 2];
    }

    private sealed class OrdersV1 : TestEndpoint<OrdersGroup>;

    private sealed class OrdersV2 : TestEndpointV2<OrdersGroup>;

    private sealed class BillingGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [2, 3];
    }

    private sealed class BillingV2 : TestEndpointV2<BillingGroup>;

    private sealed class BillingV3 : TestEndpointV3<BillingGroup>;

    private sealed class DeprecatingGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, new(2, true, new DateOnly(2030, 1, 1))];
    }

    private sealed class DeprecatingV1 : TestEndpoint<DeprecatingGroup>;

    private sealed class DeprecatingV2 : TestEndpointV2<DeprecatingGroup>;

    private sealed class LaterSunsetGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [new(2, true, new DateOnly(2031, 1, 1))];
    }

    private sealed class LaterSunsetV2 : TestEndpointV2<LaterSunsetGroup>;

    private sealed class SameRouteNameGroup : ResourceGroup;

    private sealed class SameRouteNameFirst : TestEndpoint<SameRouteNameGroup>
    {
        public override string? Name => "same-route";
    }

    private sealed class SameRouteNameSecond : TestEndpoint<SameRouteNameGroup>
    {
        public override string? Name => "same-route";
    }

    private sealed class UndeclaredGroup : ResourceGroup;

    private sealed class UndeclaredV1 : TestEndpoint<UndeclaredGroup>;

    private sealed class UndeclaredV2 : TestEndpointV2<UndeclaredGroup>;

    private sealed class NoEndpointGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 2];
    }

    private sealed class NoEndpointV1 : TestEndpoint<NoEndpointGroup>;

    private sealed class DuplicatedGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [1, 1];
    }

    private sealed class DuplicatedV1 : TestEndpoint<DuplicatedGroup>;

    private sealed class SunsetOnActiveGroup : ResourceGroup
    {
        public override MinimalEndpointVersion[] Versions => [new(1, false, new DateOnly(2030, 1, 1))];
    }

    private sealed class SunsetOnActiveV1 : TestEndpoint<SunsetOnActiveGroup>;

    private sealed class FirstSameNameGroup : ResourceGroup
    {
        public override string Name => "same";
    }

    private sealed class SecondSameNameGroup : ResourceGroup
    {
        public override string Name => "same";
    }
}
