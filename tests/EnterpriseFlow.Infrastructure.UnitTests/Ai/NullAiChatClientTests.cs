using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Ai;
using FluentAssertions;

namespace EnterpriseFlow.Infrastructure.UnitTests.Ai;

public class NullAiChatClientTests
{
    [Fact]
    public async Task SendAsync_Returns_A_Message_Explaining_No_Provider_Is_Configured()
    {
        var sut = new NullAiChatClient();

        var result = await sut.SendAsync([new AiChatMessage(AiChatRole.User, "Hola")], [], CancellationToken.None);

        result.FinalText.Should().Be("El asistente de IA no está configurado en este entorno.");
        result.ToolCalls.Should().BeEmpty();
    }
}
