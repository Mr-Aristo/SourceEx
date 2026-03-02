using SourceEx.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SourceEx.Domain.Events;

// Harcama oluşturulduğunda fırlatılacak event
public record ExpenseCreatedDomainEvent(Guid ExpenseId) : IDomainEvent;

// Harcama onaylandığında fırlatılacak event
public record ExpenseApprovedDomainEvent(Guid ExpenseId) : IDomainEvent;