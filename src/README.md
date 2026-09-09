# Budget Manager

WORK IN PROGRESS

## The model

A user owns ledgers. A ledger holds accounts, where money actually sits, and budgets, which are plans for that money, split into funds.

A transaction adds or removes money from an account. A transfer is two transactions, one negative and one positive, joined by a link row. A currency exchange has the same shape with a different currency on each side, so nothing in the system holds exchange rates.

Every amount carries its currency. Amounts in different currencies are never added together, so a balance is one total per currency rather than a single number.

Ledgers, accounts and budgets each have an owner. There are no roles; a user sees their own rows and nothing else.

## Structure

    Api            HTTP, tokens, turning errors into status codes
    Application    one handler per thing a user can do
    Domain         the model, and the interfaces the database code implements
    Infrastructure database, tokens, password hashing
    Common         the mediator, money, balances

`Api` depends on `Application` and `Infrastructure`; `Application` on `Domain`; `Infrastructure` on `Domain`, whose interfaces it implements, and on `Application` for password hashing. `Common` depends on nothing.

Controllers hold no logic. They take a command or query as the request body and hand it to the mediator, which finds the single handler registered for that type. Handlers are found by scanning the `Application` project at startup; the mediator is a minimal implementation of the pattern, not a library.

Exceptions thrown by a handler become status codes at the edge — 404 for not found, 422 for
bad values, 401 for not logged in, 403 for not yours. Anything else surfaces as a 500.

Every database call goes through one helper. Handlers never touch the database context, the helper never saves, and multi-row writes run inside a transaction. Reads share the write model, so a ledger view loads all of its transactions before adding them up.

## Elsewhere

[GUIDELINES.md](GUIDELINES.md) — commands, test layout, code style.
