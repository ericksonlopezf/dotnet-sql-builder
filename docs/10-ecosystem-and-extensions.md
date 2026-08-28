# 10. Ecosystem and Extensions

The EricksonLopez Ecosystem does not stop at the SqlBuilder. It combines harmoniously with:

- **EricksonLopez.SharedKernel**: Base abstractions.
- **EricksonLopez.DomainPrimitives**: Utilities for ValueObjects and DDD (Domain Driven Design).
- **EricksonLopez.Outbox**: Robust Outbox pattern, which takes advantage of SqlBuilder queries.

Having a SQL builder that understands Value Objects and Postgres enumeration types natively is vital. Extensions are planned to support NodaTime (Postgres) and EntityFramework Interceptors (in the future).
