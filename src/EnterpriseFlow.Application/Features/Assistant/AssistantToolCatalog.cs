using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Application.Features.Assistant;

/// <summary>
/// HU-092/ADR-0013: the entire surface of what the assistant can ask for — one entry per
/// existing Application Query it may invoke. Adding a new tool means adding a name here and a
/// matching case in <c>SendAssistantMessageCommandHandler.InvokeToolAsync</c>, never widening
/// what the model can reach beyond a specific, named Query.
/// </summary>
internal static class AssistantToolCatalog
{
    public const string GetMyProjects = "get_my_projects";
    public const string SearchMyDocuments = "search_my_documents";
    public const string GetMyOverdueTasks = "get_my_overdue_tasks";

    public static IReadOnlyList<AiToolDefinition> All { get; } =
    [
        new AiToolDefinition(
            GetMyProjects,
            "Devuelve la lista de Proyectos del tenant actual (nombre, cliente, estado).",
            """{"type":"object","properties":{}}"""),
        new AiToolDefinition(
            SearchMyDocuments,
            "Busca en el contenido de los Documentos del tenant actual (RAG, HU-101) y devuelve los fragmentos más relevantes para una pregunta — la única fuente válida para responder preguntas sobre el contenido de un Documento.",
            """{"type":"object","properties":{"query":{"type":"string","description":"La pregunta o tema a buscar."}},"required":["query"]}"""),
        new AiToolDefinition(
            GetMyOverdueTasks,
            "Devuelve las Tareas asignadas al usuario actual que ya vencieron y siguen sin completarse — la única fuente válida para responder cuántas tareas tiene atrasadas.",
            """{"type":"object","properties":{}}"""),
    ];
}
