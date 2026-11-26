# Part XXXIII — Mandatory GitHub synchronization

Owner addendum to the [master specification](MASTER_IMPLEMENTATION_SPECIFICATION.md), supplied November 26, 2026.

## 187. Commit and push after every implementation session

Before reporting implementation work, commit and push its source, tests, documentation, configuration, architectural decisions, reports and checkpoint. Exclude credentials, secrets, restricted assets, temporary files, generated caches and unnecessary build artifacts. Follow branch protection.

## 188. Remote repository as the source of truth

Keep GitHub current as the authoritative shared project record. Disclose any implementation that exists only locally.

## 189. Incremental checkpoint commits

Make meaningful commits during lengthy work. Document incomplete work on its feature branch and keep it out of main until verified and authorized.

## 190. Pull request synchronization

Associate active branches with PRs where appropriate. Update the description after pushes with progress, tests, performance, limitations and remaining work. Use draft PRs for incomplete or unverified implementations. Merge only with owner authorization.

## 191. Mandatory end-of-response verification

Inspect status and relevant diffs, run applicable tests, update traceability, progress and continuation, commit and push the feature branch, verify its remote head, then create or update its PR. If pushing fails, retain the local commit and report the synchronization blocker.

## 192. Mandatory GitHub references in every implementation report

Report branch, latest commit SHA and direct URL, PR URL, actual tests, remote synchronization, outstanding local changes and requirements, and the exact next development step.

## 193. Permanent development history

Preserve meaningful history and evidence of successful, failed and inconclusive work. Keep main protected from force pushes and deletion. Leave each session recoverable, documented and remotely inspectable.
