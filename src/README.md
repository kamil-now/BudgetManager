# Budget Manager

WORK IN PROGRESS

## The model

A user owns ledgers. A ledger holds accounts, where money actually sits, and budgets, which are plans for that money, split into funds.

A transaction adds or removes money from an account. A transfer is two transactions, one negative and one positive, joined by a link row. A currency exchange has the same shape with a different currency on each side, so nothing in the system holds exchange rates.

Every amount carries its currency. Amounts in different currencies are never added together, so a balance is one total per currency rather than a single number.

Ledgers, accounts and budgets each have an owner. There are no roles; a user sees their own rows and nothing else.

## Structure

    Api            HTTP, tokens, turning errors into status codes
    Application    one handler per thing a user can do, and the interfaces the reads use
    Domain         the model, and the interface the writes use
    Infrastructure database, tokens, password hashing
    Common         the mediator, money, balances

`Api` depends on `Application` and `Infrastructure`; `Application` on `Domain`; `Infrastructure` on `Domain` and on `Application`, whose interfaces it implements. `Common` depends on nothing.

Controllers hold no logic. They take a command or query as the request body and hand it to the mediator, which finds the single handler registered for that type. Handlers are found by scanning the `Application` project at startup; the mediator is a minimal implementation of the pattern, not a library.

Exceptions thrown by a handler become status codes at the edge — 404 for not found, 422 for
bad values, 401 for not logged in, 403 for not yours. Anything else surfaces as a 500.

Writes go through one store of generic entity operations. Every read has a class of its own that fetches the rows that read needs and nothing else - `LedgerTransactionsReader` returns a ledger's transactions - written next to the database code and named after the data it returns. One such class can feed more than one read, and a new read adds files instead of growing a shared one. Handlers never touch the database context, the store never saves, and multi-row writes run inside a transaction. Reads still share the write model, so a ledger view loads all of its transactions before adding them up.

## Design decisions

- Handlers call save, and may call it more than once. The store never saves, so the handler decides what one write covers.
- The mediator is written by hand instead of taken from a library. It finds the single handler registered for a request type and calls it, which is all this project needs.
- There are no pipeline steps around a handler. Access is checked in the mediator, validation and transactions are plain code inside the handler, and request logging is middleware in the `Api`.
- There are no roles or permissions. Only a ledger has an owner column, every other row reaches it through its parent, and a request lists the rows the mediator has to check against the user in the token.
- Missing abstractions are deliberate. Nothing is added for a case that does not exist yet.

## Elsewhere

[GUIDELINES.md](GUIDELINES.md) — commands, test layout, code style.
