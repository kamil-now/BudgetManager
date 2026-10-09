---
name: implement
argument-hint: "<description>"
description: 'Implement a change described by the user, end to end, on the current branch. Checks the tree is clean, saves the request, traces the docs and the code, has the user settle every open question — one per message, each with a proposed answer and its source — then writes the code, runs the build and the tests, and finishes with /simplify and /code-review. Use when the user invokes `/implement <what to build>`, or hands over a change to build end to end.'
---

# Implement

A gap settled before the code is written costs one answer; the same gap settled after costs a rewrite. This skill settles every gap with the user first, then builds the change.

**Workflow**: follow [workflow.md](workflow.md), Step 0 to Step 5 in order. It is the whole procedure.
