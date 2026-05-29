using System;

namespace WexTran.Api.Exceptions
{
    public class TransactionNotFoundException : Exception
    {
        public TransactionNotFoundException(Guid id)
            : base($"Transaction '{id}' was not found.") { }
    }
}
