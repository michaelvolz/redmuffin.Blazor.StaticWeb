---
title: "Grok Build model selection: default reasoning and the cost-intelligence frontier"
date: 2026-07-18
category: tooling-decisions
module: grok-build-model-selection
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Choosing among GPT-5.5, GPT-5.6 Luna, GPT-5.6 Terra, and GPT-5.6 Sol in Grok Build when reasoning settings must remain at their defaults"
  - "Selecting a default model for coding and reasoning work where both answer quality and usage cost matter"
tags:
  - grok-build
  - model-selection
  - reasoning
  - cost-intelligence
  - pareto-frontier
  - luna
  - terra
  - sol
---

# Grok Build model selection: default reasoning and the cost-intelligence frontier

## Context

The harness exposes GPT-family model choices, but the active model identifier and
reasoning-effort value are not visible from the assistant conversation. That makes
it unsafe to infer that a session is using Luna or to claim that the default
reasoning level is `low`, `medium`, or another documented value.

External comparison data is available for GPT-5.6 Luna, Terra, and Sol, but the
published evidence does not provide a directly comparable GPT-5.5 result under the
same benchmark and effort setting.

## Guidance

For GPT-5.6 at maximum reasoning, Artificial Analysis reported the following
Intelligence Index scores and cost per benchmark task on July 9, 2026:

| Model | Intelligence Index | Cost per task |
| --- | ---: | ---: |
| Sol | 59 | $1.04 |
| Terra | 55 | $0.55 |
| Luna | 51 | $0.21 |

The absolute intelligence order in that comparison is **Sol, Terra, Luna**.
Artificial Analysis also reported on July 13, 2026, that Luna and Sol were ahead
of Terra across the tested cost-intelligence chart, making Terra a weak default
Pareto choice in that analysis. This does not mean Terra is universally inferior;
it means that the published chart did not show Terra as the best choice at any
of its tested effort-cost points.

The default selection should therefore be:

- **Luna** when cost efficiency and high-volume usage are the priority.
- **Sol** when the hardest coding or reasoning tasks justify higher cost.
- **Terra** only when workload-specific testing shows a benefit not captured by
the published benchmark.
- **GPT-5.5** only after a directly comparable local or vendor benchmark is
available.

OpenAI's model documentation describes Sol as the choice for complex reasoning
and coding, Terra as the intelligence-cost balance, and Luna as the option for
cost-sensitive, high-volume workloads. Those descriptions support the same
role-based recommendation, but they do not prove the harness's hidden default
settings.

## Why This Matters

A model name alone does not determine the cost-intelligence frontier. Reasoning
effort changes both quality and cost, and a comparison made at maximum effort
cannot be assumed to describe the harness's default setting. Separating verified
benchmark results from unobservable session configuration prevents false
confidence about which model is currently answering.

The recommendation is a decision under incomplete observability: Luna is the
best researched cost-efficiency default, while Sol is the best researched quality
maximum. The recommendation should be revisited if Grok Build exposes the active
model and effort or if a benchmark using the project's real coding tasks becomes
available.

## When to Apply

- Use this guidance when selecting a fixed default model without changing
  reasoning-effort controls.
- Prefer Luna for routine, high-volume agent work when the benchmark task mix is
  reasonably representative.
- Prefer Sol for difficult, high-value tasks where failure or rework costs more
  than additional inference.
- Do not rank GPT-5.5 against Luna, Terra, and Sol as a verified four-way order
  without a common benchmark, effort level, and pricing basis.

## Examples

A practical fixed-default policy is:

```text
Routine coding and research: GPT-5.6 Luna
Difficult architecture or debugging: GPT-5.6 Sol
Terra: use only when local task results justify it
GPT-5.5: keep as a separate comparison until comparable data exists
```

A useful future benchmark should run the same prompt set against each model while
holding the harness defaults unchanged, then record task success, correction or
rework rate, latency, and usage cost. Those measurements can establish a
project-specific Pareto frontier without assuming that public benchmark effort
levels match Grok Build defaults.

## Related

- [Grok Build native LSP tooling on Windows](grok-build-lsp-roslyn-windows.md)
- [Artificial Analysis GPT-5.6 cost and intelligence comparison](https://artificialanalysis.ai/articles/gpt-5-6-intelligence-vs-cost-across-sol-terra-luna)
- [Artificial Analysis GPT-5.6 benchmark analysis](https://artificialanalysis.ai/articles/gpt-5-6-has-landed)
- [OpenAI model documentation](https://platform.openai.com/docs/models)
