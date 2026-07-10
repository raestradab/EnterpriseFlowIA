using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Moq;

namespace EnterpriseFlow.Application.UnitTests.Common.Behaviors;

public class AuthorizationBehaviorTests
{
    private sealed record ProtectedRequest : IRequest<string>, IRequirePermission
    {
        public string RequiredPermission => "some.permission";
    }

    private sealed record UnprotectedRequest : IRequest<string>;

    [Fact]
    public async Task Handle_When_User_Has_Permission_Calls_Next()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(u => u.HasPermission("some.permission")).Returns(true);

        var behavior = new AuthorizationBehavior<ProtectedRequest, string>(currentUser.Object);

        var result = await behavior.Handle(new ProtectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_When_User_Lacks_Permission_Throws_Forbidden()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(u => u.HasPermission(It.IsAny<string>())).Returns(false);

        var behavior = new AuthorizationBehavior<ProtectedRequest, string>(currentUser.Object);

        var act = () => behavior.Handle(new ProtectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_When_Request_Does_Not_Require_Permission_Calls_Next()
    {
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);

        var behavior = new AuthorizationBehavior<UnprotectedRequest, string>(currentUser.Object);

        var result = await behavior.Handle(new UnprotectedRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        currentUser.VerifyNoOtherCalls();
    }
}
