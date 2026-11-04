# Account Deposits: Interview Task

A backend interview task. You build a small REST service that holds account balances in a relational database and charges commission on deposits, withdrawals and transfers, with currency conversion through the free [Frankfurter](https://frankfurter.dev) exchange-rate API.

This README explains **how to work on the task and what to hand in**. The task itself, including every business rule, the expected fee calculations and the acceptance criteria, is in [Task.md](Task.md).

---

## Workflow at a glance

```
1. Read Task.md                  ->  understand the rules and the expected results
2. Ask questions                 ->  clear up anything ambiguous with your mentor
3. Create your branch            ->  feature/<firstname>-<lastname>
4. Build the solution            ->  small, structured commits
5. Write DEPLOYMENT.md           ->  code structure, configuration, every runnable script
6. Write FEATURES.md             ->  endpoints, DB schema, validations, examples
7. Push and notify your mentor   ->  origin/feature/<firstname>-<lastname>
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

**If you get no answer in time:** choose the most reasonable interpretation, write it down in the **Assumptions** section of [FEATURES.md](#6-write-featuresmd) and keep going. A clearly documented assumption is never counted against you.

---

## 3. Create your branch

Branch off `master`, using the `feature/` prefix and your name.

```bash
git clone https://github.com/veraiotech/veraio-interview-tasks.git
cd veraio-interview-tasks
git checkout master
git pull
git checkout -b feature/<firstname>-<lastname>
```

Example: `feature/maria-ivanova`.

Put all your work inside `Backend/AccountDeposits/`. Do not modify other tasks in the repository.

**Do not** push to `master`, and do not open a pull request unless your mentor asks for one.

---

## 4. Build the solution in structured commits

Reviewers read your commit history, not only the final code. The history should tell the story of how the solution was built.

- **Use [Conventional Commits](https://www.conventionalcommits.org/)**: `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`.
- **Write the message in the imperative**, describing what the commit does: `feat: add deposit operation`, not `changes` or `wip`.
- **Avoid both extremes**: no single "complete solution" commit, and no trail of `fix typo` commits. Squash those locally before pushing.
- **No generated or local files**: build output, dependencies, local configuration, local databases. Use `.gitignore`.
- **No secrets** in the repository.

---

## 5. Write DEPLOYMENT.md

Add `Backend/AccountDeposits/DEPLOYMENT.md`. It explains **how the application is built and how to operate it**. A reviewer who has never seen your code must be able to find their way around the source, configure the service and run every script using only this file.

It must contain the following sections.

### 5.1 Overview and tech stack

A short description of the service, then the stack: language and version, framework, database, main libraries and test tooling.

### 5.2 Prerequisites

Everything that must be installed before the service can run, with versions.

### 5.3 Environment variables

How configuration reaches the service:

- **File**: the name and location of the file the variables are read from, and whether the service loads it automatically or it must be passed in another way.
- **Template**: the committed template file the reviewer copies to create their own, and the exact copy step.
- **Structure**: the file format and naming conventions, so the reviewer can edit it without guessing.
- **Overrides**: how a variable can be set without the file (for example from the shell or a container), and which source wins when both are present.
- **Supported variables**: a table listing **every** variable the service reads, nothing more and nothing less:

| Variable | Required | Default | Description |
|---|---|---|---|

- **Behavior when a required variable is missing**: what the service does and what message it shows.

The real configuration file with local values must not be committed. Only the template is.

### 5.4 Project structure

An annotated directory tree, two or three levels deep, saying what each folder owns. Then explain:

- the layers of the application and how they depend on each other;
- where the business rules live;
- where the exchange-rate integration lives and how it is replaced in tests;
- the path of one request end to end, from the HTTP layer to the database.

### 5.5 Scripts

A table of **every** command a reviewer can run against the project: at minimum start, stop, build, migrate, seed and test, plus any others you provide.

| Script | Command | Run from | What it does |
|---|---|---|---|

Each command must be copy-pasteable. State what each one needs beforehand (for example a running database) and whether it is safe to run more than once.

Then give the **first-run sequence**: the exact ordered steps from a fresh clone to a running, seeded service.

### 5.6 Technical decisions

The key technical decisions you made, the alternatives you considered and why you chose as you did.

---

## 6. Write FEATURES.md

Add `Backend/AccountDeposits/FEATURES.md`. It describes **what the service does** in terms of its API and data. A reviewer must be able to call every endpoint and predict every response using only this file.

It must contain the following sections.

### 6.1 Database schema

Every table with its columns, types, constraints and indexes, and the relations between tables. A diagram is welcome.

| Column | Type | Constraints | Description |
|---|---|---|---|

### 6.2 Seed data

Every seeded user with its type, and every account with its ID, currency and starting balance, so the reviewer can use the IDs in requests.

### 6.3 Endpoints

A summary table of all endpoints first, then for **every** endpoint:

- method, path and one-line purpose;
- **request model**: every field with type, whether it is required, and its format;
- **response model**: every field with type;
- **validation rules** that apply to it;
- **error responses**: status code, error code and when each happens;
- an **example request** and its **example response**, taken from a real run against the seeded data.

### 6.4 Validations and error format

One table of every validation rule across the API, with the field, the rule and the resulting error, and the shape of the error body shared by all endpoints.

### 6.5 Assumptions

Every interpretation you made where [Task.md](Task.md) was ambiguous or your question went unanswered, with one sentence of reasoning each.

### 6.6 Known limitations (optional)

Anything you knowingly left out or would do differently with more time.

---

## 7. Push your branch

```bash
git push -u origin feature/<firstname>-<lastname>
```

Then send your mentor:

- the branch name;
- roughly how much time you spent;
- anything you want the reviewer to look at first.

You can keep pushing until the deadline your mentor gave you. Only the state of the branch at the deadline is reviewed.

---

## 8. Final output

When you are done, your branch `feature/<firstname>-<lastname>` on `origin` contains, under `Backend/AccountDeposits/`:

| Item | What it is |
|---|---|
| Source code | The REST service implementing every requirement in [Task.md](Task.md) |
| Migrations | Create the whole schema from an empty database |
| Seed script | At least one basic and one premium user, with accounts in at least two currencies |
| Environment template | The committed template described in `DEPLOYMENT.md` |
| `DEPLOYMENT.md` | Tech stack, prerequisites, environment variables, project structure, every runnable script, technical decisions |
| `FEATURES.md` | DB schema, seed data, every endpoint with request and response models, validations, examples, assumptions |
| `.gitignore` | Keeps build output, dependencies and local files out of the repository |
| e2e tests (bonus) | One or more automated tests per endpoint |

**Before you push, check that:**

- [ ] A fresh clone reaches a running, seeded service using only the first-run sequence in `DEPLOYMENT.md`.
- [ ] Every environment variable the code reads is in the `DEPLOYMENT.md` table, and every variable in the table is read by the code.
- [ ] Every script in the `DEPLOYMENT.md` table runs without errors.
- [ ] The seed IDs in `FEATURES.md` match the database after seeding.
- [ ] Every example in `FEATURES.md` returns the documented response against a freshly seeded service.
- [ ] Every example in [Task.md section 4](Task.md#4-expected-results) produces the documented fee and balance.
- [ ] Every item in [Task.md section 5](Task.md#5-acceptance-criteria) is met, or the gap is listed under Known limitations.
- [ ] The commit history is clean and tells the story of the solution.
- [ ] No secrets, local configuration files, local databases or build output are committed.
