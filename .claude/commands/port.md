---
description: Port one Flutter control to C# end-to-end, picking the control itself if none is given
argument-hint: "[control name — optional]"
allowed-tools: Read, Grep, Glob, Edit, Write, Bash, Agent
---

Close one Dart-to-C# control port end-to-end in this single request.

Target: **$ARGUMENTS** — if that is empty, choose the control yourself using Step 0 of the playbook
and state your pick in one line before starting. Do not ask which control to port.

Follow `docs/ai/PORT_PLAYBOOK.md` in order. It is the authority; the reminders below only exist
because they are the steps most often skipped:

- Check the Dart file's line count first. Over ~800 lines, get the spec through the `dart-spec`
  subagent (`docs/ai/DART_SPEC_PROTOCOL.md`) so the source never enters this context.
- Land missing framework primitives in `src/Plumix` before touching the control.
- Cover every behavior Flutter's own tests assert, not just the happy path.
- Update both samples when the control is user-visible.
- Finish with all four checks green: `dotnet build src/Plumix.Ci.slnf -c Debug`,
  `dotnet test src/Plumix.Tests/Plumix.Tests.csproj`, `scripts/check_line_length.sh`,
  `python3 scripts/generate_port_map.py`.
- Then close the paperwork (Step 8). A port with the code done and the docs not updated is not done.

Work through the whole thing without checking in. If you hit a hard blocker, finish everything that
does not depend on it, write the note per `docs/ai/FEATURE_TEMPLATE.md`, and say plainly what is left.
