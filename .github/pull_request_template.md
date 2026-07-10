## Qué cambia y por qué

<!-- Resumen breve. Si hay una decisión técnica con alternativas descartadas, va en un ADR
     (docs/adr/), no acá — enlazalo. -->

## Checklist

- [ ] `dotnet format EnterpriseFlow.slnx --verify-no-changes` pasa localmente
- [ ] `dotnet test EnterpriseFlow.slnx` pasa localmente (`--filter "Category!=RequiresSqlServer"`
      si no tenés SQL Server/LocalDB a mano)
- [ ] `npm run build` pasa localmente si el cambio toca `src/EnterpriseFlow.Web/`
- [ ] La cobertura agregada se mantiene ≥90% (el gate de CI lo verifica, pero conviene saberlo
      antes de abrir el PR)
- [ ] Los mensajes de commit siguen [Conventional Commits](../CONTRIBUTING.md#mensajes-de-commit--conventional-commits)
- [ ] Si el cambio agrega o difiere alcance, `docs/backlog/epics.md`/`docs/02-roadmap.md` quedan
      al día
