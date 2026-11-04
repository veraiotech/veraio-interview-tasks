# Account Deposits: Interview Task

A backend interview task. You build a small REST service that holds account balances in a relational database and charges commission on deposits, withdrawals and transfers, with currency conversion through the free [Frankfurter](https://frankfurter.dev) exchange-rate API.

This README explains **how to work on the task and what to hand in**. The task itself, including every business rule, the expected fee calculations and the acceptance criteria, is in [Task.md](Task.md).

> **Requirements may change.** As in a real project, the requirements in this README and in [Task.md](Task.md) may change while you work on the task. Your mentor will tell you when they do.

---

## Workflow at a glance

```
1. Read Task.md                  ->  understand the rules and the expected results
2. Ask questions                 ->  clear up anything ambiguous with your mentor
3. Create your feature branch     ->  feature/<firstname>-<lastname>
4. Write DEPLOYMENT.md           ->  document your solution
5. Write FEATURES.md             ->  endpoints, DB schema, validations, examples
6. Push and notify your mentor   ->  origin/feature/<firstname>-<lastname>
```

---

## 1. Review the task

Read [Task.md](Task.md) end to end before you write any code. Pay particular attention to:

- **Section 3, Commission rules**: how each fee is calculated.
- **Section 4, Expected results**: worked examples with fixed exchange rates. Your implementation should reproduce every number in these tables.
- **Section 5, Acceptance criteria**: the checklist your solution is reviewed against.

### Technology

The stack is fixed:

| Layer | Technology |
|---|---|
| Language | C# |
| Framework | .NET |
| Database | PostgreSQL |

The project layout is your choice. A reviewer should be able to start your solution with as few steps as possible.

---

## 2. Asking questions

Questions are welcome. Asking good questions is part of what we assess.

**Who to ask:** the mentor who sent you this task, through the same channel they contacted you on.

**When to ask:** before you start, if a rule in [Task.md](Task.md) can be read more than one way and the answer changes your design.

**How to ask:** for each question, say what you understood, what is unclear and which option you would choose by default. That way you can continue working while you wait for the answer.

**If you get no answer in time:** choose the most reasonable interpretation, write it down in the **Assumptions** section of [FEATURES.md](#5-write-featuresmd) and keep going. A clearly documented assumption is never counted against you.

---

## 3. Create your feature branch

Create a feature branch off `master` named `feature/<firstname>-<lastname>`, and put all your work inside `Backend/AccountDeposits/`. Do not modify other tasks in the repository.

---

## 4. Write DEPLOYMENT.md

Add `Backend/AccountDeposits/DEPLOYMENT.md` documenting your solution. What it covers and how it is structured is your choice.

---

## 5. Write FEATURES.md

Add `Backend/AccountDeposits/FEATURES.md`. It describes **what the service does** in terms of its API and data. A reviewer must be able to call every endpoint and predict every response using only this file.

It must contain the following sections.

### 5.1 Database schema

Every table with its columns, types, constraints and indexes, and the relations between tables. A diagram is welcome.

| Column | Type | Constraints | Description |
|---|---|---|---|

### 5.2 Seed data

Every seeded user with its type, and every account with its ID, currency and starting balance, so the reviewer can use the IDs in requests.

### 5.3 Endpoints

A summary table of all endpoints first, then for **every** endpoint:

- method, path and one-line purpose;
- **request model**: every field with type, whether it is required, and its format;
- **response model**: every field with type;
- **validation rules** that apply to it;
- **error responses**: status code, error code and when each happens;
- an **example request** and its **example response**, taken from a real run against the seeded data.

### 5.4 Validations and error format

One table of every validation rule across the API, with the field, the rule and the resulting error, and the shape of the error body shared by all endpoints.

### 5.5 Assumptions

Every interpretation you made where [Task.md](Task.md) was ambiguous or your question went unanswered, with one sentence of reasoning each.

### 5.6 Known limitations (optional)

Anything you knowingly left out or would do differently with more time.

---

## 6. Push your branch

Push your branch to `origin`, then send your mentor:

- the branch name;
- roughly how much time you spent;
- anything you want the reviewer to look at first.

You can keep pushing until the deadline your mentor gave you. Only the state of the branch at the deadline is reviewed.

---

## 7. Final output

When you are done, your branch `feature/<firstname>-<lastname>` on `origin` contains, under `Backend/AccountDeposits/`:

| Item | What it is |
|---|---|
| Source code | The REST service implementing every requirement in [Task.md](Task.md) |
| Migrations | Create the whole schema from an empty database |
| Seed script | At least one basic and one premium user, with accounts in at least two currencies |
| `DEPLOYMENT.md` | Your documentation of the solution |
| `FEATURES.md` | DB schema, seed data, every endpoint with request and response models, validations, examples, assumptions |
| `.gitignore` | Keeps build output, dependencies and local files out of the repository |
| e2e tests (bonus) | One or more automated tests per endpoint |

**Before you push, check that:**

- [ ] The seed IDs in `FEATURES.md` match the database after seeding.
- [ ] Every example in `FEATURES.md` returns the documented response against a freshly seeded service.
- [ ] Every example in [Task.md section 4](Task.md#4-expected-results) produces the documented fee and balance.
- [ ] Every item in [Task.md section 5](Task.md#5-acceptance-criteria) is met, or the gap is listed under Known limitations.
- [ ] No secrets, local configuration files, local databases or build output are in the repository.
