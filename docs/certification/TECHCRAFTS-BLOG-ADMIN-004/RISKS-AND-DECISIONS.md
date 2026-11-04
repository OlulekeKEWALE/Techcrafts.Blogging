# TECHCRAFTS-BLOG-ADMIN-004 — Risks, Decisions & Outstanding Human Actions

## 1. Risk Assessment & Mitigations

### Risk 1: GitHub Actions Runner Availability (Account Billing Limitation)
- **Impact:** High (Blocks automated GitHub-hosted PR check verification).
- **Status:** **BLOCKED on External GitHub Account Action**.
- **Evidence:** GitHub annotation `The job was not started because your account is locked due to a billing issue` across workflow runs `41753126508` and `41753646176`.
- **Mitigation:** The pipeline configuration (`.github/workflows/ci.yml`) is syntactically validated and tested. Local Release execution passed 100% of all 40 tests. The operator must resolve the GitHub account billing limit to allow the GitHub Actions runner to complete.

### Risk 2: Scheduled Publishing Worker Absence
- **Impact:** Low (Disclosed design boundary).
- **Status:** **ACCEPTED RISK / DISCLOSED NON-GOAL**.
- **Evidence:** The domain enum `PostStatus.Scheduled` exists, but there is no background worker or cron queue to transition scheduled posts to published state.
- **Mitigation:** The administrative UI deliberately omits any scheduling controls, allowing only explicit "Draft" and "Publish" actions. Scheduled publishing is formally documented as an unsupported capability until a dedicated background worker is implemented.

### Risk 3: Stale Conflict Screen Second-Race Overwrite
- **Impact:** Critical (Preventing subtle race conditions in concurrent multi-admin editing).
- **Status:** **RESOLVED & VERIFIED**.
- **Evidence:** Test `SQL_C04_SecondRaceConflict_AdminCOverwritesWhileAdminBViewsConflictScreen_AdminBMustConflictAgain` confirms that if Admin C commits V3 while Admin B is viewing the V2 conflict screen, Admin B's overwrite is rejected with a new `ConcurrencyConflict` and reloaded V3 state.

---

## 2. Explicit Human Decisions Outstanding

In accordance with strict change control and safety directives, the following operations are explicitly left pending for authorized human decision-makers:

1. **GitHub Account Billing Resolution:**
   - Authorize or update GitHub billing settings at `https://github.com/settings/billing` to allow GitHub-hosted runners to execute workflow runs for PR #1.
2. **Peer / Code Review Sign-Off:**
   - Review Pull Request #1: `https://github.com/OlulekeKEWALE/Techcrafts.Blogging/pull/1`.
   - Complete the reviewer checklist in the PR body.
3. **Draft to Ready-for-Review Transition:**
   - Change PR #1 from Draft to Ready for Review once peer reviewers are assigned.
4. **Merge Authorization:**
   - Authorize merge of `feature/admin-post-management` into `master` following green CI run and human review.
5. **Staging / Production Deployment Authorization:**
   - Execute the migration and deployment runbook only under controlled change management windows.
