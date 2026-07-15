# Project-specific Rules for ActualLab.Fusion.Samples

**YOU MUST READ [CODING_STYLE.md](CODING_STYLE.md) before writing or
modifying any C# code.** It's not optional. This project
**deviates from standard .NET conventions** on several points (notably:
no `Async` suffix on async methods; no XML docs on members; mixed brace
style). Default instincts from elsewhere will produce code that gets
rejected. If you haven't opened that file yet in this session, stop and
read it now.

**You MUST NOT write a single comment, docstring, or XML doc** without
first reading [CODING_STYLE.md → "Regular comments, docstrings, XML
documentation comments"](CODING_STYLE.md#regular-comments-docstrings-xml-documentation-comments).

# Git workflow — don't branch unless asked

Commit your changes directly to the default branch (`master`). **You typically
should NOT create a feature branch in this repo unless the user explicitly asks
for one.** Small, self-contained changes (docs, fixes, tweaks) belong on
`master`; a needless branch only adds a merge step later.
