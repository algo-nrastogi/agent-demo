---
applyTo: "src/Modules/**/Application/**/*.cs"
---

# CQRS Command/Query authoring rules

When creating or editing anything under an `Application/Commands/**` or `Application/Queries/**`
folder:

1. **Naming**: `<Feature>Command` / `<Feature>Query` (input), `<Feature>CommandHandler` /
   `<Feature>QueryHandler` (handler), `<Feature>CommandValidator` (validator). File name matches
   type name exactly, one type per file.
2. **Commands** return either `Result<Guid>` / `Result<TDto>` for something that creates/returns
   an id, or plain `Result` for void-like operations. Never return the raw domain entity from a
   command handler — map to a DTO.
3. **Queries** return `Result<TDto>` or `Result<IReadOnlyList<TDto>>`. Queries must not mutate
   state — no `SaveChangesAsync` in a query handler.
4. **Handler shape**:
   ```csharp
   internal sealed class CreateOrderCommandHandler(
       IOrderRepository orderRepository,
       IUnitOfWork unitOfWork,
       IDateTimeProvider dateTimeProvider)
       : IRequestHandler<CreateOrderCommand, Result<Guid>>
   {
       public async Task<Result<Guid>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
       {
           // 1. Load/verify anything required from repositories
           // 2. Construct/mutate the domain entity (invariants enforced inside the entity)
           // 3. Persist via repository + unit of work
           // 4. Return Result.Success(...)
       }
   }
   ```
   Use primary-constructor DI (as above) — do not hand-roll a constructor + private readonly fields.
5. **Validators** validate only shape/format/required-field concerns (FluentValidation). Business
   rules that depend on state (e.g. "SKU must exist") belong in the handler as a `Result.Failure`,
   not the validator, unless the check is a pure input-shape rule.
6. Every public command/query DTO uses `record` types for immutability.
7. Do not swallow exceptions from repositories — let infrastructure failures bubble up to the
   global exception middleware; only convert *expected* domain/validation outcomes to `Result`.
