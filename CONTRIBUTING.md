# Contribuir a EnterpriseFlow AI

## Mensajes de commit — Conventional Commits

F13.5 (Release 4). El formato es
[Conventional Commits](https://www.conventionalcommits.org/): `tipo(scope): descripción`.

```
feat(projects): add temporal history endpoint
fix(api): register response compression before Swagger
docs(adr): resolve ADR-0001 point 6
```

Tipos usados en este repo: `feat`, `fix`, `docs`, `refactor`, `test`,
`chore`, `ci`, `perf`. `scope` es opcional — cuando se usa, típicamente el
nombre del módulo/Feature (`projects`, `assistant`, `rag`) o la capa
(`api`, `infra`, `web`). Un `!` después del tipo/scope (`feat!:` o
`feat(api)!:`) marca un cambio incompatible (*breaking change*).

Por qué importa más allá de legibilidad: `.github/workflows/release-please.yml`
(ver más abajo) deriva automáticamente la próxima versión semántica y las
notas de la release a partir de estos mensajes — un `fix:` sube el parche,
un `feat:` sube el minor, cualquier `!` o un pie `BREAKING CHANGE:` sube el
major. Un commit que no sigue el formato simplemente no aparece en las
notas generadas (no rompe nada, pero pierde visibilidad).

## Versionado semántico y Release Notes automáticas

[release-please](https://github.com/googleapis/release-please) (Google,
Apache-2.0) corre en cada push a `main`: lee el historial de commits desde
la última release, calcula la siguiente versión ([SemVer](https://semver.org/))
según el criterio anterior, y mantiene abierto un PR con `CHANGELOG.md`
actualizado. Al mergear ese PR, crea el tag y la GitHub Release
correspondiente con las notas generadas.

Elegido sobre `semantic-release` (Node, requiere una cadena de plugins
para lograr lo mismo — más superficie de configuración para un repo que ya
tiene su propio toolchain de Node solo para el frontend) y sobre
mantener `CHANGELOG.md` a mano (lo que este mismo Release encontró que
llevaba abandonado desde Release 1 — automatizarlo es la corrección
estructural, no solo llenar el archivo una vez).

**Límite de verificación de este entorno**: este directorio de trabajo no
es un repositorio Git real (`git status` falla con "not a git repository")
— no hay historial de commits, tags ni remoto de GitHub sobre el cual
`release-please` pueda operar. El workflow y su configuración quedan
validados como YAML/JSON sintácticamente correctos, no ejecutados de
punta a punta — mismo límite, y mismo criterio de honestidad, que
`docs/11-devops.md` ya declaró para `ci.yml`/`docker-compose.yml` desde
Release 1.
