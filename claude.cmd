@echo off
wt docker run -it --rm ^
  -v "%CD%:/project" ^
  -v "D:\Projects\ActualLab.Fusion:/fusion-project" ^
  -v "%USERPROFILE%\.claude:/home/claude/.claude" ^
  -v "%USERPROFILE%\.claude.json:/home/claude/.claude.json" ^
  -e ANTHROPIC_API_KEY=%ANTHROPIC_API_KEY% ^
  -e Claude_GeminiAPIKey=%Claude_GeminiAPIKey% ^
  --network host ^
  claude-fusion-samples ^
  claude --dangerously-skip-permissions %*
