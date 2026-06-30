# Contrato — Plantilla de Architecture Decision Record (ADR)

Define el formato de los ADR en `docs/adr/`. Satisface FR-008 y FR-010. Se materializa como `docs/adr/0000-plantilla.md`.

## Plantilla

```markdown
# ADR NNNN: <título de la decisión>

**Estado**: Propuesto | Aceptado | Reemplazado | Obsoleto
**Fecha**: AAAA-MM-DD
**Spec**: [specs/NNN-...](../../specs/NNN-.../spec.md)  <!-- si aplica -->
**Reemplaza**: ADR NNNN  <!-- opcional -->
**Reemplazado por**: ADR NNNN  <!-- opcional -->

## Contexto

<Situación, fuerzas técnicas y de negocio que motivan la decisión. Falsable y concreto.>

## Decisión

<Qué se decide hacer. Imperativo y específico.>

## Alternativas consideradas

- **<Alternativa A>**: <por qué se descartó>.
- **<Alternativa B>**: <por qué se descartó>.

## Consecuencias

- **Positivas**: <beneficios>.
- **Negativas / costes**: <contrapartidas asumidas>.
```

## Reglas

1. Identificador `NNNN` secuencial de cuatro dígitos; nombre de archivo `NNNN-titulo-kebab.md`.
2. `Estado` obligatorio; las transiciones válidas son `Propuesto → Aceptado → Reemplazado/Obsoleto`.
3. Un ADR que sustituye a otro DEBE rellenar `Reemplaza`, y el sustituido DEBE marcarse `Reemplazado` con `Reemplazado por` (FR-010).
4. Secciones obligatorias: Contexto, Decisión, Alternativas consideradas, Consecuencias.
5. Redacción en **español** (FR-011).

## Índice

`docs/adr/README.md` lista todos los ADR con: número, título, estado y fecha. Se actualiza al crear o cambiar el estado de un ADR.
