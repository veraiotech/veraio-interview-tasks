# Account Deposits: Commission Service

Users hold money in currency accounts. They can deposit money, withdraw it, and transfer it to other users. Every operation carries a commission that depends on the operation, the user's tier and how much the user has already moved this week.

---

## 1. Business requirements

### 1.1 Users and accounts

- Users and their accounts are predefined. Creating, changing or closing users and accounts is out of scope.
- Each user is either **basic** or **premium**.
- Each user has **one or more currency accounts**.
- Supported currencies: **EUR**, **USD**, **GBP**.
- The predefined data must include:
  - at least one basic user and at least one premium user;
  - accounts in at least two different currencies.

### 1.2 Operations

A user can:

| Operation    | Description                                                                                        |
| ------------ | -------------------------------------------------------------------------------------------------- |
| Deposit      | Add money to one of their accounts                                                                 |
| Withdraw     | Take money out of one of their accounts                                                            |
| Transfer     | Send money from one of their accounts to another user's account, including in a different currency |
| View balance | See the current balance of every account they hold                                                 |
| View history | See every operation made on their accounts                                                         |

### 1.3 Operation outcome

Every successful deposit, withdrawal or transfer tells the user:

- the **operation reference**;
- the **fee charged**, with its currency;
- the **resulting balance** of the account the fee was charged to.

For a transfer, the user is also told the amount the receiver got, in the receiver's currency.

### 1.4 Rejections

- An operation that would leave the balance negative (**amount + fee > balance**) is rejected, and nothing about it is recorded.
- A user cannot transfer money to **themselves**, including between two of their own accounts.

---

## 2. Currency exchange

- Rates must be fetched from the free [Frankfurter API](https://frankfurter.dev) (no API key required) on **every operation that needs a conversion**, for example `https://api.frankfurter.dev/v1/latest?base=EUR`.
- Fees are **calculated in EUR**, then converted to the paying account's currency.
- The converted fee is **rounded up** to that currency's smallest unit:

| Currency | Smallest unit | Example               |
| -------- | ------------- | --------------------- |
| EUR      | 0.01          | 1.661 EUR -> 1.67 EUR |
| USD      | 0.01          | 1.665 USD -> 1.67 USD |
| GBP      | 0.01          | 0.005 GBP -> 0.01 GBP |

- The fee is **always charged to the paying user, in their account's currency**.
- When rates are unavailable, any operation that needs a conversion is refused.

---

## 3. Commission rules

### 3.1 Deposit

| User type | Fee                                         |
| --------- | ------------------------------------------- |
| All users | 0.05% of the amount, capped at **3.00 EUR** |

### 3.2 Withdrawal

| User type | Fee                                                                    |
| --------- | ---------------------------------------------------------------------- |
| Basic     | 0.5% of the amount, minimum **1.00 EUR**                               |
| Premium   | 0.25% of the part of the amount that exceeds the weekly free allowance |

**Premium weekly free allowance**

- **2,000 EUR** per calendar week, Monday to Sunday.
- Granted for at most **five operations** per week.
- From the **sixth** operation in a week onwards, 0.25% applies to the **full amount**, even if part of the allowance is unused.
- Premium users have **no minimum fee**.

### 3.3 Transfer

The **sender** pays the fee. The receiver pays nothing.

| Sender type | Fee                                                                      |
| ----------- | ------------------------------------------------------------------------ |
| Basic       | 0.5% of the amount, minimum **1.00 EUR**                                 |
| Premium     | Same weekly allowance and **same five-operation counter** as withdrawals |

**Cross-currency transfers**

- The amount is converted via EUR at the rates fetched for the operation.
- The receiver is credited in their account's currency, **rounded down** to its smallest unit.
- The sender is debited **amount + fee**, in the sender's currency.

---

## 4. Expected results

The examples below use these illustrative rates:

```
1 EUR = 1.10 USD
1 EUR = 0.85 GBP
```

### 4.1 Deposits

| Amount        | Amount in EUR | Raw fee (EUR) | Applied fee (EUR) | Fee charged                |
| ------------- | ------------- | ------------- | ----------------- | -------------------------- |
| 1,000.00 EUR  | 1,000.00      | 0.50          | 0.50              | **0.50 EUR**               |
| 10,000.00 EUR | 10,000.00     | 5.00          | 3.00 (cap)        | **3.00 EUR**               |
| 1,000.00 USD  | 909.0909...   | 0.4545...     | 0.4545...         | **0.50 USD**               |
| 10,000.00 GBP | 11,764.70...  | 5.8823...     | 3.00 (cap)        | **2.55 GBP**               |
| 10.00 GBP     | 11.7647...    | 0.005882...   | 0.005882...       | **0.01 GBP** (0.005 rounded up) |

The resulting balance is `previous balance + amount - fee`.

### 4.2 Withdrawals: basic user

| Amount       | Amount in EUR | Raw fee (EUR) | Applied fee (EUR) | Fee charged                     |
| ------------ | ------------- | ------------- | ----------------- | ------------------------------- |
| 100.00 EUR   | 100.00        | 0.50          | 1.00 (min)        | **1.00 EUR**                    |
| 1,000.00 EUR | 1,000.00      | 5.00          | 5.00              | **5.00 EUR**                    |
| 333.00 USD   | 302.7272...   | 1.5136...     | 1.5136...         | **1.67 USD** (1.665 rounded up) |
| 50.00 GBP    | 58.8235...    | 0.2941...     | 1.00 (min)        | **0.85 GBP**                    |

### 4.3 Withdrawals and transfers: premium user

**Scenario A: allowance consumed by amount.** EUR account, all operations in the same week.

| #   | Operation  | Amount       | Free part | Charged part | Fee          |
| --- | ---------- | ------------ | --------- | ------------ | ------------ |
| 1   | Withdrawal | 1,500.00 EUR | 1,500.00  | 0.00         | **0.00 EUR** |
| 2   | Transfer   | 1,000.00 EUR | 500.00    | 500.00       | **1.25 EUR** |
| 3   | Withdrawal | 200.00 EUR   | 0.00      | 200.00       | **0.50 EUR** |

**Scenario B: allowance cut off by the operation counter.** EUR account, same week.

| #           | Operation  | Amount          | Fee               | Note                                                                       |
| ----------- | ---------- | --------------- | ----------------- | -------------------------------------------------------------------------- |
| 1-5         | Withdrawal | 100.00 EUR each | **0.00 EUR** each | 500 EUR of allowance used                                                  |
| 6           | Withdrawal | 100.00 EUR      | **0.25 EUR**      | Sixth operation: full amount charged, although 1,500 EUR allowance is left |
| 7           | Withdrawal | 4,000.00 EUR    | **10.00 EUR**     |                                                                            |
| next Monday | Withdrawal | 100.00 EUR      | **0.00 EUR**      | Counter and allowance reset                                                |

**Scenario C: non-EUR account.** First operation of the week, USD account.

| Amount       | Amount in EUR | Charged part (EUR) | Fee (EUR) | Fee charged                     |
| ------------ | ------------- | ------------------ | --------- | ------------------------------- |
| 2,750.00 USD | 2,500.00      | 500.00             | 1.25      | **1.38 USD** (1.375 rounded up) |

### 4.4 Transfers

| Sender | From | To  | Amount sent | Fee                            | Sender debited | Receiver credited                       |
| ------ | ---- | --- | ----------- | ------------------------------ | -------------- | --------------------------------------- |
| Basic  | USD  | EUR | 110.00 USD  | 1.00 EUR (min) -> **1.10 USD** | **111.10 USD** | **100.00 EUR**                          |
| Basic  | USD  | GBP | 100.00 USD  | 1.00 EUR (min) -> **1.10 USD** | **101.10 USD** | **77.27 GBP** (77.2727... rounded down) |
| Basic  | EUR  | USD | 10.01 EUR   | **1.00 EUR** (min)             | **11.01 EUR**  | **11.01 USD** (11.011 rounded down)     |

### 4.5 Rejections

| Case                                                                      | Expected result                                     |
| ------------------------------------------------------------------------- | --------------------------------------------------- |
| Basic user, balance 100.00 EUR, withdraws 99.50 EUR (fee 1.00)            | Rejected: 100.50 > 100.00. Balance stays 100.00 EUR |
| Basic user, balance 100.00 EUR, withdraws 99.00 EUR (fee 1.00)            | Accepted. Resulting balance **0.00 EUR**            |
| Transfer between two accounts of the same user                            | Rejected                                            |
| Amount `0` or negative                                                    | Rejected                                            |
| Amount finer than the currency's smallest unit (`10.001 EUR`, `10.005 GBP`) | Rejected                                            |
| Unknown user or account                                                   | Rejected                                            |
| Exchange rates unavailable and the operation needs conversion             | Rejected. Nothing recorded                          |

Every rejection tells the user why the operation was refused.

---

## 5. Acceptance criteria

### 5.1 Users and accounts

- [ ] At least one basic and one premium user exist.
- [ ] Accounts exist in at least two of EUR, USD, GBP.
- [ ] Users and accounts cannot be created, changed or removed.

### 5.2 Money operations

- [ ] Deposit, withdrawal and transfer each report the operation reference, the fee (with currency) and the resulting balance.
- [ ] Every fee in section 4 is reproduced exactly when the same rates apply.
- [ ] Fees are computed in EUR, converted to the paying account's currency and rounded **up** to its smallest unit.
- [ ] The deposit fee never exceeds 3.00 EUR (converted).
- [ ] The basic withdrawal and transfer fee is never below 1.00 EUR (converted).
- [ ] Transfer receivers are credited in their own currency, rounded **down**, and pay no fee.
- [ ] Transfer senders are debited amount + fee in their own currency.
- [ ] Amounts and fees are exact to the cent. For example, the 1,000.00 USD deposit fee is 0.50 USD, never 0.51 USD.

### 5.3 Premium allowance

- [ ] The allowance is 2,000 EUR per calendar week, Monday 00:00 to Sunday 23:59:59.
- [ ] Withdrawals and outgoing transfers share one allowance and one operation counter. Deposits and incoming transfers do not touch either.
- [ ] Each of the first five operations of the week counts toward the limit, including operations whose fee was zero.
- [ ] From the sixth operation onwards, the fee is 0.25% of the full amount.
- [ ] Counter and allowance reset at the start of each calendar week.
- [ ] Rejected operations consume neither allowance nor counter.

### 5.4 Rejections and consistency

- [ ] An operation where amount + fee exceeds the balance is rejected, leaves the balance unchanged, uses no allowance and does not appear in the history.
- [ ] Self-transfers are rejected.
- [ ] Invalid amounts, unknown accounts and unavailable rates are rejected with a clear reason.
- [ ] A transfer either debits the sender and credits the receiver, or does neither.
- [ ] Operations made at the same moment on the same account can never overdraw it or use the premium allowance twice.

### 5.5 Balance and history

- [ ] A user can see every account they hold, with its currency and current balance.
- [ ] The history lists every completed operation with: reference, type, amount, currency, fee, resulting balance, date and time, and for transfers the counterparty account.
- [ ] Both sides of a transfer appear in the history of the respective users.
- [ ] History is ordered newest first.

---

## 6. Deliverables

- A working solution with instructions to run it and try every operation.
- A short list of any assumptions you made, for example the time zone used to decide the calendar week, or whether the premium allowance is tracked per user or per account.

**Bonus:** automated e2e tests for each endpoint
