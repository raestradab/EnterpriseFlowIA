using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Ai;
using FluentAssertions;
using OpenAI.Chat;

namespace EnterpriseFlow.Infrastructure.UnitTests.Ai;

/// <summary>
/// Sprint 7a: only the request-side mapping (<see cref="OpenAiChatMessageMapper.ToOpenAi(AiChatMessage)"/>)
/// is unit-tested here — <c>OpenAI.Chat.ChatMessage</c> subtypes are publicly constructible
/// request DTOs. <c>ToAiChatResponse(ChatCompletion)</c> is not: <c>ChatCompletion</c> has no
/// public constructor in the SDK (it's only produced by deserializing a real API response), so
/// that direction can't be exercised without a live call — disclosed here rather than faked.
/// </summary>
public class OpenAiChatMessageMapperTests
{
    [Fact]
    public void Maps_System_Role_To_SystemChatMessage()
    {
        var result = OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.System, "Sos el asistente."));

        result.Should().BeOfType<SystemChatMessage>();
        result.Content[0].Text.Should().Be("Sos el asistente.");
    }

    [Fact]
    public void Maps_User_Role_To_UserChatMessage()
    {
        var result = OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.User, "¿Qué proyectos tengo?"));

        result.Should().BeOfType<UserChatMessage>();
        result.Content[0].Text.Should().Be("¿Qué proyectos tengo?");
    }

    [Fact]
    public void Maps_Plain_Assistant_Role_To_AssistantChatMessage_With_Text()
    {
        var result = OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.Assistant, "Tenés 3 proyectos."));

        var assistantMessage = result.Should().BeOfType<AssistantChatMessage>().Subject;
        assistantMessage.Content[0].Text.Should().Be("Tenés 3 proyectos.");
        assistantMessage.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void Maps_Assistant_ToolCalls_To_AssistantChatMessage_With_Function_Calls()
    {
        var toolCalls = new List<AiToolCallRequest> { new("call-1", "get_my_projects", "{}") };

        var result = OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.Assistant, string.Empty, ToolCalls: toolCalls));

        var assistantMessage = result.Should().BeOfType<AssistantChatMessage>().Subject;
        assistantMessage.ToolCalls.Should().ContainSingle();
        assistantMessage.ToolCalls[0].Id.Should().Be("call-1");
        assistantMessage.ToolCalls[0].FunctionName.Should().Be("get_my_projects");
    }

    [Fact]
    public void Maps_Tool_Role_To_ToolChatMessage_Matched_By_Id()
    {
        var result = OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.Tool, "[]", "call-1", "get_my_projects"));

        var toolMessage = result.Should().BeOfType<ToolChatMessage>().Subject;
        toolMessage.ToolCallId.Should().Be("call-1");
        toolMessage.Content[0].Text.Should().Be("[]");
    }

    [Fact]
    public void Tool_Role_Without_A_ToolCallId_Throws()
    {
        var act = () => OpenAiChatMessageMapper.ToOpenAi(new AiChatMessage(AiChatRole.Tool, "[]"));

        act.Should().Throw<InvalidOperationException>();
    }
}
