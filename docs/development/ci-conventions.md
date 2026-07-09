# CI Conventions

ProjectHope uses GitHub Actions for pull request validation, main-branch validation, and tag-based releases.

## Reducing duplicate CI runs

The repository workflow owns concurrency at the caller-workflow level:

```yaml
concurrency:
  group: ${{ github.workflow }}-${{ github.event.pull_request.number || github.ref }}
  cancel-in-progress: true
```

This keeps the newest run for a pull request or ref and cancels stale queued or in-progress runs.

## Skip CI convention

Use `[skip ci]` only for intermediate commits during active development when a real CI result is not needed yet.

Never use `[skip ci]` on the final reviewable commit for a pull request. The final commit must allow GitHub Actions to run normally so the PR has a real validation result before merge.
