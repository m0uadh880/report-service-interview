# Backend Interview Exercise — C# Report Service

## Context

You're working on an internal reporting tool that aggregates sales data and generates summaries. The service has been in production for a few weeks and the team is getting complaints:

- Reports sometimes show **incorrect order counts and revenue totals**
- The same warning message **appears multiple times** when generating more than one report in the same session
- Users report that **orders placed on the first day of any date range are missing** from the output

There is also a code quality issue that hasn't caused a production incident yet but has caused timeouts in load testing.

---

## Task 1 — Find and fix the issues

Open the project in your IDE (`ReportService/`). Run it with `dotnet run` and observe the output.

Find and fix **all the issues** in the codebase. You are not limited to the symptoms described above — fix everything you consider a real problem.

**For each fix, add a comment explaining:**
1. What the bug was
2. Why it caused the observed behaviour
3. What your fix does and why it's correct

---

## Task 2 — Add an export pipeline

The team wants to export report summaries to different formats. Implement an export pipeline that satisfies these requirements:

- Supports **JSON** and **CSV** export (write output to console or a file — your choice)
- Includes a **PDF stub** that logs a "not implemented" message instead of crashing
- Adding a **new export format in the future must not require modifying existing code**
- The caller must be able to **request multiple formats in a single call**
- If one format fails, the **others must still complete** — errors should be logged, not propagated

Wire it up in `Program.cs` so that after generating a report, the summary is exported in at least two formats.

---

## Rules and expectations

- **AI assistance is allowed and encouraged.** Tools like GitHub Copilot, ChatGPT, or Claude are fine.
- **You must understand every line you submit.** If AI generates code, review it, and in your comments/explanation make clear what you changed or accepted and why.
- There is **no time limit** — take the time you need, but don't over-engineer. We're evaluating judgment and understanding, not volume of code.
- The project must **compile and run** (`dotnet run`) when you return it.
- Return your solution as a zip of the `ReportService/` folder (or a git repo link).

---

## What we are evaluating

| Area | What we look for |
|---|---|
| Bug identification | Did you find all the issues? Did you understand the root cause, not just the symptom? |
| Fix quality | Is each fix correct and minimal? Did you avoid introducing new issues? |
| Explanation | Are your comments clear and accurate? Do they show you understand the *why*? |
| Task 2 design | Is the export pipeline extensible? Are interfaces used appropriately? |
| Task 2 implementation | Does it work? Are errors handled per-exporter without breaking others? |
| Code quality | Is the result readable? Does it follow existing patterns in the codebase? |
