using System.Text.Json.Nodes;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Ai;
using FluentAssertions;

namespace EnterpriseFlow.Infrastructure.UnitTests.Ai;

/// <summary>
/// Sprint 7a: the pure translation logic is fully unit-testable without a real API call (unlike
/// <c>OpenAiChatMessageMapper</c>'s response side, which depends on an opaque SDK type) — both
/// directions here are plain <c>JsonNode</c> trees this project builds and parses itself.
/// </summary>
public class AnthropicMessageMapperTests
{
    [Fact]
    public void BuildRequestBody_Moves_System_Messages_To_The_Top_Level_Field()
    {
        var messages = new List<AiChatMessage> { new(AiChatRole.System, "Sos el asistente."), new(AiChatRole.User, "Hola") };

        var body = AnthropicMessageMapper.BuildRequestBody("claude-3-5-sonnet-20241022", messages, []);

        body["system"]!.GetValue<string>().Should().Be("Sos el asistente.");
        body["messages"]!.AsArray().Should().HaveCount(1);
        body["messages"]![0]!["role"]!.GetValue<string>().Should().Be("user");
    }

    [Fact]
    public void BuildRequestBody_Omits_System_Field_When_There_Is_No_System_Message()
    {
        var messages = new List<AiChatMessage> { new(AiChatRole.User, "Hola") };

        var body = AnthropicMessageMapper.BuildRequestBody("claude-3-5-sonnet-20241022", messages, []);

        body["system"].Should().BeNull();
    }

    [Fact]
    public void BuildRequestBody_Maps_An_Assistant_Tool_Call_Turn_To_Tool_Use_Blocks()
    {
        var toolCalls = new List<AiToolCallRequest> { new("call-1", "get_my_projects", "{}") };
        var messages = new List<AiChatMessage> { new(AiChatRole.Assistant, string.Empty, ToolCalls: toolCalls) };

        var body = AnthropicMessageMapper.BuildRequestBody("claude-3-5-sonnet-20241022", messages, []);

        var assistantMessage = body["messages"]![0]!;
        assistantMessage["role"]!.GetValue<string>().Should().Be("assistant");
        var block = assistantMessage["content"]!.AsArray()[0]!;
        block["type"]!.GetValue<string>().Should().Be("tool_use");
        block["id"]!.GetValue<string>().Should().Be("call-1");
        block["name"]!.GetValue<string>().Should().Be("get_my_projects");
    }

    [Fact]
    public void BuildRequestBody_Merges_Consecutive_Tool_Results_Into_A_Single_User_Turn()
    {
        // Anthropic requires every tool result answering the same assistant turn to be blocks
        // inside ONE user message — not separate consecutive user messages (found building
        // AnthropicChatClient, Sprint 7a; see the mapper's own doc comment).
        var messages = new List<AiChatMessage>
        {
            new(AiChatRole.Tool, "[]", "call-1", "get_my_projects"),
            new(AiChatRole.Tool, "[]", "call-2", "get_my_tasks"),
        };

        var body = AnthropicMessageMapper.BuildRequestBody("claude-3-5-sonnet-20241022", messages, []);

        body["messages"]!.AsArray().Should().HaveCount(1);
        var userMessage = body["messages"]![0]!;
        userMessage["role"]!.GetValue<string>().Should().Be("user");
        userMessage["content"]!.AsArray().Should().HaveCount(2);
        userMessage["content"]![0]!["tool_use_id"]!.GetValue<string>().Should().Be("call-1");
        userMessage["content"]![1]!["tool_use_id"]!.GetValue<string>().Should().Be("call-2");
    }

    [Fact]
    public void BuildRequestBody_Includes_Tools_As_Input_Schema()
    {
        var tools = new List<AiToolDefinition> { new("get_my_projects", "Lista de proyectos.", """{"type":"object","properties":{}}""") };

        var body = AnthropicMessageMapper.BuildRequestBody("claude-3-5-sonnet-20241022", [], tools);

        var tool = body["tools"]!.AsArray()[0]!;
        tool["name"]!.GetValue<string>().Should().Be("get_my_projects");
        tool["input_schema"]!["type"]!.GetValue<string>().Should().Be("object");
    }

    [Fact]
    public void ParseResponse_With_Text_Content_Returns_FinalText()
    {
        var response = JsonNode.Parse("""{"content":[{"type":"text","text":"Tenés 3 proyectos."}]}""")!.AsObject();

        var result = AnthropicMessageMapper.ParseResponse(response);

        result.FinalText.Should().Be("Tenés 3 proyectos.");
        result.ToolCalls.Should().BeEmpty();
    }

    [Fact]
    public void ParseResponse_With_A_Tool_Use_Block_Returns_A_ToolCall_Not_FinalText()
    {
        var response = JsonNode.Parse(
            """{"content":[{"type":"tool_use","id":"call-1","name":"get_my_projects","input":{}}]}""")!.AsObject();

        var result = AnthropicMessageMapper.ParseResponse(response);

        result.FinalText.Should().BeNull();
        result.ToolCalls.Should().ContainSingle(t => t.Id == "call-1" && t.ToolName == "get_my_projects");
    }
}
